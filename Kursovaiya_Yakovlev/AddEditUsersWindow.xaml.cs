using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Kursovaiya_Yakovlev
{
    public partial class AddEditUserWindow : Window
    {
        private Users _user;
        private bool _isEditMode;

        public AddEditUserWindow()
        {
            InitializeComponent();
            _isEditMode = false;
            _user = new Users { createdAt = DateTime.Now };
            LoadData();
            TitleTextBlock.Text = "Добавление нового пользователя";
        }

        public AddEditUserWindow(Users userToEdit)
        {
            InitializeComponent();
            _isEditMode = true;
            _user = userToEdit;
            LoadData();
            TitleTextBlock.Text = "Редактирование пользователя";
        }

        private async void LoadData()
        {
            try
            {
                using var context = DatabaseContext.GetContext();

                // Загрузка прав доступа
                var accessRights = await context.AccessRights.ToListAsync();
                AccessRightsComboBox.ItemsSource = accessRights;
                AccessRightsComboBox.DisplayMemberPath = "Title";
                AccessRightsComboBox.SelectedValuePath = "Id";

                // Для режима редактирования - заполняем поля
                if (_isEditMode)
                {
                    EmailTextBox.Text = _user.email;
                    LastNameTextBox.Text = _user.lastName;
                    FirstNameTextBox.Text = _user.firstName;
                    SurnameTextBox.Text = _user.surname;
                    PassportTextBox.Text = _user.passportNumber;
                    PhoneTextBox.Text = _user.phone;

                    // Устанавливаем выбранную роль по ID
                    AccessRightsComboBox.SelectedValue = _user.AccessR;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка обязательных полей
                if (string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
                    string.IsNullOrWhiteSpace(LastNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(PassportTextBox.Text) ||
                    string.IsNullOrWhiteSpace(PhoneTextBox.Text) ||
                    AccessRightsComboBox.SelectedValue == null) // Проверяем, что роль выбрана
                {
                    MessageBox.Show("Заполните все обязательные поля", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Получаем выбранную роль (используем SelectedValue, так как SelectedValuePath="Id")
                if (!int.TryParse(AccessRightsComboBox.SelectedValue.ToString(), out int selectedRoleId))
                {
                    MessageBox.Show("Ошибка при выборе роли", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Обновляем данные пользователя
                _user.email = EmailTextBox.Text.Trim();
                _user.lastName = LastNameTextBox.Text.Trim();
                _user.firstName = FirstNameTextBox.Text.Trim();
                _user.surname = SurnameTextBox.Text.Trim();
                _user.passportNumber = PassportTextBox.Text.Trim();
                _user.phone = PhoneTextBox.Text.Trim();
                _user.AccessR = selectedRoleId; // Устанавливаем новую роль
                _user.updatedAt = DateTime.Now;

                // Если пароль изменён (для нового пользователя или при явном изменении)
                if (!_isEditMode || !string.IsNullOrEmpty(PasswordBox.Password))
                {
                    _user.password = PasswordBox.Password;
                }

                // Сохраняем в БД
                using (var context = DatabaseContext.GetContext())
                {
                    if (_isEditMode)
                    {
                        context.Users.Update(_user);
                    }
                    else
                    {
                        context.Users.Add(_user);
                    }
                    await context.SaveChangesAsync();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.InnerException?.Message ?? ex.Message}", "Ошибка",
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