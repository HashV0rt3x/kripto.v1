using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace kripto.Helpers
{
    class chatService
    {
        public chatService()
        {
        }
        private static readonly HttpClient httpClient = new HttpClient();
        private static ClientWebSocket? webSocket;
        private static CancellationTokenSource? cancellationTokenSource;

        private static readonly string SERVER_IP = "37.27.216.90";
        private static readonly string SERVER_PORT = "8099";
        private static readonly string USERNAME = "user1";
        private static readonly string PASSWORD = "user123";

        public static async Task<string?> GetAuthTokenAsync()
        {
            try
            {
                Console.WriteLine("🔐 Getting auth token...");

                var loginRequest = new
                {
                    username = USERNAME,
                    password = PASSWORD
                };

                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(
                    $"http://{SERVER_IP}:{SERVER_PORT}/api/auth/login",
                    content);

                Console.WriteLine($"Auth Response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Auth Response Body: {responseContent}");

                    // Parse the actual response structure
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return loginResponse?.Token;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Auth failed: {errorContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Auth error: {ex.Message}");
                return null;
            }
        }

        public static async Task TestWebSocketAsync(string? authToken)
        {
            try
            {
                Console.WriteLine("\n🔌 Testing WebSocket connection...");

                cancellationTokenSource = new CancellationTokenSource();
                webSocket = new ClientWebSocket();
                // Server expects token as query parameter
                var uri = new Uri($"ws://{SERVER_IP}:{SERVER_PORT}/ws/chat?user={USERNAME}&token={authToken}");
                Console.WriteLine($"Connecting to: {uri}");

                await webSocket.ConnectAsync(uri, cancellationTokenSource.Token);

                Console.WriteLine("✅ WebSocket connected successfully!");
                Console.WriteLine($"State: {webSocket.State}");

                // Start listening for messages
                _ = Task.Run(ListenForMessagesAsync);

                // Send a test message
                await SendTestMessageAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ WebSocket connection failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        public static async Task ListenForMessagesAsync()
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
                        Console.WriteLine($"📨 Received: {message}");
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("🔌 WebSocket connection closed by server");
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("🔌 WebSocket listening cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ WebSocket listening error: {ex.Message}");
            }
        }

        public static async Task SendTestMessageAsync()
        {
            try
            {
                if (webSocket?.State == WebSocketState.Open)
                {
                    var testMessage = new
                    {
                        type = "send_message",
                        user = USERNAME,
                        data = new
                        {
                            fromUser = USERNAME,
                            toUser = "testuser2",
                            text = "Hello from console test!",
                            messageType = "text"
                        }
                    };

                    var json = JsonSerializer.Serialize(testMessage);
                    var bytes = Encoding.UTF8.GetBytes(json);

                    await webSocket.SendAsync(
                        new ArraySegment<byte>(bytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);

                    Console.WriteLine("📤 Test message sent!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Send message error: {ex.Message}");
            }
        }

        public static async Task SendPingAsync()
        {
            if (webSocket?.State == WebSocketState.Open)
            {
                var pingMessage = new
                {
                    type = "ping",
                    user = USERNAME
                };

                var json = JsonSerializer.Serialize(pingMessage);
                var bytes = Encoding.UTF8.GetBytes(json);

                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);

                Console.WriteLine("🏓 Ping sent!");
            }
            else
            {
                Console.WriteLine("❌ WebSocket not connected");
            }
        }

        public static async Task SendCustomMessageAsync(string toUser, string text)
        {
            if (webSocket?.State == WebSocketState.Open)
            {
                var message = new
                {
                    type = "send_message",
                    user = USERNAME,
                    data = new
                    {
                        fromUser = USERNAME,
                        toUser = toUser,
                        text = text,
                        messageType = "text"
                    }
                };

                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);

                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);

                Console.WriteLine($"📤 Message sent to {toUser}: {text}");
            }
            else
            {
                Console.WriteLine("❌ WebSocket not connected");
            }
        }

        public static async Task GetOnlineUsersAsync()
        {
            if (webSocket?.State == WebSocketState.Open)
            {
                var message = new
                {
                    type = "get_online_users",
                    user = USERNAME
                };

                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);

                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);

                Console.WriteLine("📋 Requested online users list...");
            }
            else
            {
                Console.WriteLine("❌ WebSocket not connected");
            }
        }

        public class LoginResponse
        {
            public bool Success { get; set; }
            public string? Token { get; set; }
            public string? Message { get; set; }
            public DateTime Timestamp { get; set; }
        }

    }
}
