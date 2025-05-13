using System.Configuration;
using System.Data;
using System.Windows;

namespace Kursovaiya_Yakovlev
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Глобальный обработчик ошибок
            DispatcherUnhandledException += (sender, args) =>
            {
                args.Handled = true;
            };
        }
    }

}
