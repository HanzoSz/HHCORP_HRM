using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HHCORP_HRM
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Khởi động ứng dụng từ LoginWindow
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}
