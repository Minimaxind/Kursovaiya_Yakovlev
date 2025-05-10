using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace Kursovaiya_Yakovlev
{
    public partial class UsersWindow : Window
    {
        private Users _selectedUser;

        public UsersWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private System.Collections.Generic.List<Users> _allUsers; // Добавляем поле для хранения всех пользователей

        private async void LoadData()
        {
            try
            {
                using var context = DatabaseContext.GetContext();

                // Загрузка пользователей с включенными данными о правах доступа
                _allUsers = await context.Users
                    .Include(u => u.AccessRights)
                    .OrderBy(u => u.lastName)
                    .ThenBy(u => u.firstName)
                    .ToListAsync();

                UsersDataGrid.ItemsSource = _allUsers;

                // Загрузка прав доступа для фильтра
                var accessRights = await context.AccessRights.ToListAsync();
                RoleComboBox.ItemsSource = accessRights;

                // Назначаем обработчики событий
                SearchTextBox.TextChanged += SearchTextBox_TextChanged;
                RoleComboBox.SelectionChanged += RoleComboBox_SelectionChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (_allUsers == null) return;

            var filteredUsers = _allUsers.AsQueryable();

            // Фильтр по поисковой строке
            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                var searchText = SearchTextBox.Text.ToLower();
                filteredUsers = filteredUsers.Where(u =>
                    (u.lastName != null && u.lastName.ToLower().Contains(searchText)) ||
                    (u.firstName != null && u.firstName.ToLower().Contains(searchText)) ||
                    (u.surname != null && u.surname.ToLower().Contains(searchText)));
            }

            // Фильтр по роли
            if (RoleComboBox.SelectedValue != null && RoleComboBox.SelectedValue is int roleId)
            {
                filteredUsers = filteredUsers.Where(u => u.AccessR == roleId);
            }

            UsersDataGrid.ItemsSource = filteredUsers.ToList();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void UsersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedUser = UsersDataGrid.SelectedItem as Users;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddEditUserWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadData(); // Обновляем данные после добавления
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editWindow = new AddEditUserWindow(_selectedUser);
            if (editWindow.ShowDialog() == true)
            {
                LoadData(); // Обновляем данные после редактирования
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить пользователя {_selectedUser.FullName}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var context = DatabaseContext.GetContext();
                    context.Users.Remove(_selectedUser);
                    await context.SaveChangesAsync();
                    LoadData(); // Обновляем данные после удаления
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для сброса пароля", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Реализация сброса пароля
            MessageBox.Show($"Сброс пароля для пользователя {_selectedUser.FullName}",
                "Сброс пароля", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}