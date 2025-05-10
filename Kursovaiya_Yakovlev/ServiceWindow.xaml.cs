using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
using Word = DocumentFormat.OpenXml.Wordprocessing;

namespace Kursovaiya_Yakovlev
{
    public partial class ServiceWindow : Window
    {
        private Service _selectedService;

        public ServiceWindow()
        {
            InitializeComponent();
            LoadServices();
            LoadFilterData();

            SearchTextBox.TextChanged += ApplyFilters;
            ExecutorComboBox.SelectionChanged += ApplyFilters;
            PriceFromTextBox.TextChanged += ApplyFilters;
            PriceToTextBox.TextChanged += ApplyFilters;
        }

        private void LoadServices()
        {
            try
            {
                using var context = DatabaseContext.GetContext();

                var services = context.Service
                    .Include(s => s.Staff)
                    .ThenInclude(st => st.Users)
                    .AsEnumerable()
                    .Select(s => new
                    {
                        s.Id,
                        s.Title,
                        s.Price,
                        ExecutorId = s.Staff?.Users?.Id,
                        ExecutorFullName = s.Staff?.Users?.FullName ?? "Не назначен"
                    })
                    .OrderBy(s => s.Title)
                    .ToList();

                DataGrid.ItemsSource = services;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}\n\n{ex.InnerException?.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void LoadFilterData()
        {
            try
            {
                using var context = DatabaseContext.GetContext();

                // Загрузка исполнителей (сотрудников)
                var executors = context.Staff
                    .Include(s => s.Users)
                    .Select(s => new { s.Users.Id, s.Users.FullName })
                    .ToList();

                ExecutorComboBox.ItemsSource = executors;
                ExecutorComboBox.DisplayMemberPath = "FullName";
                ExecutorComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке фильтров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters(object sender, RoutedEventArgs e)
        {
            try
            {
                using var context = DatabaseContext.GetContext();

                var filtered = context.Service
                    .Include(s => s.Staff)
                    .ThenInclude(st => st.Users)
                    .AsQueryable();

                // Фильтрация по поисковой строке
                if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    var searchText = SearchTextBox.Text.ToLower();
                    filtered = filtered.Where(s => s.Title.ToLower().Contains(searchText));
                }

                // Фильтрация по исполнителю
                if (ExecutorComboBox.SelectedValue != null && int.TryParse(ExecutorComboBox.SelectedValue.ToString(), out int executorId))
                {
                    filtered = filtered.Where(s => s.Staff.UserId == executorId);
                }

                // Фильтрация по цене "от"
                if (int.TryParse(PriceFromTextBox.Text, out int minPrice))
                {
                    filtered = filtered.Where(s => s.Price >= minPrice);
                }

                // Фильтрация по цене "до"
                if (int.TryParse(PriceToTextBox.Text, out int maxPrice))
                {
                    filtered = filtered.Where(s => s.Price <= maxPrice);
                }

                // Проецируем результаты в анонимный тип для отображения
                var result = filtered.AsEnumerable()
                    .Select(s => new
                    {
                        s.Id,
                        s.Title,
                        s.Price,
                        ExecutorId = s.Staff?.Users?.Id,
                        ExecutorFullName = s.Staff?.Users?.FullName ?? "Не назначен"
                    })
                    .OrderBy(s => s.Title)
                    .ToList();

                DataGrid.ItemsSource = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фильтрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGrid.SelectedItem == null)
            {
                _selectedService = null;
                return;
            }

            try
            {
                var selectedItem = DataGrid.SelectedItem;
                var idProperty = selectedItem.GetType().GetProperty("Id");
                int serviceId = (int)idProperty.GetValue(selectedItem);

                using var context = DatabaseContext.GetContext();

                _selectedService = context.Service
                    .Include(s => s.Staff)
                    .ThenInclude(st => st.Users)
                    .FirstOrDefault(s => s.Id == serviceId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выборе услуги: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _selectedService = null;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddEditServiceWindow();
                if (addWindow.ShowDialog() == true)
                {
                    LoadServices();
                    MessageBox.Show("Услуга успешно добавлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении услуги: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedService == null)
            {
                MessageBox.Show("Пожалуйста, выберите услугу для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var serviceToEdit = new Service
                {
                    Id = _selectedService.Id,
                    Title = _selectedService.Title,
                    Price = _selectedService.Price,
                    StaffId = _selectedService.StaffId
                };

                var editWindow = new AddEditServiceWindow(serviceToEdit);
                if (editWindow.ShowDialog() == true)
                {
                    using var context = DatabaseContext.GetContext();
                    context.Entry(_selectedService).CurrentValues.SetValues(serviceToEdit);
                    context.SaveChanges();

                    LoadServices();
                    MessageBox.Show("Услуга успешно изменена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании услуги: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedService == null)
            {
                MessageBox.Show("Пожалуйста, выберите услугу для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Вы уверены, что хотите удалить выбранную услугу?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = DatabaseContext.GetContext())
                    {
                        var service = context.Service.Find(_selectedService.Id);
                        if (service != null)
                        {
                            context.Service.Remove(service);
                            context.SaveChanges();
                        }
                    }
                    _selectedService = null;
                    LoadServices();
                    MessageBox.Show("Услуга успешно удалена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении услуги: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (ExecutorComboBox.SelectedValue == null)
            {
                MessageBox.Show("Пожалуйста, выберите исполнителя для печати прайс-листа", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int executorId = (int)ExecutorComboBox.SelectedValue;
                string executorName = ((dynamic)ExecutorComboBox.SelectedItem).FullName;

                // Создаем диалог сохранения файла
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Word Documents (*.docx)|*.docx",
                    FileName = $"Прайс-лист_{executorName}_{DateTime.Now:yyyyMMdd}.docx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Генерируем документ
                    GeneratePriceListDocument(executorId, executorName, saveFileDialog.FileName);

                    // Открываем документ
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveFileDialog.FileName,
                        UseShellExecute = true
                    });

                    MessageBox.Show("Прайс-лист успешно создан!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании прайс-листа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GeneratePriceListDocument(int executorId, string executorName, string filePath)
        {
            try
            {
                using (var context = DatabaseContext.GetContext())
                {
                    var services = context.Service
                        .Include(s => s.Staff)
                        .ThenInclude(st => st.Users)
                        .Where(s => s.Staff.UserId == executorId)
                        .OrderBy(s => s.Title)
                        .ToList();

                    using (WordprocessingDocument wordDocument =
                        WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
                    {
                        MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                        mainPart.Document = new Document();
                        Body body = mainPart.Document.AppendChild(new Body());

                        // Настройки страницы
                        SectionProperties sectionProps = new SectionProperties();
                        PageSize pageSize = new PageSize() { Width = 11906U, Height = 16838U }; // A4
                        PageMargin pageMargin = new PageMargin()
                        {
                            Top = 1701,
                            Right = 850,
                            Bottom = 1701,
                            Left = 1701,
                            Header = 851,
                            Footer = 992,
                            Gutter = 0
                        };
                        sectionProps.Append(pageSize, pageMargin);
                        body.Append(sectionProps);

                        // Шапка документа
                        Paragraph header = new Paragraph(
                            new ParagraphProperties(
                                new Justification() { Val = JustificationValues.Center }),
                            new Run(
                                new RunProperties(
                                    new RunFonts() { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                                    new FontSize() { Val = "28" },
                                    new Bold()),
                                new Text($"ПРАЙС-ЛИСТ УСЛУГ")));
                        body.Append(header);

                        // Исполнитель
                        Paragraph executorParagraph = new Paragraph(
                            new ParagraphProperties(
                                new Justification() { Val = JustificationValues.Center }),
                            new Run(
                                new RunProperties(
                                    new RunFonts() { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                                    new FontSize() { Val = "22" }),
                                new Text($"Исполнитель: {executorName}")));
                        body.Append(executorParagraph);

                        // Дата
                        Paragraph dateParagraph = new Paragraph(
                            new ParagraphProperties(
                                new Justification() { Val = JustificationValues.Center }),
                            new Run(
                                new RunProperties(
                                    new RunFonts() { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                                    new FontSize() { Val = "22" }),
                                new Text($"Дата: {DateTime.Now.ToString("dd.MM.yyyy")}")));
                        body.Append(dateParagraph);
                        body.Append(new Paragraph(new Run(new Break()))); // Пустая строка

                        // Таблица с услугами
                        Table table = new Table();
                        TableProperties tblProps = new TableProperties(
                            new TableBorders(
                                new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                                new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 }
                            ),
                            new TableWidth() { Width = "100%", Type = TableWidthUnitValues.Pct }
                        );
                        table.AppendChild(tblProps);

                        // Заголовки таблицы
                        TableRow headerRow = new TableRow();
                        headerRow.Append(
                            CreateTableCell("№ п/п", true, "20%"),
                            CreateTableCell("Наименование услуги", true, "60%"),
                            CreateTableCell("Цена, руб.", true, "20%")
                        );
                        table.Append(headerRow);

                        // Строки с услугами
                        int index = 1;
                        foreach (var service in services)
                        {
                            TableRow row = new TableRow();
                            row.Append(
                                CreateTableCell(index.ToString(), false, "20%"),
                                CreateTableCell(service.Title, false, "60%"),
                                CreateTableCell(service.Price.ToString("N0"), false, "20%")
                            );
                            table.Append(row);
                            index++;
                        }

                        body.Append(table);
                        body.Append(new Paragraph(new Run(new Break()))); // Пустая строка

                        // Итого
                        Paragraph totalParagraph = new Paragraph(
                            new ParagraphProperties(
                                new Justification() { Val = JustificationValues.Right }),
                            new Run(
                                new RunProperties(
                                    new RunFonts() { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                                    new FontSize() { Val = "22" },
                                    new Bold()),
                                new Text($"Итого услуг: {services.Count}")));
                        body.Append(totalParagraph);

                        // Подпись
                        Paragraph signatureParagraph = new Paragraph(
                            new ParagraphProperties(
                                new Justification() { Val = JustificationValues.Right }),
                            new Run(
                                new RunProperties(
                                    new RunFonts() { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                                    new FontSize() { Val = "22" }),
                                new Text($"\n\nОтветственный: _________________ / {executorName} /")));
                        body.Append(signatureParagraph);

                        mainPart.Document.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при создании прайс-листа: " + ex.Message);
            }
        }

        private TableCell CreateTableCell(string text, bool isHeader, string width)
        {
            TableCell cell = new TableCell();
            cell.Append(new TableCellProperties(new TableCellWidth() { Width = width }));

            Paragraph paragraph = new Paragraph();
            Run run = new Run();
            RunProperties runProps = new RunProperties();

            runProps.Append(new RunFonts() { Ascii = "Times New Roman", HighAnsi = "Times New Roman" });
            runProps.Append(new FontSize() { Val = isHeader ? "22" : "20" });

            if (isHeader)
            {
                runProps.Append(new Bold());
            }

            run.Append(runProps);
            run.Append(new Text(text));
            paragraph.Append(run);

            if (isHeader)
            {
                paragraph.ParagraphProperties = new ParagraphProperties(
                    new Justification() { Val = JustificationValues.Center });
            }

            cell.Append(paragraph);
            return cell;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}