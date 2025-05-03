using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Windows.Media.Imaging;

namespace Kursovaiya_Yakovlev
{
    public partial class LoadWindow : Window
    {
        private readonly DatabaseContext _dbContext;

        public LoadWindow()
        {
            InitializeComponent();
            _dbContext = new DatabaseContext();
            Loaded += OnWindowLoaded;
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {

            await InitializeSystem();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


        private async Task InitializeSystem()
        {
            try
            {
                // Этап 1: Проверка подключения к БД
                StatusText.Text = "Проверка подключения к базе данных...";
                await CheckDatabaseConnection();

                // Этап 2: Загрузка начальных данных
                StatusText.Text = "Загрузка начальных данных...";
                await Task.Delay(6000); // Имитация загрузки

                // Этап 3: Инициализация системы
                StatusText.Text = "Завершение инициализации...";
                await Task.Delay(2000);

                // Открываем главное окно
                var authWindow = new AuthWindow();
                authWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                StatusText.Text = ex.Message;
                ProgressBar.IsIndeterminate = false;
            }
        }

        private async Task CheckDatabaseConnection()
        {
            try
            {
                // Проверяем, что можем подключиться к БД
                bool canConnect = await _dbContext.Database.CanConnectAsync();

                if (!canConnect)
                {
                    throw new Exception("Не удалось подключиться к базе данных");
                }

                // Дополнительная проверка - выполняем простой запрос
                var testQuery = await _dbContext.users.FirstOrDefaultAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("Ошибка обновления базы данных: " + ex.InnerException?.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка подключения: " + ex.Message);
            }
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Сбрасываем состояние
                ProgressBar.IsIndeterminate = true;
                StatusText.Text = "Повторная попытка подключения...";

                // Пытаемся подключиться снова
                await CheckDatabaseConnection();

                // Если успешно, продолжаем загрузку
                StatusText.Text = "Подключение восстановлено! Продолжение загрузки...";
                await InitializeSystem();
            }
            catch (Exception ex)
            {
                StatusText.Text = ex.Message;
                ProgressBar.IsIndeterminate = false;
            }
        }
    }
}