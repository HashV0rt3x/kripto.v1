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

                // ShutdownMode ni o'zgartirish - dastur manual yopilguncha ishlaydi
                this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // Global exception handler qo'shish
                this.DispatcherUnhandledException += App_DispatcherUnhandledException;

                // Input window yaratish va ko'rsatish
                System.Diagnostics.Debug.WriteLine("🔧 InputWindow yaratilmoqda...");
                var inputWindow = new InputWindow();

                System.Diagnostics.Debug.WriteLine("📱 InputWindow.ShowDialog() chaqirilmoqda...");
                bool? dialogResult = inputWindow.ShowDialog();

                System.Diagnostics.Debug.WriteLine($"📋 Dialog result: {dialogResult}");

                if (dialogResult == true)
                {
                    // Ma'lumotlarni olish va validatsiya qilish
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

                    // ShutdownMode ni qaytarish - main window yopilganda dastur tugaydi
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
                System.Diagnostics.Debug.WriteLine($"💥 Global exception: {e.Exception.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {e.Exception.StackTrace}");

                string errorMessage = $"Kutilmagan xatolik yuz berdi:\n\n{e.Exception.Message}\n\nDastur davom etishga harakat qiladi.";

                MessageBox.Show(errorMessage, "Xatolik", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exception'ni handle qilindi deb belgilash
                e.Handled = true;
            }
            catch
            {
                // Agar global exception handler'da ham xatolik bo'lsa, dasturni majburiy yopish
                Environment.Exit(-1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔚 Dastur yopilmoqda - cleanup...");

                // Cleanup operations
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