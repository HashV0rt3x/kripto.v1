using kripto.Windows;
using System.Configuration;
using System.Data;
using System.Windows;

namespace kripto
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var inputWindow = new InputWindow();
            var result = inputWindow.ShowDialog();

            if (result != true)
            {
                MessageBox.Show("Dasturdan chiqilmoqda, chunki kerakli ma'lumotlar kiritilmadi.");
                Application.Current.Shutdown(); // yoki Environment.Exit(0);
                return;
            }

            string name = inputWindow.NameInput;
            int age = inputWindow.AgeInput;
            string city = inputWindow.CityInput;

            // Endi main window ochiladi
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
