using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace kripto.Helpers
{
    public class ChatHistoryService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        public static string SERVER_IP = "37.27.216.90";
        private static readonly string SERVER_PORT = "8099";

        public static string? AuthToken { get; set; }
        public ChatHistoryService(string _authToken)
        {
            AuthToken = _authToken;
        }
        // Auth token ni saqlash uchun

        // Chat history ni Dictionary<string,string> format da olish
        public static async Task<List<ChatMessageDto>> GetHistoryAsync(string targetUser,string authToken)
        {
            try
            {
                AuthToken = authToken; 
                // Agar token yo'q bo'lsa, auth qilish
                if (string.IsNullOrEmpty(AuthToken))
                {
                    if (string.IsNullOrEmpty(AuthToken))
                    {
                        Console.WriteLine("❌ Auth token olishda xatolik");
                        return null;
                    }
                }

                // Authorization header qo'shish
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {AuthToken}");

                // API ga so'rov yuborish
                var response = await httpClient.GetAsync(
                    $"http://{SERVER_IP}:{SERVER_PORT}/api/Chat/history/{targetUser}");

                Console.WriteLine($"History Response Status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"History Response: {responseContent}");

                    // Response ni parse qilish
                    var apiResponse = JsonSerializer.Deserialize<ChatHistoryApiResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        // ChatResponseDto dan Dictionary ga o'zgartirish
                        //var result = new Dictionary<string, string>();

                        //foreach (var message in apiResponse.Data)
                        //{
                        //    // Message ID ni key, Text ni value sifatida
                        //    if (!string.IsNullOrEmpty(message.Id) && !string.IsNullOrEmpty(message.Text))
                        //    {
                        //        result[message.ToUser] = message.Text;
                        //    }
                        //}

                        return apiResponse.Data;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ History API Error: {errorContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetHistoryAsync Error: {ex.Message}");
                return null;
            }
        }

        // Alternative: Simplified Dictionary format uchun
        public static async Task<Dictionary<string, string>?> GetHistorySimpleAsync(string targetUser)
        {
            try
            {
                if (string.IsNullOrEmpty(AuthToken))
                {
                    if (string.IsNullOrEmpty(AuthToken))
                        return null;
                }

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {AuthToken}");

                // Agar serverda history-simple endpoint mavjud bo'lsa
                var response = await httpClient.GetAsync(
                    $"http://{SERVER_IP}:{SERVER_PORT}/api/Chat/history-simple/{targetUser}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    var apiResponse = JsonSerializer.Deserialize<SimpleDictResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return apiResponse?.Data;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetHistorySimpleAsync Error: {ex.Message}");
                return null;
            }
        }

        // Xabar yuborish methodi ham qo'shamiz
        public static async Task<bool> SendMessageAsync(string toUser, string message, string messageType = "text")
        {
            try
            {
                if (string.IsNullOrEmpty(AuthToken))
                {
                    if (string.IsNullOrEmpty(AuthToken))
                        return false;
                }

                var sendRequest = new
                {
                    fromUser = "user1", // Yoki current user
                    toUser = toUser,
                    text = message,
                    messageType = messageType,
                    roomId = ""
                };

                var json = JsonSerializer.Serialize(sendRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {AuthToken}");

                var response = await httpClient.PostAsync(
                    $"http://{SERVER_IP}:{SERVER_PORT}/api/Chat/send", content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SendMessageAsync Error: {ex.Message}");
                return false;
            }
        }
    }

    // Response model lari
    public class ChatHistoryApiResponse
    {
        public bool Success { get; set; }
        public List<ChatMessageDto>? Data { get; set; }
    }

    public class ChatMessageDto
    {
        public string Id { get; set; } = "";
        public string FromUser { get; set; } = "";
        public string FromUserId { get; set; } = "";
        public string ToUser { get; set; } = "";
        public string Text { get; set; } = "";
        public string MessageType { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool IsRecipientOnline { get; set; }
        public string? RoomId { get; set; }
    }

    public class SimpleDictResponse
    {
        public bool Success { get; set; }
        public Dictionary<string, string>? Data { get; set; }
    }
}