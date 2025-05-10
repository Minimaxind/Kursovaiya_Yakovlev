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

            MessageBox.Show("Функция печати будет реализована в будущем", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
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