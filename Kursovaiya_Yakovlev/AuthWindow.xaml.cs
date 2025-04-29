using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace Kursovaiya_Yakovlev
{
    public partial class AuthWindow : Window
    {
        private DatabaseContext _dbContext;
        public AuthWindow()
        {

            InitializeComponent();
            _dbContext = new DatabaseContext();

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordPlaceholder.Visibility = string.IsNullOrEmpty(PasswordBox.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        private void AuthorizationButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Пожалуйста, запомните все поля.");
                return;
            }

            var user = _dbContext.users.FirstOrDefault(s => s.email == email && s.password == password);
            if (user != null)
            {
                MessageBox.Show($"Добро пожаловать, {user.firstName} {user.lastName}!");
            }
            else
            {
                MessageBox.Show("Неверный email или пароль");
            }
        }
        private void RegistrationButton_Click(object sender, RoutedEventArgs e)
        {
            RegWindow regWindow = new RegWindow();
            regWindow.Show();
            this.Hide();
        }
    }
}