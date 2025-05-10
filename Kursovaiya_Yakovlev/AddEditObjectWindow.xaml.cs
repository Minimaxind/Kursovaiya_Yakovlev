
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;

namespace Kursovaiya_Yakovlev
{
    public partial class AddEditObjectWindow : Window
    {
        private HouseData _houseData;
        private bool _isEditMode;
        private Dictionary<string, string> _imageUrls = new Dictionary<string, string>();

        public string WindowTitle => _isEditMode ? "Редактирование объекта" : "Добавление объекта";

        public AddEditObjectWindow()
        {
            InitializeComponent();
            _houseData = new HouseData();
            _isEditMode = false;
            DataContext = this;
            InitializeControls();
            InitializeEmptyAddress();
        }

        public AddEditObjectWindow(HouseData houseData, string addressJson = null)
        {
            InitializeComponent();
            _houseData = houseData;
            _isEditMode = true;
            DataContext = this;
            InitializeControls();
            LoadExistingData(addressJson);
        }

        private void InitializeControls()
        {
            try
            {
                using var context = DatabaseContext.GetContext();
                StatusComboBox.ItemsSource = context.Status.ToList();
                StatusComboBox.DisplayMemberPath = "Title";
                StatusComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                ShowExtendedErrorMessage("Ошибка загрузки статусов", ex);
            }
        }

        private void InitializeEmptyAddress()
        {
            _houseData.Address = "{}";
            ClearAddressFields();
        }

        private void ClearAddressFields()
        {
            ZipTextBox.Text = "";
            CityTextBox.Text = "";
            StreetTextBox.Text = "";
            HouseNumberTextBox.Text = "";
            ApartmentNumberTextBox.Text = "";
        }

        private void LoadExistingData(string addressJson = null)
        {
            TitleTextBox.Text = _houseData.Title;
            DescriptionTextBox.Text = _houseData.Description;
            PriceTextBox.Text = _houseData.Price.ToString("N0");

            // Set selected status
            StatusComboBox.SelectedValue = _houseData.StatusId;

            FloorTextBox.Text = _houseData.PropertyDetails.floor;
            SquareTextBox.Text = _houseData.PropertyDetails.square;
            RoomsTextBox.Text = _houseData.PropertyDetails.room_count;
            BalconyCheckBox.IsChecked = _houseData.PropertyDetails.balcony;
            ElevatorCheckBox.IsChecked = _houseData.PropertyDetails.elevator;

            if (!string.IsNullOrEmpty(addressJson))
            {
                _houseData.Address = addressJson;
            }
            UpdateAddressFields();

            if (!string.IsNullOrEmpty(_houseData.Images))
            {
                _imageUrls = JsonSerializer.Deserialize<Dictionary<string, string>>(_houseData.Images)
                           ?? new Dictionary<string, string>();
            }
            UpdateImagesList();
        }

        private void UpdateAddressFields()
        {
            try
            {
                if (!string.IsNullOrEmpty(_houseData.Address) && _houseData.Address != "{}")
                {
                    var address = JsonSerializer.Deserialize<AddressInfo>(_houseData.Address);
                    ZipTextBox.Text = address.zip;
                    CityTextBox.Text = address.city;
                    StreetTextBox.Text = address.street;
                    HouseNumberTextBox.Text = address.house_number;
                    ApartmentNumberTextBox.Text = address.apartment_number;
                }
            }
            catch
            {
                ClearAddressFields();
            }
        }

