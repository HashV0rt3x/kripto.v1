using kripto.Security;
using kripto.Windows;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace kripto
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Connection info properties
        private RutokenHelper? rutokenHelper;

        public string IpAddress { get; private set; }
        public string Password { get; private set; }

        public MainWindow()
        {
            // InputWindow kodini olib tashladik!

            try
            {
                

                System.Diagnostics.Debug.WriteLine("MainWindow constructor boshlandi");
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("MainWindow InitializeComponent tugadi");

                // Simple test
                this.Title = "Kripto Messenger - Connected";

                // Window events
                this.Loaded += MainWindow_Loaded;
                this.Closing += MainWindow_Closing;

                System.Diagnostics.Debug.WriteLine("MainWindow constructor tugadi");
                RutokenHelper rutokenHelper = new RutokenHelper();

                rutokenHelper.Initialize(Password);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow constructor xatolik: {ex.Message}");
                MessageBox.Show($"MainWindow yaratishda xatolik: {ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Xatolik", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // Connection info'ni o'rnatish uchun method
        public void SetConnectionInfo(string ipAddress, string password)
        {
            this.IpAddress = ipAddress;
            this.Password = password;

            System.Diagnostics.Debug.WriteLine($"Connection info set: {ipAddress}, {password}");

            // Title'ni yangilash
            this.Title = $"Kripto Messenger - Connected to {ipAddress}";
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MainWindow_Loaded chaqirildi");

            // Welcome message
            if (MessagesPanel != null)
            {
                AddWelcomeMessage();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MainWindow_Closing chaqirildi");

            // Confirmation dialog
            var result = MessageBox.Show("Dasturdan chiqishni xohlaysizmi?", "Tasdiqlash",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

        private void AddWelcomeMessage()
        {
            try
            {
                var welcomeMessage = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                    CornerRadius = new CornerRadius(8),
                    //Padding = new Thickness(16, 12),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    //Margin = new Thickness(0, 20)
                };

                var welcomeText = new TextBlock
                {
                    Text = $"Welcome to Kripto Messenger! 🔐\nConnected to: {IpAddress ?? "Unknown"}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(240, 246, 252)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };

                welcomeMessage.Child = welcomeText;
                MessagesPanel.Children.Add(welcomeMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddWelcomeMessage xatolik: {ex.Message}");
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("SendButton_Click chaqirildi");
                SendMessage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendButton_Click xatolik: {ex.Message}");
                MessageBox.Show($"Xabar yuborishda xatolik: {ex.Message}", "Xatolik",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MessageTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdatePlaceholder();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MessageTextBox_LostFocus xatolik: {ex.Message}");
            }
        }

        private void MessageTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdatePlaceholder();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MessageTextBox_GotFocus xatolik: {ex.Message}");
            }
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(MessageTextBox.Text))
                {
                    SendMessage();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MessageTextBox_KeyDown xatolik: {ex.Message}");
            }
        }

        private void UpdatePlaceholder()
        {
            try
            {
                // Placeholder logic'ni oddiylashtirish
                if (PlaceholderText != null)
                {
                    if (string.IsNullOrEmpty(MessageTextBox.Text) && !MessageTextBox.IsFocused)
                    {
                        PlaceholderText.Visibility = Visibility.Visible;
                        PlaceholderText.Text = "Type a message...";
                    }
                    else
                    {
                        PlaceholderText.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdatePlaceholder xatolik: {ex.Message}");
            }
        }

        private void SendMessage()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("SendMessage chaqirildi");

                string messageContent = MessageTextBox?.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(messageContent))
                {
                    return;
                }

                // Simple message display
                MessageBox.Show($"Xabar yuborildi: {messageContent}", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                MessageTextBox.Text = "";
                UpdatePlaceholder();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendMessage xatolik: {ex.Message}");
                MessageBox.Show($"Xabar yuborishda xatolik: {ex.Message}", "Xatolik",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }





        private async Task InitializeRuTokenAsync()
        {
            try
            {
                //StatusTextBlock.Text = "🔍 RuToken tekshirilmoqda...";
                //StatusTextBlock.Foreground = new SolidColorBrush(Colors.Orange);

                rutokenHelper = new RutokenHelper();
                bool initialized = await Task.Run(() =>
                {
                    try
                    {
                        return rutokenHelper.Initialize(Password);
                    }
                    catch
                    {
                        return false;
                    }
                });

                if (initialized)
                {
                    await LoadUserFromRuTokenAsync();
                }
                else
                {
                    throw new Exception("RuToken initialization failed");
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "⚠️ RuToken not available";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Orange);

                System.Diagnostics.Debug.WriteLine($"❌ RuToken init failed: {ex.Message}");

                // RuToken ishlamasa, fallback user
                await SetFallbackUserAsync();
            }
        }

        private async Task LoadUserFromRuTokenAsync()
        {
            try
            {
                if (rutokenHelper == null)
                {
                    throw new Exception("RuToken helper is null");
                }

                // "user" label'li token'ni qidirish
                var userToken = await Task.Run(() => rutokenHelper.GetTokenByLabel("user"));

                currentUser = userToken.Data;

                await Dispatcher.InvokeAsync(() =>
                {
                    StatusTextBlock.Text = $"🔐 RuToken: {currentUser}";
                    StatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                    CurrentUserTextBlock.Text = currentUser;
                });

                System.Diagnostics.Debug.WriteLine($"✅ User token loaded: {currentUser}");
            }
            catch (Exception tokenEx)
            {
                System.Diagnostics.Debug.WriteLine($"❌ User token failed: {tokenEx.Message}");

                // "user" token yo'q bo'lsa, boshqa tokenlarni ko'rish
                await LoadAnyAvailableTokenAsync();
            }
        }

        private async Task LoadAnyAvailableTokenAsync()
        {
            try
            {
                if (rutokenHelper == null)
                {
                    throw new Exception("RuToken helper is null");
                }

                var allTokens = await Task.Run(() => rutokenHelper.GetAllTokens());

                if (allTokens.Count > 0)
                {
                    var firstToken = allTokens[0];
                    currentUser = !string.IsNullOrEmpty(firstToken.Data) ?
                                  firstToken.Data : firstToken.Label;

                    await Dispatcher.InvokeAsync(() =>
                    {
                        StatusTextBlock.Text = $"🔐 RuToken: {currentUser}";
                        StatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                        CurrentUserTextBlock.Text = currentUser;
                    });

                    System.Diagnostics.Debug.WriteLine($"✅ Alternative token loaded: {currentUser}");

                    // Debug: Barcha tokenlarni ko'rsatish
                    foreach (var token in allTokens)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"📊 Token: Label='{token.Label}', Data='{token.Data}', Size={token.SizeBytes}");
                    }
                }
                else
                {
                    throw new Exception("Hech qanday token topilmadi");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ All tokens failed: {ex.Message}");
                await SetFallbackUserAsync();
            }
        }

        private async Task SetFallbackUserAsync()
        {
            await Task.Run(() =>
            {
                // Environment username'ni ishlatish
                string envUser = Environment.UserName;
                if (!string.IsNullOrEmpty(envUser) && envUser.Length >= 3)
                {
                    currentUser = envUser;

                    Dispatcher.InvokeAsync(() =>
                    {
                        StatusTextBlock.Text = $"👤 System User: {currentUser}";
                        StatusTextBlock.Foreground = new SolidColorBrush(Colors.Blue);
                        CurrentUserTextBlock.Text = currentUser;
                    });

                    System.Diagnostics.Debug.WriteLine($"✅ Fallback user set: {currentUser}");
                }
                else
                {
                    currentUser = "Unknown User";

                    Dispatcher.InvokeAsync(() =>
                    {
                        StatusTextBlock.Text = "❓ Unknown User";
                        StatusTextBlock.Foreground = new SolidColorBrush(Colors.Gray);
                        CurrentUserTextBlock.Text = currentUser;
                    });

                    System.Diagnostics.Debug.WriteLine("⚠️ No user identification available");
                }
            });
        }

    }
}