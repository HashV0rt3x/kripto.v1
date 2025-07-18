using kripto.Security;
using kripto.Windows;
using System;
using System.Text;
using System.Threading.Tasks;
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
        private string currentUser = "Unknown User";

        public string IpAddress { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;

        public MainWindow()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MainWindow constructor boshlandi");
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("MainWindow InitializeComponent tugadi");

                // Simple test
                this.Title = "Kripto Messenger - Starting...";

                // Window events
                this.Loaded += MainWindow_Loaded;
                this.Closing += MainWindow_Closing;

                System.Diagnostics.Debug.WriteLine("MainWindow constructor tugadi");
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

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MainWindow_Loaded chaqirildi");

            try
            {
                // UI elementlarini tekshirish
                if (MessagesPanel == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ MessagesPanel null!");
                    MessageBox.Show("UI elementlari to'g'ri yuklanmadi!", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Welcome message
                AddWelcomeMessage();

                // RuToken'ni ishga tushirish - faqat password mavjud bo'lganda
                if (!string.IsNullOrEmpty(Password))
                {
                    System.Diagnostics.Debug.WriteLine($"RuToken ishga tushirilmoqda, Password length: {Password.Length}");
                    await InitializeRuTokenAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Password mavjud emas, fallback user ishlatiladi");
                    await SetFallbackUserAsync();
                }

                // UI ni yangilash
                UpdatePlaceholder();

                System.Diagnostics.Debug.WriteLine("MainWindow_Loaded muvaffaqiyatli tugadi");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow_Loaded xatolik: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Dastur yuklanishida xatolik: {ex.Message}", "Xatolik",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MainWindow_Closing chaqirildi");

            try
            {
                // RuToken resurslarini tozalash
                rutokenHelper?.Dispose();

                // Confirmation dialog
                var result = MessageBox.Show("Dasturdan chiqishni xohlaysizmi?", "Tasdiqlash",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow_Closing xatolik: {ex.Message}");
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

                System.Diagnostics.Debug.WriteLine("Welcome message qo'shildi");
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
                if (e.Key == Key.Enter && MessageTextBox != null && !string.IsNullOrWhiteSpace(MessageTextBox.Text))
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
                if (PlaceholderText != null && MessageTextBox != null)
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

                // Xabarni UI ga qo'shish
                AddMessageToChat(messageContent, currentUser, true);

                // Test uchun - javob xabari
                AddMessageToChat($"Echo: {messageContent}", "Server", false);

                if (MessageTextBox != null)
                {
                    MessageTextBox.Text = "";
                }
                UpdatePlaceholder();

                // Scroll pastga
                MessagesScrollViewer?.ScrollToBottom();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendMessage xatolik: {ex.Message}");
                MessageBox.Show($"Xabar yuborishda xatolik: {ex.Message}", "Xatolik",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddMessageToChat(string message, string sender, bool isOwnMessage)
        {
            try
            {
                if (MessagesPanel == null) return;

                var messageContainer = new Border
                {
                    Background = new SolidColorBrush(isOwnMessage ?
                        Color.FromRgb(35, 134, 54) : Color.FromRgb(33, 38, 45)),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(16, 12, 16, 12),
                    Margin = new Thickness(isOwnMessage ? 50 : 0, 4, isOwnMessage ? 0 : 50, 4),
                    HorizontalAlignment = isOwnMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    MaxWidth = 400
                };

                var messageStack = new StackPanel();

                // Sender name
                var senderText = new TextBlock
                {
                    Text = sender,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                    Margin = new Thickness(0, 0, 0, 4)
                };

                // Message text
                var messageText = new TextBlock
                {
                    Text = message,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(240, 246, 252)),
                    TextWrapping = TextWrapping.Wrap
                };

                // Time stamp
                var timeText = new TextBlock
                {
                    Text = DateTime.Now.ToString("HH:mm"),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 4, 0, 0)
                };

                messageStack.Children.Add(senderText);
                messageStack.Children.Add(messageText);
                messageStack.Children.Add(timeText);

                messageContainer.Child = messageStack;
                MessagesPanel.Children.Add(messageContainer);

                System.Diagnostics.Debug.WriteLine($"Xabar qo'shildi: {sender} - {message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddMessageToChat xatolik: {ex.Message}");
            }
        }

        private async Task InitializeRuTokenAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 RuToken tekshirilmoqda...");

                if (string.IsNullOrEmpty(Password))
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Password bo'sh, RuToken ishga tushirilmaydi");
                    await SetFallbackUserAsync();
                    return;
                }

                rutokenHelper = new RutokenHelper();
                bool initialized = await Task.Run(() =>
                {
                    try
                    {
                        return rutokenHelper.Initialize(Password);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"RuToken init xatolik: {ex.Message}");
                        return false;
                    }
                });

                if (initialized)
                {
                    System.Diagnostics.Debug.WriteLine("✅ RuToken muvaffaqiyatli ishga tushirildi");
                    await LoadUserFromRuTokenAsync();
                }
                else
                {
                    throw new Exception("RuToken initialization failed");
                }
            }
            catch (Exception ex)
            {
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
                    System.Diagnostics.Debug.WriteLine("⚠️ rutokenHelper null!");
                    throw new Exception("RuToken helper is null");
                }

                // "user" label'li token'ni qidirish
                var userToken = await Task.Run(() =>
                {
                    try
                    {
                        return rutokenHelper.GetTokenByLabel("user");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"GetTokenByLabel('user') xatolik: {ex.Message}");
                        throw;
                    }
                });

                if (userToken != null && !string.IsNullOrEmpty(userToken.Data))
                {
                    currentUser = userToken.Data;

                    await Dispatcher.BeginInvoke(() =>
                    {
                        this.Title = $"Kripto Messenger - 🔐 {currentUser} @ {IpAddress}";
                    });

                    System.Diagnostics.Debug.WriteLine($"✅ User token loaded: {currentUser}");
                }
                else
                {
                    throw new Exception("User token ma'lumoti bo'sh");
                }
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
                    System.Diagnostics.Debug.WriteLine("⚠️ rutokenHelper null in LoadAnyAvailableTokenAsync!");
                    throw new Exception("RuToken helper is null");
                }

                var allTokens = await Task.Run(() =>
                {
                    try
                    {
                        return rutokenHelper.GetAllTokens();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"GetAllTokens() xatolik: {ex.Message}");
                        return new List<TokenData>();
                    }
                });

                if (allTokens != null && allTokens.Count > 0)
                {
                    var firstToken = allTokens[0];
                    currentUser = !string.IsNullOrEmpty(firstToken.Data) ?
                                  firstToken.Data : firstToken.Label;

                    await Dispatcher.BeginInvoke(() =>
                    {
                        this.Title = $"Kripto Messenger - 🔐 {currentUser} @ {IpAddress}";
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
            try
            {
                await Task.Run(() =>
                {
                    // Environment username'ni ishlatish
                    string envUser = Environment.UserName;
                    if (!string.IsNullOrEmpty(envUser) && envUser.Length >= 3)
                    {
                        currentUser = envUser;

                        Dispatcher.BeginInvoke(() =>
                        {
                            this.Title = $"Kripto Messenger - 👤 {currentUser} @ {IpAddress}";
                        });

                        System.Diagnostics.Debug.WriteLine($"✅ Fallback user set: {currentUser}");
                    }
                    else
                    {
                        currentUser = "Unknown User";

                        Dispatcher.BeginInvoke(() =>
                        {
                            this.Title = $"Kripto Messenger - ❓ Unknown @ {IpAddress}";
                        });

                        System.Diagnostics.Debug.WriteLine("⚠️ No user identification available");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetFallbackUserAsync xatolik: {ex.Message}");
                currentUser = "Default User";
                this.Title = $"Kripto Messenger - Default @ {IpAddress}";
            }
        }
    }
}