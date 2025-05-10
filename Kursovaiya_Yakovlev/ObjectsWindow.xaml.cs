using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using System.Windows.Media.Imaging;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Data;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
using System.Linq;

namespace Kursovaiya_Yakovlev
{
    public partial class ObjectsWindow : Window
    {
        private HouseData _selectedObject;
        private List<string> _currentImages = new List<string>();
        private int _currentImageIndex = 0;
        private bool _isFirstLoad = true;

        public ObjectsWindow()
        {
            InitializeComponent();
            ConfigureDataGrid();
            LoadObjects();
            UpdateImageNavigation();
           if(UserSession.accessR == 3)
            {
                
                    DeleteButton.Visibility = Visibility.Collapsed;
                    AddButton.Visibility = Visibility.Collapsed;
                    EditButton.Visibility = Visibility.Collapsed;
            }
        }

        private void ConfigureDataGrid()
        {
            ObjectsDataGrid.AutoGenerateColumns = false;
            ObjectsDataGrid.Columns.Clear();

            ObjectsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "ID",
                Binding = new Binding("Id"),
                Width = 50
            });
            ObjectsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Название",
                Binding = new Binding("Title"),
                Width = 150
            });
            ObjectsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Статус",
                Binding = new Binding("Status.Title"),
                Width = 120
            });
            ObjectsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Цена",
                Binding = new Binding("Price") { StringFormat = "{0:N0} руб." },
                Width = 100
            });
            ObjectsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Этаж",
                Binding = new Binding("PropertyDetails.floor"),
                Width = 60
            });
            ObjectsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Площадь",
                Binding = new Binding("PropertyDetails.square"),
                Width = 80
            });
            ObjectsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Комнат",
                Binding = new Binding("PropertyDetails.room_count"),
                Width = 70
            });
            ObjectsDataGrid.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Балкон",
                Binding = new Binding("PropertyDetails.balcony"),
                IsReadOnly = true,
                Width = 70
            });
            ObjectsDataGrid.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Лифт",
                Binding = new Binding("PropertyDetails.elevator"),
                IsReadOnly = true,
                Width = 70
            });
        }

        private void LoadObjects()
        {
            try
            {
                using var context = DatabaseContext.GetContext();
                var objects = context.HouseData
                    .Include(o => o.Transactions)
                    .Include(o => o.Status) // Include Status
                    .AsEnumerable()
                    .Select(o => new
                    {
                        o.Id,
                        o.Title,
                        o.Price,
                        o.Status, // Include Status
                        FormattedAddress = FormatAddress(o.Address),
                        PropertyDetails = o.PropertyDetails,
                        o.Images,
                        o.Properties
                    })
                    .ToList();

                ObjectsDataGrid.ItemsSource = objects;
                _isFirstLoad = false;
            }
            catch (Exception ex)
            {
                ShowExtendedErrorMessage("Ошибка загрузки объектов", ex);
            }
        }

        private string FormatAddress(string jsonAddress)
        {
            if (string.IsNullOrEmpty(jsonAddress) || jsonAddress == "{}")
                return "Адрес не указан";

            try
            {
                var address = JsonSerializer.Deserialize<AddressInfo>(jsonAddress);
                if (address == null) return "Адрес не указан";

                var parts = new List<string>();
                if (!string.IsNullOrEmpty(address.city)) parts.Add(address.city);
                if (!string.IsNullOrEmpty(address.street)) parts.Add(address.street);
                if (!string.IsNullOrEmpty(address.house_number)) parts.Add(address.house_number);
                if (!string.IsNullOrEmpty(address.apartment_number)) parts.Add($"кв. {address.apartment_number}");

                return parts.Count > 0 ? string.Join(", ", parts) : "Адрес не указан";
            }
            catch
            {
                return "Неверный формат адреса";
            }
        }

        private void ApplyFilters()
        {
            try
            {
                using var context = DatabaseContext.GetContext();
                var allObjects = context.HouseData
                    .Include(o => o.Transactions)
                    .AsEnumerable()
                    .Select(o => new
                    {
                        o.Id,
                        o.Title,
                        o.Price,
                        FormattedAddress = FormatAddress(o.Address),
                        PropertyDetails = o.PropertyDetails,
                        o.Images,
                        o.Properties
                    })
                    .ToList();

                var query = allObjects.AsQueryable();

                if (int.TryParse(AreaFromTextBox.Text, out int minArea))
                    query = query.Where(o => int.Parse(o.PropertyDetails.square) >= minArea);
                if (int.TryParse(AreaToTextBox.Text, out int maxArea))
                    query = query.Where(o => int.Parse(o.PropertyDetails.square) <= maxArea);

                if (int.TryParse(RoomsFromTextBox.Text, out int minRooms))
                    query = query.Where(o => int.Parse(o.PropertyDetails.room_count) >= minRooms);
                if (int.TryParse(RoomsToTextBox.Text, out int maxRooms))
                    query = query.Where(o => int.Parse(o.PropertyDetails.room_count) <= maxRooms);

                if (ElevatorCheckBox.IsChecked == true)
                    query = query.Where(o => o.PropertyDetails.elevator);

                if (BalconyCheckBox.IsChecked == true)
                    query = query.Where(o => o.PropertyDetails.balcony);

                if (decimal.TryParse(PriceFromTextBox.Text, out decimal minPrice))
                    query = query.Where(o => o.Price >= minPrice);
                if (decimal.TryParse(PriceToTextBox.Text, out decimal maxPrice))
                    query = query.Where(o => o.Price <= maxPrice);

                if (MyObjectsCheckBox.IsChecked == true)
                {
                    // query = query.Where(o => o.OwnerId == currentUserId);
                }

                var result = query.ToList();

                if (result.Count > 0 || _isFirstLoad)
                    ObjectsDataGrid.ItemsSource = result;
                else
                    ObjectsDataGrid.ItemsSource = null;
            }
            catch (Exception ex)
            {
                ShowExtendedErrorMessage("Ошибка фильтрации", ex);
            }
        }

        private void ShowExtendedErrorMessage(string title, Exception ex)
        {
            string message = $"{title}: {ex.Message}";
            if (ex.InnerException != null)
                message += $"\nВнутреннее исключение: {ex.InnerException.Message}";

            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private async void ObjectsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ObjectsDataGrid.SelectedItem == null)
            {
                _selectedObject = null;
                ResetImageGallery();
                return;
            }

            try
            {
                var selectedItem = ObjectsDataGrid.SelectedItem;
                var idProperty = selectedItem.GetType().GetProperty("Id");
                int objectId = (int)idProperty.GetValue(selectedItem);

                using var context = DatabaseContext.GetContext();
                _selectedObject = await context.HouseData
                    .Include(o => o.Transactions)
                    .FirstOrDefaultAsync(o => o.Id == objectId);

                await LoadObjectImages();
            }
            catch (Exception ex)
            {
                ShowExtendedErrorMessage("Ошибка при выборе объекта", ex);
                _selectedObject = null;
                ResetImageGallery();
            }
        }

        private async Task LoadObjectImages()
        {
            _currentImages.Clear();
            _currentImageIndex = 0;

            if (_selectedObject != null)
            {
                _currentImages = _selectedObject.ImageUrls;
            }

            await DisplayCurrentImage();
        }

        private async Task DisplayCurrentImage()
        {
            if (_currentImages.Count == 0)
            {
                ObjectImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/no_image.png"));
                ImageCounter.Text = "Нет изображений";
                UpdateImageNavigation();
                return;
            }

            try
            {
                var imageUrl = _currentImages[_currentImageIndex];
                if (Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imageUrl);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ObjectImage.Source = bitmap;
                    ImageCounter.Text = $"{_currentImageIndex + 1}/{_currentImages.Count}";
                }
                else
                {
                    ObjectImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/no_image.png"));
                    ImageCounter.Text = "Неверный URL";
                }
            }
            catch (Exception ex)
            {
                ObjectImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/no_image.png"));
                ImageCounter.Text = "Ошибка загрузки";
                Console.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
            }
            finally
            {
                UpdateImageNavigation();
            }
        }

        private void UpdateImageNavigation()
        {
            PrevImageButton.IsEnabled = _currentImages.Count > 0 && _currentImageIndex > 0;
            NextImageButton.IsEnabled = _currentImages.Count > 0 && _currentImageIndex < _currentImages.Count - 1;
        }

        private void ResetImageGallery()
        {
            _currentImages.Clear();
            _currentImageIndex = 0;
            ObjectImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/no_image.png"));
            ImageCounter.Text = "0/0";
            UpdateImageNavigation();
        }

        private async void PrevImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentImageIndex > 0)
            {
                _currentImageIndex--;
                await DisplayCurrentImage();
            }
        }

        private async void NextImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentImageIndex < _currentImages.Count - 1)
            {
                _currentImageIndex++;
                await DisplayCurrentImage();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddEditObjectWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadObjects();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObject == null)
            {
                MessageBox.Show("Выберите объект для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editWindow = new AddEditObjectWindow(_selectedObject, _selectedObject.Address);
            if (editWindow.ShowDialog() == true)
            {
                LoadObjects();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObject == null)
            {
                MessageBox.Show("Выберите объект для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Удалить выбранный объект?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using var context = DatabaseContext.GetContext();
                    using var transaction = context.Database.BeginTransaction();

                    try
                    {
                        // Загружаем объект с транзакциями
                        var entity = context.HouseData
                            .Include(o => o.Transactions)
                            .FirstOrDefault(o => o.Id == _selectedObject.Id);

                        if (entity != null)
                        {
                            // Удаляем связанные транзакции
                            if (entity.Transactions != null && entity.Transactions.Any())
                            {
                                context.Transactions.RemoveRange(entity.Transactions);
                            }

                            // Удаляем сам объект
                            context.HouseData.Remove(entity);
                            context.SaveChanges();
                            transaction.Commit();
                            LoadObjects();
                        }
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ShowExtendedErrorMessage("Ошибка удаления", ex);
                }
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObject == null)
            {
                MessageBox.Show("Выберите объект для печати", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Загружаем полные данные объекта
                using var context = DatabaseContext.GetContext();
                var fullObject = context.HouseData
                    .Include(o => o.Status)
                    .FirstOrDefault(o => o.Id == _selectedObject.Id);

                if (fullObject != null)
                {
                    CreateWordDocument(fullObject);
                }
                else
                {
                    MessageBox.Show("Не удалось загрузить данные объекта", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подготовке данных для печати: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CreateWordDocument(HouseData houseData)
        {
            if (houseData == null)
            {
                MessageBox.Show("Не выбран объект для печати", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Настройка диалога сохранения файла
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Документ Word (*.docx)|*.docx",
                FileName = $"Карточка_объекта_{houseData.Id}_{DateTime.Now:yyyyMMddHHmmss}.docx",
                DefaultExt = ".docx",
                Title = "Сохранить карточку объекта"
            };

            // Показываем диалог и проверяем результат
            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                try
                {
                    using (WordprocessingDocument wordDocument =
                        WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
                    {
                        // Создаем главную часть документа
                        MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                        mainPart.Document = new Document(new Body());

                        // Добавляем стили
                        StyleDefinitionsPart stylePart = mainPart.AddNewPart<StyleDefinitionsPart>();
                        stylePart.Styles = GenerateDocumentStyles();
                        stylePart.Styles.Save();

                        // Получаем тело документа
                        Body body = mainPart.Document.Body;

                        // Шапка документа
                        Paragraph header = new Paragraph(
                            new ParagraphProperties(
                                new ParagraphStyleId() { Val = "Header" },
                                new Justification() { Val = JustificationValues.Center }
                            ),
                            new Run(new Text("ООО \"РиэлТОР\""))
                        );
                        body.Append(header);

                        // Дата документа
                        Paragraph date = new Paragraph(
                        new ParagraphProperties(
                            new ParagraphStyleId() { Val = "DocumentDate" },
                            new Justification() { Val = JustificationValues.Right }
                        ),
                        new Run(new Text(DateTime.Now.ToString("dd.MM.yyyy")))
                    );
                    body.Append(date);

                    // Заголовок документа
                    Paragraph title = new Paragraph(
                        new ParagraphProperties(
                            new ParagraphStyleId() { Val = "DocumentTitle" },
                            new Justification() { Val = JustificationValues.Center },
                            new SpacingBetweenLines() { After = "200" }
                        ),
                        new Run(new Text("КАРТОЧКА ОБЪЕКТА НЕДВИЖИМОСТИ"))
                    );
                    body.Append(title);

                    // Таблица с основными данными
                    Table table = new Table();

                    // Свойства таблицы
                    TableProperties tblProps = new TableProperties(
                        new TableBorders(
                            new TopBorder() { Val = BorderValues.Single, Size = 4 },
                            new BottomBorder() { Val = BorderValues.Single, Size = 4 },
                            new LeftBorder() { Val = BorderValues.Single, Size = 4 },
                            new RightBorder() { Val = BorderValues.Single, Size = 4 },
                            new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 4 },
                            new InsideVerticalBorder() { Val = BorderValues.Single, Size = 4 }
                        ),
                        new TableWidth() { Width = "100%", Type = TableWidthUnitValues.Pct }
                    );
                    table.AppendChild(tblProps);

                    // Добавляем строки в таблицу
                    table.Append(CreateTableRow("Идентификатор объекта", houseData.Id.ToString(), true));
                    table.Append(CreateTableRow("Наименование объекта", houseData.Title, false));
                    table.Append(CreateTableRow("Статус объекта", houseData.Status?.Title ?? "Не указан", false));
                    table.Append(CreateTableRow("Цена", $"{houseData.Price:N0} руб.", false));
                    table.Append(CreateTableRow("Адрес", FormatAddress(houseData.Address), false));
                    table.Append(CreateTableRow("Площадь", $"{houseData.PropertyDetails.square} м²", false));
                    table.Append(CreateTableRow("Этаж", houseData.PropertyDetails.floor, false));
                    table.Append(CreateTableRow("Количество комнат", houseData.PropertyDetails.room_count, false));
                    table.Append(CreateTableRow("Наличие лифта", houseData.PropertyDetails.elevator ? "Да" : "Нет", false));
                    table.Append(CreateTableRow("Наличие балкона", houseData.PropertyDetails.balcony ? "Да" : "Нет", false));

                    body.Append(table);

                    // Подпись
                    Paragraph signature = new Paragraph(
                        new ParagraphProperties(
                            new ParagraphStyleId() { Val = "Signature" },
                            new Justification() { Val = JustificationValues.Right },
                            new SpacingBetweenLines() { Before = "400" }
                        ),
                        new Run(new Text("Менеджер по недвижимости: _________________ /Иванов И.И./"))
                    );
                    body.Append(signature);

                    // Печать
                    Paragraph stamp = new Paragraph(
                        new ParagraphProperties(
                            new ParagraphStyleId() { Val = "Stamp" },
                            new Justification() { Val = JustificationValues.Left },
                            new SpacingBetweenLines() { Before = "600" }
                        ),
                        new Run(new Text("М.П."))
                    );
                    body.Append(stamp);

                        // Сохраняем документ
                        mainPart.Document.Save();
                    }

                    MessageBox.Show($"Документ успешно сохранен:\n{filePath}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при создании документа:\n{ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private TableRow CreateTableRow(string name, string value, bool isHeader)
        {
            TableRow row = new TableRow();

            // Ячейка с названием параметра
            TableCell nameCell = new TableCell(new Paragraph(
                new ParagraphProperties(new ParagraphStyleId() { Val = isHeader ? "TableHeader" : "TableCell" }),
                new Run(new Text(name))
            ));
            nameCell.TableCellProperties = new TableCellProperties(
                new Shading() { Fill = isHeader ? "D3D3D3" : "FFFFFF" } // Серый фон для заголовка
            );

            // Ячейка со значением
            TableCell valueCell = new TableCell(new Paragraph(
                new ParagraphProperties(new ParagraphStyleId() { Val = "TableCell" }),
                new Run(new Text(value))
            ));

            row.Append(nameCell, valueCell);
            return row;
        }

        private DocumentFormat.OpenXml.Wordprocessing.Styles GenerateDocumentStyles()
        {
            var styles = new DocumentFormat.OpenXml.Wordprocessing.Styles();

            // Стиль для заголовка документа
            var titleStyle = new DocumentFormat.OpenXml.Wordprocessing.Style()
            {
                Type = DocumentFormat.OpenXml.Wordprocessing.StyleValues.Paragraph,
                StyleId = "DocumentTitle"
            };
            titleStyle.Append(new DocumentFormat.OpenXml.Wordprocessing.StyleName() { Val = "Document Title" });
            titleStyle.Append(new DocumentFormat.OpenXml.Wordprocessing.NextParagraphStyle() { Val = "Normal" });
            titleStyle.Append(new DocumentFormat.OpenXml.Wordprocessing.StyleParagraphProperties(
                new DocumentFormat.OpenXml.Wordprocessing.SpacingBetweenLines() { After = "200" },
                new DocumentFormat.OpenXml.Wordprocessing.Justification()
                { Val = DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Center }
            ));
            titleStyle.Append(new DocumentFormat.OpenXml.Wordprocessing.StyleRunProperties(
                new DocumentFormat.OpenXml.Wordprocessing.Bold(),
                new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "28" },
                new DocumentFormat.OpenXml.Wordprocessing.RunFonts() { Ascii = "Times New Roman" }
            ));
            styles.Append(titleStyle);

            // Стиль для обычного текста
            var normalStyle = new DocumentFormat.OpenXml.Wordprocessing.Style()
            {
                Type = DocumentFormat.OpenXml.Wordprocessing.StyleValues.Paragraph,
                StyleId = "Normal"
            };
            normalStyle.Append(new DocumentFormat.OpenXml.Wordprocessing.StyleName() { Val = "Normal" });
            normalStyle.Append(new DocumentFormat.OpenXml.Wordprocessing.StyleRunProperties(
                new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "24" },
                new DocumentFormat.OpenXml.Wordprocessing.RunFonts() { Ascii = "Times New Roman" }
            ));
            styles.Append(normalStyle);

            // Стиль для шапки
            var headerStyle = new DocumentFormat.OpenXml.Wordprocessing.Style()
            {
                Type = DocumentFormat.OpenXml.Wordprocessing.StyleValues.Paragraph,
                StyleId = "Header"
            };
            headerStyle.Append(new DocumentFormat.OpenXml.Wordprocessing.StyleName() { Val = "Header" });
            headerStyle.Append(new DocumentFormat.OpenXml.Wordprocessing.StyleRunProperties(
                new DocumentFormat.OpenXml.Wordprocessing.Bold(),
                new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "26" },
                new DocumentFormat.OpenXml.Wordprocessing.RunFonts() { Ascii = "Times New Roman" }
            ));
            styles.Append(headerStyle);

            // Стиль для ячеек таблицы
            var tableCellStyle = new DocumentFormat.OpenXml.Wordprocessing.Style()
            {
                Type = DocumentFormat.OpenXml.Wordprocessing.StyleValues.Paragraph,
                StyleId = "TableCell"
            };
            tableCellStyle.Append(new DocumentFormat.OpenXml.Wordprocessing.StyleName() { Val = "Table Cell" });
            tableCellStyle.Append(new DocumentFormat.OpenXml.Wordprocessing.StyleRunProperties(
                new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "22" },
                new DocumentFormat.OpenXml.Wordprocessing.RunFonts() { Ascii = "Times New Roman" }
            ));
            styles.Append(tableCellStyle);

            // Стиль для заголовков таблицы
            var tableHeaderStyle = new DocumentFormat.OpenXml.Wordprocessing.Style()
            {
                Type = DocumentFormat.OpenXml.Wordprocessing.StyleValues.Paragraph,
                StyleId = "TableHeader"
            };
            tableHeaderStyle.Append(new DocumentFormat.OpenXml.Wordprocessing.StyleName() { Val = "Table Header" });
            tableHeaderStyle.Append(new DocumentFormat.OpenXml.Wordprocessing.StyleRunProperties(
                new DocumentFormat.OpenXml.Wordprocessing.Bold(),
                new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "22" },
                new DocumentFormat.OpenXml.Wordprocessing.RunFonts() { Ascii = "Times New Roman" }
            ));
            styles.Append(tableHeaderStyle);

            // Стиль для подписи
            var signatureStyle = new DocumentFormat.OpenXml.Wordprocessing.Style()
            {
                Type = DocumentFormat.OpenXml.Wordprocessing.StyleValues.Paragraph,
                StyleId = "Signature"
            };
            signatureStyle.Append(new DocumentFormat.OpenXml.Wordprocessing.StyleName() { Val = "Signature" });
            signatureStyle.Append(new DocumentFormat.OpenXml.Wordprocessing.StyleRunProperties(
                new DocumentFormat.OpenXml.Wordprocessing.Italic(),
                new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "22" },
                new DocumentFormat.OpenXml.Wordprocessing.RunFonts() { Ascii = "Times New Roman" }
            ));
            styles.Append(signatureStyle);

            return styles;
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}