using kripto.Helpers;
using kripto.Models;
using kripto.Security;
using kripto.Services;
using kripto.Windows;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Win32;
using Org.BouncyCastle.Asn1.Cmp;
using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
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
using System.Windows.Threading;

namespace kripto
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SignalRService signalR = new SignalRService();
        private WebRtcService webrtc;
        //private string myId = "user2";
        //private string peerId = "user1";
        private string pendingOffer;

        // Services
        private RutokenHelper? rutokenHelper;
        private ChatService? chatService;
        private UserApiService? userApiService;
        private DispatcherTimer? refreshTimer;
        private ChatHistoryService? chatHistoryService;

        // Properties
        private string currentUser = "admin";
        private string token = "";
        private string authtoken = "";
        private string selectedChatUser = string.Empty;
        private List<string> onlineUsers = new List<string>();
        private List<OnlineUserDto> onlineUsersFromApi = new List<OnlineUserDto>();
        private Dictionary<string, List<(string message, bool isFromMe, DateTime time)>> chatHistory = new();

        public string IpAddress { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;
        public string TokenPassword { get; private set; } = string.Empty;

        private void ToggleCallButtons(bool enable)
        {
            btnEndCall.IsEnabled = !btnEndCall.IsEnabled;
            btnCall.IsEnabled = !btnCall.IsEnabled;
        }

        private void SetStatus(string text, bool accept = false, bool end = false, bool reject = false)
        {
            CallStatusText.Text = text;
            btnCall.IsEnabled = accept;
            btnEndCall.IsEnabled = end || reject;
            //btnRejectCall.Enabled = reject;
        }

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                this.Title = "Kripto Messenger - Starting...";

                Task.Run(async () => await signalR.ConnectAsync(currentUser));

                signalR.OnIncomingCall = async (from, offer) =>
                {
                    selectedChatUser = from;
                    pendingOffer = offer;
                    await Dispatcher.InvokeAsync(() =>
                    {
                        var popup = new CallWindow
                        {
                            Owner = this,
                            Topmost = true,
                        };

                        popup.btnAccept.Click += async (_, __) =>
                        {
                            popup.Close();
                            try
                            {
                                webrtc = CreateWebRTC();

                                webrtc.InitAsReceiver();
                                await webrtc.SetRemoteDescriptionAsync(pendingOffer, RTCSdpType.offer);
                                var answer = await webrtc.CreateAnswerAsync();
                                await signalR.AnswerCallAsync(selectedChatUser, answer);

                                webrtc.StartAudio();

                                Dispatcher.Invoke(() =>
                                {
                                    SetStatus("📞 Qo‘ng‘iroq qabul qilindi", end: true);
                                    ToggleCallButtons(false);
                                });
                            }
                            catch (Exception ex)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    SetStatus("❌ Javob yuborilmadi", accept: true);
                                    MessageBox.Show(ex.Message);
                                });
                            }
                        };

                        popup.btnReject.Click += async (_, __) =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                popup.Close();
                                try
                                {
                                    Task.Run(async () => await signalR.RejectCallAsync(selectedChatUser));
                                    SetStatus("⛔ Siz qo‘ng‘iroqni rad qildingiz");
                                    webrtc?.Close();
                                }
                                catch (Exception ex)
                                {
                                    SetStatus("❌ Rad qilishda xato", end: true);
                                    MessageBox.Show(ex.Message);
                                }
                                btnCall.IsEnabled = true;
                                btnEndCall.IsEnabled = false;
                            });
                        };

                        popup.ShowDialog();
                    });
                    Dispatcher.Invoke(() => SetStatus($"📞 Qo‘ng‘iroq kelmoqda: {from}", accept: true, reject: true));
                };

                signalR.OnCallAnswered = async (from, answer) =>
                {
                    await webrtc.SetRemoteDescriptionAsync(answer, RTCSdpType.answer);
                    webrtc.StartAudio();
                    Dispatcher.Invoke(() => SetStatus("✅ Qarshi tomon javob berdi", end: true));
                };

                signalR.OnCallEnded = (fromConnId) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        btnCall.IsEnabled = true;
                        SetStatus("❌ Qarshi tomon qo‘ng‘iroqni yakunladi", accept: true);
                        webrtc?.Close();
                    });
                };

                signalR.OnCallRejected = (from) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        SetStatus("🚫 Qarshi tomon qo‘ng‘iroqni rad etdi", accept: true);
                        webrtc?.Close();
                        btnCall.IsEnabled = true;
                        btnEndCall.IsEnabled = false; 
                    });
                    //ToggleCallButtons(false);
                };

                signalR.OnIceCandidateReceived = async (from, candidate) =>
                {
                    await webrtc.AddIceCandidateAsync(candidate);
                };

                // Window events
                this.Loaded += MainWindow_Loaded;
                this.Closing += MainWindow_Closing;

                // Timer o'rnatish (5 soniyada bir API'dan userlarni yangilash)
                refreshTimer = new DispatcherTimer();
                refreshTimer.Interval = TimeSpan.FromSeconds(5);
                refreshTimer.Tick += RefreshTimer_Tick;

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

        /// <summary>
        /// Connection ma'lumotlarini o'rnatish
        /// </summary>
        public async void SetConnectionInfo(string ipAddress, string password)
        {
            this.IpAddress = ipAddress;
            this.Password = password;

            RuToken.InitializeRutoken(password);
            // var res = RuToken.ListAllTokens();
            this.currentUser = RuToken.ReadCustomToken("user");
            this.token = RuToken.ReadCustomToken("kripto");
            TokenPassword = RuToken.ReadCustomToken("password");


            System.Diagnostics.Debug.WriteLine($"Connection info set: {ipAddress}, {password.Length} chars");
            this.Title = $"Kripto Messenger - Connecting to {ipAddress}";


            // UserApiService'ni yaratish
            await InitializeUserApiService();
        }

        /// <summary>
        /// UserApiService'ni ishga tushirish
        /// </summary>
        private async Task InitializeUserApiService()
        {
            try
            {
                if (!string.IsNullOrEmpty(IpAddress))
                {
                    // Backend API'ning base URL'ini o'rnatish
                    string apiBaseUrl = $"http://{IpAddress}:8099"; // Port'ni o'zingiznikiga o'zgartiring
                    userApiService = new UserApiService(apiBaseUrl,authtoken);

                    onlineUsers = await userApiService.GetAllUserNamesAsync();
                    UpdateUsersPanel();

                    System.Diagnostics.Debug.WriteLine($"UserApiService yaratildi: {apiBaseUrl}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeUserApiService xatolik: {ex.Message}");
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MainWindow_Loaded chaqirildi");

            try
            {
                if (MessagesPanel == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ MessagesPanel null!");
                    MessageBox.Show("UI elementlari to'g'ri yuklanmadi!", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Welcome message
                AddWelcomeMessage();

                // RuToken'ni ishga tushirish
                if (!string.IsNullOrEmpty(Password))
                {
                    System.Diagnostics.Debug.WriteLine($"RuToken ishga tushirilmoqda...");
                    //await InitializeRuTokenAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Password mavjud emas, fallback user ishlatiladi");
                    await SetFallbackUserAsync();
                }

                // Chat service'ni ishga tushirish
                await InitializeChatServiceAsync();

                // API token o'rnatish va userlarni yuklash
                await InitializeApiIntegrationAsync();

                // UserApiService'ni yaratish
                InitializeUserApiService();

                // UI ni yangilash
                UpdatePlaceholder();
                UpdateUIState();
                UpdateChatHeader();

                // Timer boshlash
                refreshTimer?.Start();

                System.Diagnostics.Debug.WriteLine("MainWindow_Loaded muvaffaqiyatli tugadi");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow_Loaded xatolik: {ex.Message}");
                MessageBox.Show($"Dastur yuklanishida xatolik: {ex.Message}", "Xatolik",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// API integratsiyasini ishga tushirish
        /// </summary>
        private async Task InitializeApiIntegrationAsync()
        {
            try
            {
                if (userApiService == null)
                {
                    System.Diagnostics.Debug.WriteLine("UserApiService null, API integratsiyasi o'tkazib yuborildi");
                    return;
                }

                // Token olish (agar ChatService'dan token olinsa)
                if (chatService != null)
                {
                    try
                    {
                        // Agar ChatService'da token olish metodi bo'lsa
                        // var token = await chatService.GetAuthTokenAsync();
                        // if (!string.IsNullOrEmpty(token))
                        // {
                        //     userApiService.SetAuthToken(token);
                        // }

                        // Hozircha test token
                        userApiService.SetAuthToken("test_token_123");
                        System.Diagnostics.Debug.WriteLine("API token o'rnatildi");

                        userApiService = new UserApiService(IpAddress,authtoken);

                        //Console.WriteLine(userApiService.GetAllUsersAsync);
                    }
                    catch (Exception tokenEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Token olishda xatolik: {tokenEx.Message}");
                    }
                }

                // Dastlabki userlarni yuklash
                await LoadUsersFromApiAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeApiIntegrationAsync xatolik: {ex.Message}");
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MainWindow_Closing chaqirildi");

            try
            {
                var result = MessageBox.Show("Dasturdan chiqishni xohlaysizmi?", "Tasdiqlash",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                // Timer to'xtatish
                refreshTimer?.Stop();

                // Resurslarni tozalash
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (chatService != null)
                        {
                            await chatService.DisconnectAsync();
                            chatService.Dispose();
                        }

                        userApiService?.Dispose();
                        rutokenHelper?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Cleanup xatolik: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow_Closing xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Timer tick - API'dan userlarni yangilash
        /// </summary>
        private async void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                await LoadUsersFromApiAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshTimer_Tick xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// API'dan userlarni yuklash
        /// </summary>
        private async Task LoadUsersFromApiAsync()
        {
            try
            {
                if (userApiService == null)
                {
                    System.Diagnostics.Debug.WriteLine("UserApiService null, fallback users ishlatiladi");

                    // Fallback - eski test userlar
                    if (onlineUsers.Count == 0)
                    {
                        onlineUsers = new List<string> { "TestUser1", "TestUser2", "Admin", "Alice", "Bob" };
                        Dispatcher.Invoke(() => UpdateUsersPanel());
                    }
                    return;
                }

                // API'dan online userlarni olish
                var apiUsers = await userApiService.GetOnlineUsersAsync();
                System.Diagnostics.Debug.WriteLine($"API'dan {apiUsers.Count} user olindi");

                if (apiUsers.Count > 0)
                {
                    onlineUsersFromApi = apiUsers;

                    // String list'ni ham yangilash (eski kod bilan uygunlik uchun)
                    onlineUsers = apiUsers
                        .Where(u => !u.UserName.Equals(currentUser, StringComparison.OrdinalIgnoreCase))
                        .Select(u => u.UserName)
                        .ToList();

                    Dispatcher.Invoke(() =>
                    {
                        UpdateUsersPanel();
                        AddSystemMessage($"👥 API'dan {apiUsers.Count} online user yangilandi");
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("API'dan userlar olinmadi, mavjud ro'yxat saqlanadi");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadUsersFromApiAsync xatolik: {ex.Message}");

                // API xatolik bo'lsa, fallback userlarni ko'rsatish
                if (onlineUsers.Count == 0)
                {
                    onlineUsers = new List<string> { "TestUser1", "TestUser2", "Admin" };
                    Dispatcher.Invoke(() =>
                    {
                        UpdateUsersPanel();
                        AddSystemMessage("⚠️ API'dan userlar olinmadi, test userlar ko'rsatildi");
                    });
                }
            }
        }

        /// <summary>
        /// Chat service'ni ishga tushirish
        /// </summary>
        private async Task InitializeChatServiceAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔌 Chat service ishga tushirilmoqda: {IpAddress}:8099");

                // ChatService yaratish - kiritilgan IP manzilni ishlatish
                chatService = new ChatService(IpAddress, "8099", currentUser);

                // Event handlerlar
                chatService.MessageReceived += OnMessageReceived;
                chatService.OnlineUsersUpdated += OnOnlineUsersUpdated;
                chatService.ConnectionStatusChanged += OnConnectionStatusChanged;
                chatService.ErrorOccurred += OnErrorOccurred;

                // Avval server mavjudligini tekshirish
                bool serverExists = await TestServerAsync(IpAddress, "8099");
                if (serverExists is not true)
                {
                    throw new Exception($"Server {IpAddress}:8099 ga ulanib bo'lmadi. Server ishlamayotgan bo'lishi mumkin.");
                }

                // Authentication
                string authenticated = await chatService.AuthenticateAsync(currentUser,TokenPassword);
                if (authenticated is null)
                {
                    throw new Exception("Authentication failed - parol noto'g'ri bo'lishi mumkin");
                }

                authtoken = authenticated;

                //chatHistoryService = new ChatHistoryService(authtoken);

                //var his =await ChatHistoryService.GetHistoryAsync(targetUser: "user1",authtoken);

                // Connection
                bool connected = await chatService.ConnectAsync();
                if (!connected)
                {
                    throw new Exception("WebSocket connection failed");
                }

                System.Diagnostics.Debug.WriteLine("✅ Chat service muvaffaqiyatli ishga tushdi");

                Dispatcher.Invoke(() =>
                {
                    this.Title = $"Kripto Messenger - 🟢 Connected - {currentUser}@{IpAddress}";
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Chat service init xatolik: {ex.Message}");

                Dispatcher.Invoke(() =>
                {
                    string detailedError = ex.Message;

                    if (ex.Message.Contains("127.0.0.1") || ex.Message.Contains("localhost"))
                    {
                        detailedError += "\n\n💡 Agar server localhost'da ishlamayotgan bo'lsa:\n" +
                                       "• Server dasturini ishga tushiring\n" +
                                       "• To'g'ri port (8099) ishlatilayotganini tekshiring\n" +
                                       "• Firewall server portini bloklamayotganini tekshiring";
                    }
                    else
                    {
                        detailedError += $"\n\n💡 Server {IpAddress}:8099 mavjud emasligining sabablari:\n" +
                                       "• Server o'chiq bo'lishi mumkin\n" +
                                       "• IP manzil noto'g'ri kiritilgan\n" +
                                       "• Network bog'lanish muammosi\n" +
                                       "• Firewall portni bloklagan";
                    }

                    MessageBox.Show($"Server bilan bog'lanishda xatolik:\n\n{detailedError}",
                        "Ulanish xatoligi", MessageBoxButton.OK, MessageBoxImage.Warning);

                    this.Title = $"Kripto Messenger - 🔴 Offline - {currentUser}@{IpAddress}";

                    // Offline mode uchun test userlarni qo'shish
                    ShowOfflineTestUsers();
                });
            }
        }

        /// <summary>
        /// Offline rejimda test userlarni ko'rsatish
        /// </summary>
        private void ShowOfflineTestUsers()
        {
            try
            {
                // Test userlar
                onlineUsers = new List<string> { "TestUser1", "TestUser2", "Admin", "Alice", "Bob" };
                UpdateUsersPanel();

                // Notification
                AddSystemMessage("🔴 Offline rejimda ishlayapman. Test userlar bilan xabar almashishingiz mumkin.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowOfflineTestUsers xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Server mavjudligini oddiy tekshirish
        /// </summary>
        private async Task<bool> TestServerAsync(string ip, string port)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                var response = await client.GetAsync($"http://{ip}:{port}/");
                return true;
            }
            catch (System.Net.Http.HttpRequestException)
            {
                return false;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TestServer xatolik: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xabar qabul qilish event handler
        /// </summary>
        private void OnMessageReceived(string fromUser, string messageText, DateTime timestamp)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    bool isOwnMessage = fromUser.Equals(currentUser, StringComparison.OrdinalIgnoreCase);

                    // Chat history'ga qo'shish
                    AddToChatHistory(fromUser, messageText, !isOwnMessage, timestamp);

                    // Agar ushbu user bilan chat ochiq bo'lsa, xabarni ko'rsatish
                    if (selectedChatUser == fromUser || isOwnMessage)
                    {
                        AddMessageToChat(messageText, fromUser, isOwnMessage);
                    }
                    else
                    {
                        // Boshqa userdan xabar kelsa, notification
                        AddSystemMessage($"💬 {fromUser}dan yangi xabar keldi!");

                        // User button'ni highlight qilish (unread indicator)
                        HighlightUserWithNewMessage(fromUser);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnMessageReceived xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Online userlar yangilash event handler
        /// </summary>
        private void OnOnlineUsersUpdated(List<string> users)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    onlineUsers = users.Where(u => !u.Equals(currentUser, StringComparison.OrdinalIgnoreCase)).ToList();
                    UpdateUsersPanel();

                    AddSystemMessage($"👥 Online users yangilandi: {onlineUsers.Count} foydalanuvchi");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnOnlineUsersUpdated xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Connection status o'zgarish event handler
        /// </summary>
        private void OnConnectionStatusChanged(bool isConnected)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    string status = isConnected ? "🟢 Connected" : "🔴 Disconnected";
                    this.Title = $"Kripto Messenger - {status} - {currentUser}@{IpAddress}";
                    UpdateUIState();

                    string statusMessage = isConnected ? "✅ Server bilan bog'lanish o'rnatildi" : "❌ Server bilan bog'lanish uzildi";
                    AddSystemMessage(statusMessage);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnConnectionStatusChanged xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Xatolik event handler
        /// </summary>
        private void OnErrorOccurred(string errorMessage)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Chat error: {errorMessage}");
                    AddSystemMessage($"⚠️ Xatolik: {errorMessage}");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnErrorOccurred xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Users panel'ni yangilash (yangi API integratsiyasi bilan)
        /// </summary>
        private void UpdateUsersPanel()
        {
            try
            {
                if (UsersPanel == null) return;

                UsersPanel.Children.Clear();

                // Agar API'dan userlar bo'lsa, ularni ishlatish
                if (onlineUsersFromApi.Count > 0)
                {
                    foreach (var apiUser in onlineUsersFromApi)
                    {
                        if (!apiUser.UserName.Equals(currentUser, StringComparison.OrdinalIgnoreCase))
                        {
                            var userControl = CreateUserControlFromApi(apiUser);
                            UsersPanel.Children.Add(userControl);
                        }
                    }
                }
                else
                {
                    // Fallback - string list'dan user buttonlar yaratish
                    foreach (string user in onlineUsers)
                    {
                        var userButton = CreateUserButton(user);
                        UsersPanel.Children.Add(userButton);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Users panel yangilandi: {onlineUsers.Count} users");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateUsersPanel xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// API'dan kelgan user uchun control yaratish
        /// </summary>
        private Border CreateUserControlFromApi(OnlineUserDto user)
        {
            var userBorder = new Border
            {
                Background = new SolidColorBrush(user.UserName == selectedChatUser ?
                    Color.FromRgb(35, 134, 54) : Color.FromRgb(33, 38, 45)),
                Margin = new Thickness(12, 4, 12, 4),
                Padding = new Thickness(12, 8, 12, 8),
                CornerRadius = new CornerRadius(6),
                Cursor = Cursors.Hand,
                Tag = user.UserName
            };

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            // Avatar
            var avatar = new Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(16),
                Background = new SolidColorBrush(Color.FromRgb(35, 134, 54)),
                Margin = new Thickness(0, 0, 12, 0)
            };

            var avatarText = new TextBlock
            {
                Text = string.IsNullOrEmpty(user.UserName) ? "?" : user.UserName[0].ToString().ToUpper(),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            avatar.Child = avatarText;

            // User info
            var userInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var nameText = new TextBlock
            {
                Text = user.UserName,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(240, 246, 252))
            };

            var statusText = new TextBlock
            {
                Text = user.ConnectionCount > 1 ? $"Online ({user.ConnectionCount} sessions)" : "Online",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129))
            };

            // Connection time
            var connectedText = new TextBlock
            {
                Text = $"Connected: {user.ConnectedAt:HH:mm}",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158))
            };

            userInfo.Children.Add(nameText);
            userInfo.Children.Add(statusText);
            userInfo.Children.Add(connectedText);

            // Unread indicator
            var unreadIndicator = new Border
            {
                Width = 10,
                Height = 10,
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                Margin = new Thickness(8, 0, 0, 0),
                Visibility = HasUnreadMessages(user.UserName) ? Visibility.Visible : Visibility.Collapsed
            };

            stackPanel.Children.Add(avatar);
            stackPanel.Children.Add(userInfo);
            stackPanel.Children.Add(unreadIndicator);
            userBorder.Child = stackPanel;

            // Click event
            userBorder.MouseLeftButtonUp += (s, e) => SelectChatUser(user.UserName);

            return userBorder;
        }

        /// <summary>
        /// User button yaratish (fallback string list uchun)
        /// </summary>
        private Border CreateUserButton(string userName)
        {
            var userBorder = new Border
            {
                Background = new SolidColorBrush(userName == selectedChatUser ?
                    Color.FromRgb(35, 134, 54) : Color.FromRgb(33, 38, 45)),
                Margin = new Thickness(12, 4, 12, 4),
                Padding = new Thickness(12, 8, 12, 8),
                CornerRadius = new CornerRadius(6),
                Cursor = Cursors.Hand,
                Tag = userName
            };
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            // Avatar
            var avatar = new Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(16),
                Background = new SolidColorBrush(Color.FromRgb(35, 134, 54)),
                Margin = new Thickness(0, 0, 12, 0)
            };

            var avatarText = new TextBlock
            {
                Text = userName.Length > 0 ? userName[0].ToString().ToUpper() : "?",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            avatar.Child = avatarText;

            // User info
            var userInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var nameText = new TextBlock
            {
                Text = userName,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(240, 246, 252))
            };

            var statusText = new TextBlock
            {
                Text = "Online",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129))
            };

            userInfo.Children.Add(nameText);
            userInfo.Children.Add(statusText);

            // Unread indicator
            var unreadIndicator = new Border
            {
                Width = 10,
                Height = 10,
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                Margin = new Thickness(8, 0, 0, 0),
                Visibility = HasUnreadMessages(userName) ? Visibility.Visible : Visibility.Collapsed
            };

            stackPanel.Children.Add(avatar);
            stackPanel.Children.Add(userInfo);
            stackPanel.Children.Add(unreadIndicator);
            userBorder.Child = stackPanel;

            // Click event
            userBorder.MouseLeftButtonUp += (s, e) => SelectChatUser(userName);

            return userBorder;
        }

        /// <summary>
        /// Chat user'ni tanlash
        /// </summary>
        private void SelectChatUser(string userName)
        {
            try
            {
                string previousUser = selectedChatUser;
                selectedChatUser = userName;

                System.Diagnostics.Debug.WriteLine($"Chat user selected: {userName}");

                // Chat header'ni yangilash
                UpdateChatHeader();

                // Users panel'ni yangilash (selection highlight)
                UpdateUsersPanel();

                // Chat history'ni yuklash
                LoadChatHistory(userName);

                // Unread messages'ni clear qilish
                ClearUnreadMessages(userName);

                // Agar user o'zgargan bo'lsa, welcome message
                if (previousUser != userName)
                {
                    AddSystemMessage($"💬 {userName} bilan suhbat boshlandi");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SelectChatUser xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Chat header'ni yangilash
        /// </summary>
        private void UpdateChatHeader()
        {
            try
            {
                if (string.IsNullOrEmpty(selectedChatUser))
                {
                    if (ChatHeaderTextBlock != null)
                        ChatHeaderTextBlock.Text = "Select a conversation";
                    if (ChatStatusText != null)
                        ChatStatusText.Text = "Click on a user to start chatting";
                    if (ChatAvatarText != null)
                        ChatAvatarText.Text = "?";
                }
                else
                {
                    if (ChatHeaderTextBlock != null)
                        ChatHeaderTextBlock.Text = selectedChatUser;

                    // API'dan user ma'lumotlari bo'lsa, to'liq ma'lumot ko'rsatish
                    var apiUser = onlineUsersFromApi.FirstOrDefault(u => u.UserName == selectedChatUser);
                    if (apiUser != null)
                    {
                        if (ChatStatusText != null)
                            ChatStatusText.Text = apiUser.ConnectionCount > 1 ?
                                $"Online ({apiUser.ConnectionCount} sessions)" : "Online";
                    }
                    else
                    {
                        if (ChatStatusText != null)
                            ChatStatusText.Text = "Online";
                    }

                    if (ChatAvatarText != null)
                        ChatAvatarText.Text = selectedChatUser.Length > 0 ? selectedChatUser[0].ToString().ToUpper() : "?";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateChatHeader xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Chat history'ni yuklash
        /// </summary>
        private async Task LoadChatHistory(string userName)
        {
            try
            {
                if (MessagesPanel == null) return;

                // Avvalgi xabarlarni tozalash
                MessagesPanel.Children.Clear();

                // Agar ushbu user bilan history bo'lsa, yuklash
                //if (chatHistory.ContainsKey(userName))
                //{
                ChatHistoryService.SERVER_IP = IpAddress;
                var messages = await ChatHistoryService.GetHistoryAsync(userName, authtoken);
                if (messages != null && messages.Count > 0)
                {
                    foreach (var msg in messages)
                    {
                        AddMessageToChat(msg.Text,  msg.FromUser, msg.FromUser == currentUser);
                    }
                }
                //}

                // Scroll pastga
                MessagesScrollViewer?.ScrollToBottom();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadChatHistory xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Chat history'ga xabar qo'shish
        /// </summary>
        private void AddToChatHistory(string userName, string message, bool isFromThem, DateTime timestamp)
        {
            try
            {
                if (!chatHistory.ContainsKey(userName))
                {
                    chatHistory[userName] = new List<(string, bool, DateTime)>();
                }

                chatHistory[userName].Add((message, !isFromThem, timestamp));

                // History'ni 100 xabargacha cheklash
                if (chatHistory[userName].Count > 100)
                {
                    chatHistory[userName].RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddToChatHistory xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Yangi xabar bilan user'ni highlight qilish
        /// </summary>
        private void HighlightUserWithNewMessage(string userName)
        {
            // Unread messages uchun simple tracking
            // Real implementation'da database yoki file'da saqlash mumkin
        }

        /// <summary>
        /// User'da unread messages borligini tekshirish
        /// </summary>
        private bool HasUnreadMessages(string userName)
        {
            // Simple implementation - real app'da proper tracking kerak
            return false;
        }

        /// <summary>
        /// Unread messages'ni clear qilish
        /// </summary>
        private void ClearUnreadMessages(string userName)
        {
            // Implementation for clearing unread status
        }

        /// <summary>
        /// System message qo'shish
        /// </summary>
        private void AddSystemMessage(string message)
        {
            try
            {
                if (MessagesPanel == null) return;

                var systemMessage = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(20, 4, 20, 4)
                };

                var messageText = new TextBlock
                {
                    Text = message,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };

                systemMessage.Child = messageText;
                MessagesPanel.Children.Add(systemMessage);

                MessagesScrollViewer?.ScrollToBottom();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddSystemMessage xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// UI state'ni yangilash
        /// </summary>
        private void UpdateUIState()
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    bool isConnected = chatService?.IsConnected == true;
                    bool hasSelectedUser = !string.IsNullOrEmpty(selectedChatUser);

                    SendButton.IsEnabled = true; // Offline'da ham yozish mumkin
                    MessageTextBox.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateUIState xatolik: {ex.Message}");
                }
            });
        }

        private void AddWelcomeMessage()
        {
            try
            {
                var welcomeMessage = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16, 12, 16, 12),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20)
                };

                var welcomeText = new TextBlock
                {
                    Text = $"Welcome to Kripto Messenger! 🔐\nConnecting to: {IpAddress ?? "Unknown"}",
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
                _ = SendMessageAsync();
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
                    _ = SendMessageAsync();
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
                if (PlaceholderText != null && MessageTextBox != null)
                {
                    if (string.IsNullOrEmpty(MessageTextBox.Text) && !MessageTextBox.IsFocused)
                    {
                        PlaceholderText.Visibility = Visibility.Visible;

                        if (string.IsNullOrEmpty(selectedChatUser))
                        {
                            PlaceholderText.Text = "Select a user to start chatting...";
                        }
                        else if (chatService?.IsConnected == true)
                        {
                            PlaceholderText.Text = $"Type a message to {selectedChatUser}...";
                        }
                        else
                        {
                            PlaceholderText.Text = $"Type a message to {selectedChatUser} (offline)...";
                        }
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

        /// <summary>
        /// Xabar yuborish (async)
        /// </summary>
        private async Task SendMessageAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("SendMessageAsync chaqirildi");

                string messageContent = MessageTextBox?.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(messageContent))
                {
                    return;
                }

                if (string.IsNullOrEmpty(selectedChatUser))
                {
                    MessageBox.Show("Xabar yuborish uchun foydalanuvchini tanlang!", "Xatolik",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // O'z xabarimizni UI ga qo'shish
                AddMessageToChat(messageContent, currentUser, true);

                // Chat history'ga qo'shish
                AddToChatHistory(selectedChatUser, messageContent, false, DateTime.Now);

                if (chatService?.IsConnected == true)
                {
                    // Server ga yuborish
                    bool sent = await chatService.SendMessageAsync(selectedChatUser, messageContent);

                    if (!sent)
                    {
                        AddSystemMessage("⚠️ Xabar yuborilmadi - server bilan bog'lanish yo'q");
                    }
                }
                else
                {
                    // Offline mode - test javob
                    await Task.Delay(1000); // Simulate network delay
                    string response = GenerateTestResponse(messageContent);
                    AddMessageToChat(response, selectedChatUser, false);
                    AddToChatHistory(selectedChatUser, response, true, DateTime.Now);
                    AddSystemMessage("📱 Test javob (offline mode)");
                }

                // TextBox'ni tozalash
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
                System.Diagnostics.Debug.WriteLine($"SendMessageAsync xatolik: {ex.Message}");
                MessageBox.Show($"Xabar yuborishda xatolik: {ex.Message}", "Xatolik",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Test javob generatsiya qilish (offline mode uchun)
        /// </summary>
        private string GenerateTestResponse(string originalMessage)
        {
            var responses = new[]
            {
                $"Sizning '{originalMessage}' xabaringizni qabul qildim!",
                "Rahmat xabaringiz uchun!",
                "Qanday hollar?",
                "Yaxshi, tushundim.",
                "Bu qiziqarli!",
                "Ha, to'g'ri aytasiz.",
                "Menga ham shunday tuyuladi.",
                "Keling, buni muhokama qilaylik."
            };

            var random = new Random();
            return responses[random.Next(responses.Length)];
        }

        private void AddMessageToChat(string message, string sender, bool isOwnMessage)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (MessagesPanel == null) return;

                    var messageContainer = new Border
                    {
                        Background = new SolidColorBrush(isOwnMessage ?
                            Color.FromRgb(35, 134, 54) : Color.FromRgb(33, 38, 45)),
                        CornerRadius = new CornerRadius(12),
                        Padding = new Thickness(14, 8, 14, 8),
                        Margin = new Thickness(isOwnMessage ? 50 : 5, 4, isOwnMessage ? 5 : 50, 4),
                        HorizontalAlignment = isOwnMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                        MaxWidth = 400
                    };

                    var messageStack = new StackPanel();

                    // Sender name (faqat boshqa userlar uchun)
                    if (!isOwnMessage)
                    {
                        var senderText = new TextBlock
                        {
                            Text = sender,
                            FontSize = 12,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                            Margin = new Thickness(0, 0, 0, 4)
                        };
                        messageStack.Children.Add(senderText);
                    }

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
            });
        }

        //private async Task InitializeRuTokenAsync()
        //{
        //    try
        //    {
        //        System.Diagnostics.Debug.WriteLine("🔍 RuToken tekshirilmoqda...");

        //        if (string.IsNullOrEmpty(Password))
        //        {
        //            System.Diagnostics.Debug.WriteLine("⚠️ Password bo'sh, RuToken ishga tushirilmaydi");
        //            await SetFallbackUserAsync();
        //            return;
        //        }

        //        rutokenHelper = new RutokenHelper();
        //        bool initialized = await Task.Run(() =>
        //        {
        //            try
        //            {
        //                return rutokenHelper.Initialize(Password);
        //            }
        //            catch (Exception ex)
        //            {
        //                System.Diagnostics.Debug.WriteLine($"RuToken init xatolik: {ex.Message}");
        //                return false;
        //            }
        //        });

        //        if (initialized)
        //        {
        //            System.Diagnostics.Debug.WriteLine("✅ RuToken muvaffaqiyatli ishga tushirildi");
        //            await LoadUserFromRuTokenAsync();
        //        }
        //        else
        //        {
        //            throw new Exception("RuToken initialization failed");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"❌ RuToken init failed: {ex.Message}");
        //        await SetFallbackUserAsync();
        //    }
        //}

        private async Task LoadUserFromRuTokenAsync()
        {
            try
            {
                if (rutokenHelper == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ rutokenHelper null!");
                    throw new Exception("RuToken helper is null");
                }

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

                    System.Diagnostics.Debug.WriteLine($"✅ Alternative token loaded: {currentUser}");

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
                    string envUser = "admin";
                    if (!string.IsNullOrEmpty(envUser) && envUser.Length >= 3)
                    {
                        currentUser = envUser;
                        System.Diagnostics.Debug.WriteLine($"✅ Fallback user set: {currentUser}");
                    }
                    else
                    {
                        currentUser = "Unknown User";
                        System.Diagnostics.Debug.WriteLine("⚠️ No user identification available");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetFallbackUserAsync xatolik: {ex.Message}");
                currentUser = "Default User";
            }
        }

        private async void btnCall_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //selectedChatUser = "user1";
                webrtc = CreateWebRTC();

                webrtc.InitAsCaller();
                var offer = await webrtc.CreateOfferAsync();
                await signalR.CallUserAsync(selectedChatUser, offer);

                SetStatus("📤 Qo‘ng‘iroq yuborildi...", end: true);
                StartCallTimeout();
            }
            catch (Exception ex)
            {
                SetStatus("❌ Qo‘ng‘iroq yuborilmadi", accept: true);
                MessageBox.Show(ex.Message);
            }
        }

        private WebRtcService CreateWebRTC()
        {
            var rtc = new WebRtcService();

            rtc.OnIceCandidateReady = async (ice) =>
            {
                await signalR.SendIceCandidateAsync(selectedChatUser, ice);
            };

            return rtc;
        }

        private async void StartCallTimeout()
        {
            await Task.Delay(600000);
            if (btnEndCall.IsEnabled && !btnCall.IsEnabled)
            {
                await signalR.EndCallAsync(selectedChatUser);
                await Dispatcher.Invoke(async () =>
                {
                    SetStatus("⌛ Javob bo‘lmadi, avtomatik tugatildi");
                    btnEndCall.IsEnabled = false;
                    btnCall.IsEnabled = true;
                });
                webrtc?.Close();
            }
        }
        
        private async void btnEndCall_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await signalR.EndCallAsync(selectedChatUser);
                Dispatcher.Invoke(() =>
                {
                    SetStatus("🔚 Qo‘ng‘iroq tugatildi", accept: true, end: false);
                });

                webrtc?.Close();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    SetStatus("❌ Tugatishda xato", end: true);
                    MessageBox.Show(ex.Message);
                });
            }
            Dispatcher.Invoke(() =>
            {
                btnCall.IsEnabled = true;
                btnEndCall.IsEnabled = false;
            });
        }

        private async void btnFileUpload_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog() == true)
            {
                var result = await chatService.SendFileAsync(selectedChatUser, fileDialog.FileName);
            }
            AddMessageToChat($"{fileDialog.FileName} fayli jo'natildi!", currentUser, true);
        }
    }
}