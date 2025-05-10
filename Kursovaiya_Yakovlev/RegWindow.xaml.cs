using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Kursovaiya_Yakovlev
{
    public partial class RegWindow : Window
    {
        private readonly DatabaseContext _dbContext;

        public RegWindow()
        {
            InitializeComponent();

            try
            {
                // Инициализация контекста базы данных
                _dbContext = new DatabaseContext();

                // Проверка подключения к базе данных
                if (!_dbContext.Database.CanConnect())
                {
                    MessageBox.Show("Не удалось подключиться к базе данных", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации базы данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка инициализации контекста
                if (_dbContext == null)
                {
                    MessageBox.Show("Ошибка подключения к базе данных", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Валидация обязательных полей
                if (!ValidateRequiredFields())
                    return;

                // Получаем значения полей с проверкой на null
                string firstName = FirstNameBox.Text?.Trim() ?? string.Empty;
                string lastName = LastNameBox.Text?.Trim() ?? string.Empty;
                string middleName = MiddleNameBox.Text?.Trim() ?? string.Empty;
                string email = EmailBox.Text?.Trim() ?? string.Empty;
                string password = PasswordBox.Password ?? string.Empty;
                string passport = PassportBox.Text?.Trim() ?? string.Empty;
                string phone = PhoneBox.Text?.Trim() ?? string.Empty;

                // Валидация email
                if (!ValidateEmail(email))
                {
                    MessageBox.Show("Введите корректный email адрес!", "Ошибка валидации",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

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

                // Проверка уникальности email
                if (await _dbContext.Users.AnyAsync(u => u.email == email))
                {
                    MessageBox.Show("Пользователь с таким email уже существует!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newUser = new Users
                {
                    firstName = firstName,
                    lastName = lastName,
                    surname = middleName,
                    email = email.ToLower(),
                    password = password, // Пароль сохраняется в открытом виде
                    passportNumber = passport,
                    phone = NormalizePhone(phone),
                    AccessR = 3, // Права доступа по умолчанию
                    createdAt = DateTime.UtcNow
                };

                // Добавление пользователя в базу данных
                _dbContext.Users.Add(newUser);
                await _dbContext.SaveChangesAsync();

                MessageBox.Show("Регистрация прошла успешно!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Переход на окно авторизации
                AuthWindow authWindow = new AuthWindow();
                authWindow.Show();
                this.Close();
            }
            catch (DbUpdateException dbEx)
            {
                MessageBox.Show($"Ошибка базы данных: {dbEx.InnerException?.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

            if (string.IsNullOrWhiteSpace(EmailBox.Text))
            {
                ShowValidationError("Введите email", EmailBox);
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

        private bool ValidateEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }

        private bool ValidatePassword(string password)
        {
            // Минимум 8 символов, хотя бы одна цифра и одна заглавная буква
            var regex = new Regex(@"^(?=.*\d)(?=.*[A-Z]).{8,}$");
            return regex.IsMatch(password);
        }

        private bool ValidatePassport(string passport)
        {
            // Ровно 10 цифр (серия и номер без пробелов)
            return Regex.IsMatch(passport, @"^\d{10}$");
        }

        private bool ValidatePhone(string phone)
        {
            // Форматы: +79991234567 или 89991234567
            return Regex.IsMatch(phone, @"^(\+7|8)\d{10}$");
        }

        private string NormalizePhone(string phone)
        {
            // Приводим телефон к формату +7XXXXXXXXXX
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
            PasswordPlaceholder.Visibility = string.IsNullOrEmpty(PasswordBox.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            AuthWindow authWindow = new AuthWindow();
            authWindow.Show();
            this.Hide();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _dbContext?.Dispose();
        }
    }
}