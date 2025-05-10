using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Kursovaiya_Yakovlev
{
    public partial class ChngWindow : Window
    {
        private readonly DatabaseContext _dbContext;
        private Users _currentUser;

        public ChngWindow()
        {
            InitializeComponent();

            try
            {
                _dbContext = new DatabaseContext();

                // Загружаем данные текущего пользователя из базы
                _currentUser = _dbContext.Users.FirstOrDefault(u => u.Id == UserSession.UserId);
                if (_currentUser == null)
                {
                    MessageBox.Show("Пользователь не найден в базе данных", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // Заполняем поля данными пользователя
                LoadUserData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void LoadUserData()
        {
            FirstNameBox.Text = _currentUser.firstName;
            LastNameBox.Text = _currentUser.lastName;
            MiddleNameBox.Text = _currentUser.surname;
            PasswordBox.Password = _currentUser.password;
            PassportBox.Text = _currentUser.passportNumber;
            PhoneBox.Text = _currentUser.phone;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация обязательных полей
                if (!ValidateRequiredFields())
                    return;

                // Получаем значения полей с проверкой на null
                string firstName = FirstNameBox.Text?.Trim() ?? string.Empty;
                string lastName = LastNameBox.Text?.Trim() ?? string.Empty;
                string middleName = MiddleNameBox.Text?.Trim() ?? string.Empty;
                string password = PasswordBox.Password ?? string.Empty;
                string passport = PassportBox.Text?.Trim() ?? string.Empty;
                string phone = PhoneBox.Text?.Trim() ?? string.Empty;

                // Валидация пароля
                if (!ValidatePassword(password))
                {
                    MessageBox.Show("Пароль должен содержать:\n- Минимум 8 символов\n- Хотя бы одну цифру\n- Хотя бы одну заглавную букву",
                        "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Валидация паспорта
                if (!ValidatePassport(passport))
                {
                    MessageBox.Show("Паспорт должен содержать 10 цифр (серия и номер без пробелов)",
                        "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Валидация телефона
                if (!ValidatePhone(phone))
                {
                    MessageBox.Show("Введите корректный номер телефона (например: +79991234567 или 89991234567)",
                        "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Обновляем данные пользователя
                _currentUser.firstName = firstName;
                _currentUser.lastName = lastName;
                _currentUser.surname = middleName;
                _currentUser.password = password;
                _currentUser.passportNumber = passport;
                _currentUser.phone = NormalizePhone(phone);
                _currentUser.updatedAt = DateTime.UtcNow;

                // Сохраняем изменения
                await _dbContext.SaveChangesAsync();

                // Обновляем данные в UserSession
                UpdateUserSession();

                MessageBox.Show("Данные успешно обновлены!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (DbUpdateException dbEx)
            {
                MessageBox.Show($"Ошибка базы данных: {dbEx.InnerException?.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUserSession()
        {
            UserSession.FirstName = _currentUser.firstName;
            UserSession.LastName = _currentUser.lastName;
            UserSession.accessR = _currentUser.AccessR;
        }

        #region Методы валидации

        private bool ValidateRequiredFields()
        {
            if (string.IsNullOrWhiteSpace(FirstNameBox.Text))
            {
                ShowValidationError("Введите имя", FirstNameBox);
                return false;
            }

            if (string.IsNullOrWhiteSpace(LastNameBox.Text))
            {
                ShowValidationError("Введите фамилию", LastNameBox);
                return false;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                ShowValidationError("Введите пароль", PasswordBox);
                return false;
            }

            if (string.IsNullOrWhiteSpace(PassportBox.Text))
            {
                ShowValidationError("Введите паспортные данные", PassportBox);
                return false;
            }

            if (string.IsNullOrWhiteSpace(PhoneBox.Text))
            {
                ShowValidationError("Введите номер телефона", PhoneBox);
                return false;
            }

            return true;
        }

        private bool ValidatePassword(string password)
        {
            var regex = new Regex(@"^(?=.*\d)(?=.*[A-Z]).{8,}$");
            return regex.IsMatch(password);
        }

        private bool ValidatePassport(string passport)
        {
            return Regex.IsMatch(passport, @"^\d{10}$");
        }

        private bool ValidatePhone(string phone)
        {
            return Regex.IsMatch(phone, @"^(\+7|8)\d{10}$");
        }

        private string NormalizePhone(string phone)
        {
            return phone.StartsWith("8") ? "+7" + phone.Substring(1) : phone;
        }

        private void ShowValidationError(string message, Control control)
        {
            MessageBox.Show(message, "Ошибка валидации",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            control.Focus();
        }

        #endregion

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Обновляем видимость плейсхолдера
            var placeholder = ((Grid)PasswordBox.Parent).Children
                .OfType<TextBlock>()
                .FirstOrDefault();

            if (placeholder != null)
            {
                placeholder.Visibility = string.IsNullOrEmpty(PasswordBox.Password)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Возвращаемся на предыдущее окно
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _dbContext?.Dispose();
        }
    }
}