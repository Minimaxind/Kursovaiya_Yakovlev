using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Kursovaiya_Yakovlev
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            WelcomeText.Text = $"Здравствуйте, {UserSession.FirstName} {UserSession.LastName}";
            InitializeMenu();
        }

        private void InitializeMenu()
        {
            MenuPanel.Children.Clear();
            switch (UserSession.accessR)
            {
                case 1: // Администратор
                    AddMenuButton("Доступные объекты", ObjectsButton_Click);
                    AddMenuButton("Услуги", ServicesButton_Click);
                    AddMenuButton("Контракты", ContractsButton_Click);
                    AddMenuButton("Пользователи", UsersButton_Click);
                    break;
                case 2: // Менеджер
                    AddMenuButton("Доступные объекты", ObjectsButton_Click);
                    AddMenuButton("Услуги", ServicesButton_Click);
                    AddMenuButton("Контракты", ContractsButton_Click);
                    break;
                case 3: // Клиент
                    AddMenuButton("Доступные объекты", ObjectsButton_Click);
                    AddMenuButton("Запись на услуги", AddServicesButton_Click);
                    AddMenuButton("Личные данные", PersonalDataButton_Click);
                    break;
                    
                case 4: // Риелтор
                    AddMenuButton("Доступные объекты", ObjectsButton_Click);
                    AddMenuButton("Услуги", ServicesButton_Click);
                    AddMenuButton("Контракты", ContractsButton_Click);
                    AddMenuButton("Личные данные", PersonalDataButton_Click);
                    break;
                default:
                    AddMenuButton("Личные данные", PersonalDataButton_Click);
                    break;
            }
        }

        private void AddMenuButton(string content, RoutedEventHandler clickHandler)
        {
            var button = new Button
            {
                Content = content,
                FontSize = 28,
                Margin = new Thickness(0, 0, 0, 20),
                Height = 60,
                Foreground = new SolidColorBrush(Colors.White)
            };

            button.Click += clickHandler;

            // Создаем ControlTemplate
            var template = new ControlTemplate(typeof(Button));

            // Создаем фабрику элементов для Border
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "Border";
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(13, 169, 96)));

            // Создаем фабрику элементов для ContentPresenter
            var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            // Добавляем ContentPresenter в Border
            borderFactory.AppendChild(contentPresenterFactory);

            // Устанавливаем визуальное дерево шаблона
            template.VisualTree = borderFactory;

            // Создаем триггеры
            var hoverTrigger = new Trigger
            {
                Property = UIElement.IsMouseOverProperty,
                Value = true,
                Setters = {
            new Setter(Border.BackgroundProperty,
                      new SolidColorBrush(Color.FromRgb(14, 186, 106)),
                      "Border")
        }
            };

            var pressedTrigger = new Trigger
            {
                Property = Button.IsPressedProperty,
                Value = true,
                Setters = {
            new Setter(Border.BackgroundProperty,
                      new SolidColorBrush(Color.FromRgb(12, 154, 80)),
                      "Border")
        }
            };

            // Добавляем триггеры в шаблон
            template.Triggers.Add(hoverTrigger);
            template.Triggers.Add(pressedTrigger);

            button.Template = template;
            MenuPanel.Children.Add(button);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ObjectsButton_Click(object sender, RoutedEventArgs e)
        {
            ObjectsWindow objectsWindow = new ObjectsWindow();
            objectsWindow.Show();
            this.Close();
        }

       

        private void ServicesButton_Click(object sender, RoutedEventArgs e)
        {
            ServiceWindow serviceWindow = new ServiceWindow();
            serviceWindow.Show();
            this.Close();
        }

        private void ContractsButton_Click(object sender, RoutedEventArgs e)
        {
            ContractWindow contractWindow = new ContractWindow();
            contractWindow.Show();
            this.Close();
        }

        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {
            UsersWindow usersWindow = new UsersWindow();
            usersWindow.Show();
            this.Close();
        }

        private void PersonalDataButton_Click(object sender, RoutedEventArgs e)
        {
            ChngWindow personalDataWindow = new ChngWindow();
            personalDataWindow.Show();
            this.Close();
        }

        private void AddServicesButton_Click(object sender, RoutedEventArgs e)
        {
            ServiceAppointmentWindow serviceAppointmentWindow = new ServiceAppointmentWindow();
            serviceAppointmentWindow.Show();
            
        }
    }
}
