using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace Kursovaiya_Yakovlev
{
    public partial class AddEditContractWindow : Window
    {
        private Transactions _transaction;
        private bool _isEditMode;

        public AddEditContractWindow()
        {
            InitializeComponent();
            _isEditMode = false;
            _transaction = new Transactions { TransactionDate = DateTime.Now };
            LoadData();
            Title = "Добавление нового контракта";
        }

        public AddEditContractWindow(Transactions transactionToEdit)
        {
            InitializeComponent();
            _isEditMode = true;
            _transaction = transactionToEdit;
            LoadData();
            Title = "Редактирование контракта";
        }

        private void LoadData()
        {
            try
            {
                using var context = DatabaseContext.GetContext();

                // Загрузка данных для ComboBox
                var services = context.Service.ToList();
                ServiceComboBox.ItemsSource = services;
                ServiceComboBox.DisplayMemberPath = "Title";
                ServiceComboBox.SelectedValuePath = "Id";

                var properties = context.HouseData.ToList();
                PropertyComboBox.ItemsSource = properties;
                PropertyComboBox.DisplayMemberPath = "Title";
                PropertyComboBox.SelectedValuePath = "Id";

                var owners = context.Users.ToList(); // Предполагаем, что роль 3 - владельцы
                OwnerComboBox.ItemsSource = owners;
                OwnerComboBox.DisplayMemberPath = "FullName";
                OwnerComboBox.SelectedValuePath = "Id";

                var clients = context.Users.ToList(); // Предполагаем, что роль 2 - клиенты
                ClientComboBox.ItemsSource = clients;
                ClientComboBox.DisplayMemberPath = "FullName";
                ClientComboBox.SelectedValuePath = "Id";

                var statuses = context.Status.ToList();
                StatusComboBox.ItemsSource = statuses;
                StatusComboBox.DisplayMemberPath = "Title";
                StatusComboBox.SelectedValuePath = "Id";

                // Если режим редактирования - заполняем поля
                if (_isEditMode)
                {
                    ServiceComboBox.SelectedValue = _transaction.ServiceId;
                    PropertyComboBox.SelectedValue = _transaction.PropertyId;
                    OwnerComboBox.SelectedValue = _transaction.OwnerId;
                    ClientComboBox.SelectedValue = _transaction.ClientId;
                    AmountTextBox.Text = _transaction.Amount.ToString();
                    DatePicker.SelectedDate = _transaction.TransactionDate;
                    StatusComboBox.SelectedValue = _transaction.StatusId;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация данных
                if (ServiceComboBox.SelectedValue == null ||
                    PropertyComboBox.SelectedValue == null ||
                    OwnerComboBox.SelectedValue == null ||
                    ClientComboBox.SelectedValue == null ||
                    StatusComboBox.SelectedValue == null ||
                    !decimal.TryParse(AmountTextBox.Text, out decimal amount) ||
                    DatePicker.SelectedDate == null)
                {
                    MessageBox.Show("Пожалуйста, заполните все поля корректно", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Заполняем данные транзакции
                _transaction.ServiceId = (int)ServiceComboBox.SelectedValue;
                _transaction.PropertyId = (int)PropertyComboBox.SelectedValue;
                _transaction.OwnerId = (int)OwnerComboBox.SelectedValue;
                _transaction.ClientId = (int)ClientComboBox.SelectedValue;
                _transaction.Amount = amount;
                _transaction.TransactionDate = DateTime.SpecifyKind(DatePicker.SelectedDate.Value, DateTimeKind.Utc);
                _transaction.StatusId = (int)StatusComboBox.SelectedValue;

                using (var context = DatabaseContext.GetContext())
                {
                    if (_isEditMode)
                    {
                        context.Transactions.Update(_transaction);
                    }
                    else
                    {
                        context.Transactions.Add(_transaction);
                    }
                    context.SaveChanges();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                string errorDetails = ex.Message;
                if (ex.InnerException != null)
                {
                    errorDetails += "\nInner Exception: " + ex.InnerException.Message;
                }
                MessageBox.Show($"Ошибка при сохранении контракта: {errorDetails}", "Ошибка",
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