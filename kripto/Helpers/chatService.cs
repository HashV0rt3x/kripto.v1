﻿using Backup.Service.Services;
using kripto.Models;
using MaterialDesignColors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Windows.Storage.Streams;

namespace kripto.Helpers
{
    public class ChatService : IDisposable
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private ClientWebSocket? webSocket;
        private CancellationTokenSource? cancellationTokenSource;
        private bool isConnected = false;
        private bool isDisposed = false;

        public string ServerIP { get; set; }
        public string ServerPort { get; set; }
        public string Username { get; set; }
        public string? AuthToken { get; private set; }

        // Events
        public event Action<string, string, DateTime>? MessageReceived;
        public event Action<List<string>>? OnlineUsersUpdated;
        public event Action<bool>? ConnectionStatusChanged;
        public event Action<string>? ErrorOccurred;

        public bool IsConnected => isConnected && webSocket?.State == WebSocketState.Open;

        public ChatService(string serverIP, string serverPort, string username)
        {
            ServerIP = serverIP;
            ServerPort = serverPort;
            Username = username;

            // HTTP client timeout
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            System.Diagnostics.Debug.WriteLine($"ChatService yaratildi: {username}@{serverIP}:{serverPort}");
        }

        /// <summary>
        /// Auth token olish
        /// </summary>
        public async Task<string?> AuthenticateAsync(string username,string password)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔐 Authentication: {Username}@{ServerIP}:{ServerPort}");

                var loginRequest = new
                {
                    username = username,
                    password = password
                };

                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(
                    $"http://{ServerIP}:{ServerPort}/api/auth/login",
                    content);