        private void UpdateImagesList()
        {
            ImagesListBox.Items.Clear();
            foreach (var image in _imageUrls)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(image.Value);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ImagesListBox.Items.Add(new ImageItem
                    {
                        Key = image.Key,
                        Image = bitmap,
                        Url = image.Value
                    });
                }
                catch
                {
                    ImagesListBox.Items.Add(new ImageItem
                    {
                        Key = image.Key,
                        Image = new BitmapImage(new Uri("pack://application:,,,/Resources/no_image.png")),
                        Url = image.Value
                    });
                }
            }
        }

        private void AddImageButton_Click(object sender, RoutedEventArgs e)
        {
            string url = ImageUrlTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Введите URL изображения", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                MessageBox.Show("Введите корректный URL", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var uri = new Uri(url);
                if (!uri.AbsolutePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) &&
                    !uri.AbsolutePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) &&
                    !uri.AbsolutePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("URL должен вести на изображение (jpg, jpeg, png)", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var key = $"url{_imageUrls.Count + 1}";
                _imageUrls.Add(key, url);
                UpdateImagesList();
                ImageUrlTextBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления изображения: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (ImagesListBox.SelectedItem is ImageItem selectedImage)
            {
                _imageUrls.Remove(selectedImage.Key);
                UpdateImagesList();
            }
            else
            {
                MessageBox.Show("Выберите изображение для удаления", "Внимание",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                using var context = DatabaseContext.GetContext();
                using var transaction = context.Database.BeginTransaction();

                try
                {
                    _houseData.Title = TitleTextBox.Text;
                    _houseData.Description = DescriptionTextBox.Text;
                    _houseData.Price = decimal.Parse(PriceTextBox.Text);
                    _houseData.StatusId = (int)StatusComboBox.SelectedValue;
                    _houseData.UpdatedAt = DateTime.Now;

                    // Изменения для работы с адресом
                    if (!string.IsNullOrWhiteSpace(CityTextBox.Text) &&
                        !string.IsNullOrWhiteSpace(StreetTextBox.Text) &&
                        !string.IsNullOrWhiteSpace(HouseNumberTextBox.Text))
                    {
                        _houseData.AddressData = new AddressInfo
                        {
                            zip = ZipTextBox.Text,
                            city = CityTextBox.Text,
                            street = StreetTextBox.Text,
                            house_number = HouseNumberTextBox.Text,
                            apartment_number = ApartmentNumberTextBox.Text
                        };
                    }
                    else
                    {
                        _houseData.Address = "{}"; // Пустой JSON объект
                    }

                    _houseData.PropertyDetails = new HouseData.PropertyDetailsData
                    {
                        floor = FloorTextBox.Text,
                        square = SquareTextBox.Text,
                        room_count = RoomsTextBox.Text,
                        balcony = BalconyCheckBox.IsChecked == true,
                        elevator = ElevatorCheckBox.IsChecked == true,
                        decoration = false,
                        living_area = ""
                    };

                    _houseData.Images = JsonSerializer.Serialize(_imageUrls);

                    if (_isEditMode)
                    {
                        context.HouseData.Update(_houseData);
                    }
                    else
                    {
                        _houseData.CreatedAt = DateTime.Now;
                        context.HouseData.Add(_houseData);
                    }

                    context.SaveChanges();
                    transaction.Commit();
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ShowExtendedErrorMessage("Ошибка сохранения", ex);
                }
            }
            catch (Exception ex)
            {
                ShowExtendedErrorMessage("Ошибка сохранения", ex);
            }
        }

        private void ShowExtendedErrorMessage(string title, Exception ex)
        {
            string message = $"{title}: {ex.Message}";
            if (ex.InnerException != null)
                message += $"\nВнутреннее исключение: {ex.InnerException.Message}";

            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Введите название объекта", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price <= 0)
            {
                MessageBox.Show("Введите корректную цену", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(FloorTextBox.Text) || !int.TryParse(FloorTextBox.Text, out _))
            {
                MessageBox.Show("Введите корректный этаж", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(SquareTextBox.Text) || !int.TryParse(SquareTextBox.Text, out _))
            {
                MessageBox.Show("Введите корректную площадь", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(RoomsTextBox.Text) || !int.TryParse(RoomsTextBox.Text, out _))
            {
                MessageBox.Show("Введите корректное количество комнат", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class ImageItem
    {
        public string Key { get; set; }
        public BitmapImage Image { get; set; }
        public string Url { get; set; }
    }
}