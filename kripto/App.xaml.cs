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
                System.Diagnostics.Debug.WriteLine("=== Kripto Messenger dasturi ishga tushmoqda ===");

                base.OnStartup(e);

                // ShutdownMode ni o'zgartirish
                this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // Global exception handler
                this.DispatcherUnhandledException += App_DispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                // Input window yaratish va ko'rsatish
                System.Diagnostics.Debug.WriteLine("🔧 InputWindow yaratilmoqda...");
                var inputWindow = new InputWindow();

                System.Diagnostics.Debug.WriteLine("📱 InputWindow.ShowDialog() chaqirilmoqda...");
                bool? dialogResult = inputWindow.ShowDialog();

                System.Diagnostics.Debug.WriteLine($"📋 Dialog result: {dialogResult}");

                if (dialogResult == true)
                {
                    // Ma'lumotlarni olish va validatsiya
                    string ipAddress = inputWindow.IpAddressText?.Trim() ?? string.Empty;
                    string password = inputWindow.Password?.Trim() ?? string.Empty;

                    if (string.IsNullOrEmpty(ipAddress) || string.IsNullOrEmpty(password))
                    {
                        MessageBox.Show("IP Address yoki Password bo'sh. Dastur yopiladi.",
                            "Ma'lumot etishmayapti", MessageBoxButton.OK, MessageBoxImage.Warning);
                        this.Shutdown();
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"📡 Connection: IP={ipAddress}, Password={new string('*', password.Length)}");

                    // Main window yaratish
                    System.Diagnostics.Debug.WriteLine("🏠 MainWindow yaratilmoqda...");
                    var mainWindow = new MainWindow();

                    // Ma'lumotlarni MainWindow ga uzatish
                    mainWindow.SetConnectionInfo(ipAddress, password);

                    // Main window'ni asosiy window qilib belgilash
                    this.MainWindow = mainWindow;

                    // ShutdownMode ni qaytarish
                    this.ShutdownMode = ShutdownMode.OnMainWindowClose;

                    System.Diagnostics.Debug.WriteLine("🚀 MainWindow.Show() chaqirilmoqda...");
                    mainWindow.Show();

                    // Window closed event handler
                    mainWindow.Closed += MainWindow_Closed;

                    System.Diagnostics.Debug.WriteLine("✅ Dastur muvaffaqiyatli ishga tushdi");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ Foydalanuvchi dialog'ni bekor qildi");
                    MessageBox.Show("Dastur yopilmoqda - kerakli ma'lumotlar kiritilmadi.",
                        "Dastur yopilmoqda", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Shutdown();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 OnStartup kritik xatolik: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                MessageBox.Show($"Dastur ishga tushirishda kritik xatolik:\n\n{ex.Message}\n\nDastur yopiladi.",
                    "Kritik xatolik", MessageBoxButton.OK, MessageBoxImage.Error);

                try
                {
                    this.Shutdown();
                }
                catch
                {
                    Environment.Exit(-1);
                }
            }
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🏠 MainWindow yopildi - dastur tugaydi");
                this.Shutdown();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow_Closed xatolik: {ex.Message}");
                Environment.Exit(0);
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"💥 UI Thread exception: {e.Exception.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {e.Exception.StackTrace}");

                string errorMessage = $"UI xatoligi:\n\n{e.Exception.Message}\n\nDastur davom etishga harakat qiladi.";

                MessageBox.Show(errorMessage, "UI Xatoligi", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exception'ni handle qilindi
                e.Handled = true;
            }
            catch
            {
                Environment.Exit(-1);
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                System.Diagnostics.Debug.WriteLine($"💥 Unhandled domain exception: {exception?.Message}");
                System.Diagnostics.Debug.WriteLine($"Is terminating: {e.IsTerminating}");

                if (exception != null)
                {
                    string errorMessage = $"Kutilmagan xatolik:\n\n{exception.Message}\n\nDastur yopilishi mumkin.";

                    // UI thread'da bo'lsak MessageBox ko'rsatish
                    if (Application.Current?.Dispatcher?.CheckAccess() == true)
                    {
                        MessageBox.Show(errorMessage, "Kritik xatolik", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch
            {
                // Final fallback
                Environment.Exit(-1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔚 Dastur yopilmoqda - cleanup...");

                // Global cleanup
                base.OnExit(e);

                System.Diagnostics.Debug.WriteLine("✅ Dastur muvaffaqiyatli yopildi");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnExit xatolik: {ex.Message}");
            }
        }
    }
}