                System.Diagnostics.Debug.WriteLine($"Auth Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Auth Response: {responseContent}");

                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (loginResponse?.Success == true && !string.IsNullOrEmpty(loginResponse.Token))
                    {
                        AuthToken = loginResponse.Token;
                        System.Diagnostics.Debug.WriteLine("✅ Authentication muvaffaqiyatli");
                        return AuthToken;
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"❌ Auth failed: {errorContent}");
                ErrorOccurred?.Invoke($"Authentication failed: {errorContent}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Auth error: {ex.Message}");
                ErrorOccurred?.Invoke($"Authentication error: {ex.Message}");
                return null;
            }
        }


        /// <summary>
        /// WebSocket connection ochish
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(AuthToken))
                {
                    System.Diagnostics.Debug.WriteLine("❌ Auth token mavjud emas");
                    ErrorOccurred?.Invoke("Auth token required. Please authenticate first.");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine("🔌 WebSocket connection boshlandi...");

                // Cleanup existing connection
                await DisconnectAsync();

                cancellationTokenSource = new CancellationTokenSource();
                webSocket = new ClientWebSocket();

                // WebSocket URI
                var uri = new Uri($"ws://{ServerIP}:{ServerPort}/ws/chat?user={Username}&token={AuthToken}");
                System.Diagnostics.Debug.WriteLine($"Connecting to: {uri}");

                // Connect
                await webSocket.ConnectAsync(uri, cancellationTokenSource.Token);

                if (webSocket.State == WebSocketState.Open)
                {
                    isConnected = true;
                    ConnectionStatusChanged?.Invoke(true);

                    // Start listening
                    _ = Task.Run(ListenForMessagesAsync);

                    // Get online users
                    await GetOnlineUsersAsync();

                    System.Diagnostics.Debug.WriteLine("✅ WebSocket muvaffaqiyatli ulandi");
                    return true;
                }
                else
                {
                    throw new Exception($"WebSocket connection failed. State: {webSocket.State}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ WebSocket connection xatolik: {ex.Message}");
                isConnected = false;
                ConnectionStatusChanged?.Invoke(false);
                ErrorOccurred?.Invoke($"Connection error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// WebSocket xabarlarini tinglash
        /// </summary>
        private async Task ListenForMessagesAsync()
        {
            var buffer = new byte[4096];

            try
            {
                while (webSocket?.State == WebSocketState.Open &&
                        !cancellationTokenSource?.Token.IsCancellationRequested == true)
                {
                    var result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationTokenSource?.Token ?? CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        System.Diagnostics.Debug.WriteLine($"📨 Qabul qilindi: {message}");

                        // Xabarni parse qilish
                        await ParseIncomingMessage(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        System.Diagnostics.Debug.WriteLine("🔌 Server tomonidan yopildi");
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("🔌 Listening bekor qilindi");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Listening xatolik: {ex.Message}");
                ErrorOccurred?.Invoke($"Message listening error: {ex.Message}");
            }
            finally
            {
                isConnected = false;
                ConnectionStatusChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Kelgan xabarlarni parse qilish
        /// </summary>
        private async Task ParseIncomingMessage(string jsonMessage)
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        var messageObj = JsonSerializer.Deserialize<JsonElement>(jsonMessage);

                        if (messageObj.TryGetProperty("type", out var typeElement))
                        {
                            string messageType = typeElement.GetString() ?? "";

                            switch (messageType.ToLower())
                            {
                                case "message":
                                case "new_message":
                                case "message_sent":
                                case "send_message":
                                    HandleIncomingChatMessage(messageObj);
                                    break;

                                case "online_users":
                                    HandleOnlineUsersUpdate(messageObj);
                                    break;

                                case "user_joined":
                                case "user_left":
                                    HandleUserStatusChange(messageObj);
                                    break;

                                case "pong":
                                    System.Diagnostics.Debug.WriteLine("🏓 Pong qabul qilindi");
                                    break;

                                default:
                                    System.Diagnostics.Debug.WriteLine($"⚠️ Noma'lum message type: {messageType}");
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Message parse xatolik: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ParseIncomingMessage xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Chat xabarini handle qilish
        /// </summary>
        private async void HandleIncomingChatMessage(JsonElement messageObj)
        {
            try
            {
                if (messageObj.TryGetProperty("data", out var dataElement))
                {
                    string fromUser = "";
                    string messageText = "";

                    if (dataElement.TryGetProperty("fromUser", out var fromElement))
                        fromUser = fromElement.GetString() ?? "";

                    if (dataElement.TryGetProperty("text", out var textElement) && !string.IsNullOrEmpty(textElement.GetString()))
                        messageText = AesEncryptionService.Decrypt(textElement.GetString(), CredentialsManager.GetInstance().Token) ?? "";

                    if (dataElement.TryGetProperty("messageType", out var messageTypeElement) && messageTypeElement.GetString() == "file")
                    {
                        if (dataElement.TryGetProperty("fileName", out var fileNameElement) && dataElement.TryGetProperty("fileContent", out var fileContent))
                        {
                            string fileName = fileNameElement.GetString() ?? "unknown_file";
                            messageText = $"📎 Fayl: {fileName}";
                            string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                            await File.WriteAllBytesAsync(savePath, fileContent.GetBytesFromBase64());
                            MessageBox.Show($"Fayl saqlandi: {savePath}", "Fayl saqlandi", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                    /*
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var fileMessage = JsonSerializer.Deserialize<FileMessageDto>(json);

                        if (fileMessage?.FileContent != null)
                        {
                            string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                                                           fileMessage.FileName);
                            await File.WriteAllBytesAsync(savePath, fileMessage.FileContent);
                            MessageBox.Show($"File saved to: {savePath}");
                        }
                    }
                    */

                    if (!string.IsNullOrEmpty(fromUser) && !string.IsNullOrEmpty(messageText))
                    {
                        MessageReceived?.Invoke(fromUser, messageText, DateTime.Now);
                        System.Diagnostics.Debug.WriteLine($"💬 Xabar: {fromUser} -> {messageText}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ HandleIncomingChatMessage xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Online userlar ro'yxatini handle qilish
        /// </summary>
        private void HandleOnlineUsersUpdate(JsonElement messageObj)
        {
            try
            {
                var users = new List<string>();

                if (messageObj.TryGetProperty("users", out var usersElement) &&
                    usersElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var userElement in usersElement.EnumerateArray())
                    {
                        string? userName = userElement.GetString();
                        if (!string.IsNullOrEmpty(userName))
                        {
                            users.Add(userName);
                        }
                    }
                }

                OnlineUsersUpdated?.Invoke(users);
                System.Diagnostics.Debug.WriteLine($"👥 Online users: {string.Join(", ", users)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ HandleOnlineUsersUpdate xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// User status o'zgarishini handle qilish
        /// </summary>
        private void HandleUserStatusChange(JsonElement messageObj)
        {
            try
            {
                // User joined/left eventlarini handle qilish
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500); // Kichik delay
                    await GetOnlineUsersAsync();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ HandleUserStatusChange xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Xabar yuborish
        /// </summary>
        public async Task<bool> SendMessageAsync(string toUser, string messageText)
        {
            try
            {
                if (!IsConnected)
                {
                    ErrorOccurred?.Invoke("Not connected to server");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(messageText))
                {
                    return false;
                }

                var message = new
                {
                    type = "send_message",
                    user = Username,
                    data = new
                    {
                        fromUser = Username,
                        toUser = toUser,
                        text = AesEncryptionService.Encrypt(messageText.Trim(), CredentialsManager.GetInstance().Token),
                        messageType = "text",
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    }
                };

                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);

                await webSocket!.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);

                System.Diagnostics.Debug.WriteLine($"📤 Xabar yuborildi: {toUser} -> {messageText}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SendMessage xatolik: {ex.Message}");
                ErrorOccurred?.Invoke($"Send message error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendFileAsync(string toUser, string dialogFileName)
        {
            try
            {
                if (!IsConnected)
                {
                    ErrorOccurred?.Invoke("Not connected to server");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(dialogFileName))
                    return false;

                var fileName = Path.GetFileName(dialogFileName);
                var fileBytes = await File.ReadAllBytesAsync(dialogFileName);

                var message = new
                {
                    type = "send_message",
                    user = Username,
                    data = new
                    {
                        fromUser = Username,
                        toUser = toUser,
                        fileName = fileName,
                        fileContent = fileBytes,
                        messageType = "file",
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    }
                };

                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);

                await webSocket!.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);

                System.Diagnostics.Debug.WriteLine($"📤 Xabar yuborildi: {toUser} -> {dialogFileName}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SendMessage xatolik: {ex.Message}");
                ErrorOccurred?.Invoke($"Send message error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Online userlar ro'yxatini so'rash
        /// </summary>
        public async Task GetOnlineUsersAsync()
        {
            try
            {
                if (!IsConnected)
                    return;

                var message = new
                {
                    type = "get_online_users",
                    user = Username
                };

                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);

                await webSocket!.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);

                System.Diagnostics.Debug.WriteLine("📋 Online users so'ralindi");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GetOnlineUsers xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Ulanishni yopish
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = null;
                }

                if (webSocket != null)
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Client disconnect",
                            CancellationToken.None);
                    }

                    webSocket.Dispose();
                    webSocket = null;
                }

                isConnected = false;
                ConnectionStatusChanged?.Invoke(false);

                System.Diagnostics.Debug.WriteLine("🔌 WebSocket ulanishi yopildi");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Disconnect xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Resurslarni tozalash
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;

            try
            {
                _ = Task.Run(async () => await DisconnectAsync());
                isDisposed = true;

                System.Diagnostics.Debug.WriteLine("✅ ChatService disposed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Dispose xatolik: {ex.Message}");
            }
        }

        ~ChatService()
        {
            Dispose();
        }
    }

    /// <summary>
    /// Login response modeli
    /// </summary>
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}