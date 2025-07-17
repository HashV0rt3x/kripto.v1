using kripto.Windows;
using System;
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
            try
            {
                System.Diagnostics.Debug.WriteLine("OnStartup boshlandi");

                base.OnStartup(e);

                // ShutdownMode ni o'zgartirish
                this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // Input window yaratish
                System.Diagnostics.Debug.WriteLine("InputWindow yaratilmoqda");
                var inputWindow = new InputWindow();
                System.Diagnostics.Debug.WriteLine("InputWindow yaratildi");

                System.Diagnostics.Debug.WriteLine("InputWindow.ShowDialog() chaqirilmoqda");
                bool? result = inputWindow.ShowDialog();
                System.Diagnostics.Debug.WriteLine($"Dialog result: {result}");

                if (result == true)
                {
                    // Ma'lumotlarni olish
                    string ipAddressText = inputWindow.IpAddressText;
                    string password = inputWindow.Password;

                    System.Diagnostics.Debug.WriteLine($"IP: {ipAddressText}, Password: {password}");

                    // Main window yaratish
                    System.Diagnostics.Debug.WriteLine("MainWindow yaratilmoqda");
                    var mainWindow = new MainWindow();
                    System.Diagnostics.Debug.WriteLine("MainWindow yaratildi");

                    // Ma'lumotlarni MainWindow ga uzatish
                    mainWindow.SetConnectionInfo(ipAddressText, password);

                    // Main window'ni asosiy window qilib belgilash
                    this.MainWindow = mainWindow;

                    // ShutdownMode ni qaytarish
                    this.ShutdownMode = ShutdownMode.OnMainWindowClose;

                    System.Diagnostics.Debug.WriteLine("MainWindow.Show() chaqirilmoqda");
                    mainWindow.Show();
                    System.Diagnostics.Debug.WriteLine("MainWindow ko'rsatildi");

                    // Closed event qo'shish
                    mainWindow.Closed += (s, args) => {
                        System.Diagnostics.Debug.WriteLine("MainWindow closed - dastur tugaydi");
                        this.Shutdown();
                    };
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Dialog bekor qilindi");
                    MessageBox.Show("Dasturdan chiqilmoqda, chunki kerakli ma'lumotlar kiritilmadi.",
                        "Dastur yopilmoqda", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Shutdown();
                }

                System.Diagnostics.Debug.WriteLine("OnStartup tugaydi");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnStartup da xatolik: {ex.Message}");
                MessageBox.Show($"Dastur ishga tushirishda xatolik: {ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Kritik xatolik", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Shutdown();
            }
        }
    }
}