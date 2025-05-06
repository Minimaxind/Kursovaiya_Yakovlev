using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Kursovaiya_Yakovlev
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        public MainWindow()
        {
            InitializeComponent();
            WelcomeText.Text = $"Здравствуйте, {UserSession.FirstName} {UserSession.LastName}";
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        
        private void ObjectsButton_Click(object sender, RoutedEventArgs e)
        {
            
        }
        private void ServicesButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ContractsButton_Click(object sender, RoutedEventArgs e)
        {
            ContractWindow contractWindow = new ContractWindow();
            contractWindow.Show();
            this.Close();
        }
        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
