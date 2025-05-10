using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace Kursovaiya_Yakovlev
{
    public partial class AddEditServiceWindow : Window
    {
        private readonly Service _service;
        public string WindowTitle => _service.Id == 0 ? "Добавление услуги" : "Редактирование услуги";

        public AddEditServiceWindow(Service service = null)
        {
            InitializeComponent();

            _service = service ?? new Service();
            DataContext = this;
            LoadExecutors();
            LoadServiceData();
        }

        private void LoadExecutors()
        {
            try
            {
                using var context = DatabaseContext.GetContext();
                var executors = context.Staff
                    .Include(s => s.Users)
                    .Select(s => new { s.Id, s.Users.FullName })
                    .ToList();

                ExecutorComboBox.ItemsSource = executors;
                ExecutorComboBox.DisplayMemberPath = "FullName";
                ExecutorComboBox.SelectedValuePath = "Id";

                if (_service.StaffId > 0)
                {
                    ExecutorComboBox.SelectedValue = _service.StaffId;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке исполнителей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadServiceData()
        {
            TitleTextBox.Text = _service.Title;
            PriceTextBox.Text = _service.Price > 0 ? _service.Price.ToString() : "";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите название услуги", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(PriceTextBox.Text, out int price) || price <= 0)
            {
                MessageBox.Show("Пожалуйста, введите корректную цену (целое положительное число)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _service.Title = TitleTextBox.Text.Trim();
                _service.Price = price;

                if (ExecutorComboBox.SelectedValue != null && int.TryParse(ExecutorComboBox.SelectedValue.ToString(), out int staffId))
                {
                    _service.StaffId = staffId;
                }
                else
                {
                    _service.StaffId = 0;
                }

                using var context = DatabaseContext.GetContext();

                if (_service.Id == 0)
                {
                    context.Service.Add(_service);
                }
                else
                {
                    context.Service.Update(_service);
                }

                context.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении услуги: {ex.Message}", "Ошибка",
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