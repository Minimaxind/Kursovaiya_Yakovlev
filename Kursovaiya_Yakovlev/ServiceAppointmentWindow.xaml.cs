using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace Kursovaiya_Yakovlev
{
    public partial class ServiceAppointmentWindow : Window
    {
        public ServiceAppointmentWindow()
        {
            InitializeComponent();
            LoadData();
            Title = "Запись на услугу";

            // Устанавливаем текущего пользователя как клиента
            if (UserSession.UserId > 0)
            {
                ClientTextBlock.Text = $"{UserSession.LastName} {UserSession.FirstName}";
            }
        }

        private void LoadData()
        {
            try
            {
                using var context = DatabaseContext.GetContext();

                // Загрузка специалистов
                var staffMembers = context.Staff
                    .Include(s => s.Users)
                    .ToList();

                StaffComboBox.ItemsSource = staffMembers;
                StaffComboBox.DisplayMemberPath = "Users.FullName";
                StaffComboBox.SelectedValuePath = "Id";

                // Загрузка объектов недвижимости
                var properties = context.HouseData.ToList();
                PropertyComboBox.ItemsSource = properties;
                PropertyComboBox.DisplayMemberPath = "Title";
                PropertyComboBox.SelectedValuePath = "Id";

                // Установка текущей даты и времени
                DatePicker.SelectedDate = DateTime.Now;
                TimeComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StaffComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (StaffComboBox.SelectedValue is int staffId)
            {
                LoadServicesForStaff(staffId);
            }
        }

        private void LoadServicesForStaff(int staffId)
        {
            try
            {
                using var context = DatabaseContext.GetContext();

                var services = context.Service
                    .Where(s => s.StaffId == staffId)
                    .ToList();

                ServiceComboBox.ItemsSource = services;
                ServiceComboBox.DisplayMemberPath = "Title";
                ServiceComboBox.SelectedValuePath = "Id";

                ServiceComboBox.SelectionChanged += ServiceComboBox_SelectionChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке услуг: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ServiceComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ServiceComboBox.SelectedItem is Service selectedService)
            {
                PriceTextBlock.Text = $"{selectedService.Price} руб.";
                // Устанавливаем цену услуги как сумму транзакции
                AmountTextBlock.Text = $"{selectedService.Price} руб.";
            }
            else
            {
                PriceTextBlock.Text = "";
                AmountTextBlock.Text = "";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем, что пользователь авторизован
                if (UserSession.UserId <= 0)
                {
                    MessageBox.Show("Для записи на услугу необходимо авторизоваться", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Валидация данных
                if (StaffComboBox.SelectedValue == null ||
                    ServiceComboBox.SelectedValue == null ||
                    PropertyComboBox.SelectedValue == null ||
                    DatePicker.SelectedDate == null ||
                    TimeComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, заполните все обязательные поля", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Получаем выбранное время
                var timeStr = (TimeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (!TimeSpan.TryParse(timeStr, out var time))
                {
                    MessageBox.Show("Некорректное время", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создаем дату выполнения услуги
                var serviceDate = DatePicker.SelectedDate.Value.Date.Add(time);

                using (var context = DatabaseContext.GetContext())
                {
                    // Получаем выбранную услугу для цены
                    var selectedService = context.Service.Find((int)ServiceComboBox.SelectedValue);
                    if (selectedService == null)
                    {
                        MessageBox.Show("Выбранная услуга не найдена", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Получаем владельца недвижимости
                    var property = context.HouseData
                        .Include(p => p.Transactions)
                        .FirstOrDefault(p => p.Id == (int)PropertyComboBox.SelectedValue);

                    if (property == null)
                    {
                        MessageBox.Show("Выбранный объект недвижимости не найден", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Создаем новую транзакцию
                    var transaction = new Transactions
                    {
                        PropertyId = property.Id,
                        ServiceId = selectedService.Id,
                        OwnerId = property.Transactions.FirstOrDefault()?.OwnerId ?? 1, // Или другой способ получить владельца
                        ClientId = UserSession.UserId,
                        TransactionDate = serviceDate,
                        Amount = selectedService.Price,
                        StatusId = 1 // Установите соответствующий начальный статус
                    };

                    context.Transactions.Add(transaction);
                    context.SaveChanges();
                }

                MessageBox.Show("Запись на услугу успешно сохранена", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += "\nInner Exception: " + ex.InnerException.Message;
                }

                MessageBox.Show($"Ошибка при сохранении записи: {errorMessage}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}