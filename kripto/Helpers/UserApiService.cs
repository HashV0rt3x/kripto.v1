// Services/UserApiService.cs
using kripto.Helpers;
using kripto.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace kripto.Services
{
    public class UserApiService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private string? _authToken;
        private bool _disposed = false;

        public UserApiService(string baseUrl,string authToken)
        {
            _authToken = authToken;
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            SetAuthToken(authToken);
        }

        /// <summary>
        /// Authentication token o'rnatish
        /// </summary>
        public void SetAuthToken(string token)
        {
            //_authToken = token;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            System.Diagnostics.Debug.WriteLine($"API token o'rnatildi: {token.Substring(0, Math.Min(10, token.Length))}...");
        }

        /// <summary>
        /// Online userlarni olish
        /// </summary>
        public async Task<List<OnlineUserDto>> GetOnlineUsersAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"API'dan online userlar so'ralmoqda: {_baseUrl}/api/OnlineUsers");

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/OnlineUsers");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"API javob: {json.Substring(0, Math.Min(200, json.Length))}...");

                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<OnlineUserDto>>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"API'dan {apiResponse.Data.Count} user muvaffaqiyatli olindi");
                        return apiResponse.Data;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"API javob muvaffaqiyatsiz: {apiResponse?.Message}");
                        return new List<OnlineUserDto>();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"API HTTP error: {response.StatusCode} - {response.ReasonPhrase}");

                    // Error content'ni o'qish
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Error content: {errorContent}");

                    return new List<OnlineUserDto>();
                }
            }
            catch (HttpRequestException httpEx)
            {
                System.Diagnostics.Debug.WriteLine($"HTTP request error: {httpEx.Message}");
                return new List<OnlineUserDto>();
            }
            catch (TaskCanceledException timeoutEx)
            {
                System.Diagnostics.Debug.WriteLine($"Request timeout: {timeoutEx.Message}");
                return new List<OnlineUserDto>();
            }
            catch (JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine($"JSON parsing error: {jsonEx.Message}");
                return new List<OnlineUserDto>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetOnlineUsersAsync umumiy xatolik: {ex.Message}");
                return new List<OnlineUserDto>();
            }
        }

        /// <summary>
        /// Barcha foydalanuvchilarni olish (pagination va qidiruv bilan)
        /// </summary>
        public async Task<UserListResponse> GetAllUsersAsync(string? search = null, int page = 1, int pageSize = 20)
        {
            try
            {
                //// Token borligini tekshiramiz
                //if (string.IsNullOrWhiteSpace(_authToken))
                //    throw new InvalidOperationException("Avtorizatsiya tokeni mavjud emas.");

                // Tokenni headerga qo‘shamiz
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiJlNmU4YWRlZS05N2EzLTQ5NzktYTEwOC02MDBmYWM0OTkyZDciLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjAxOTgyMGUzLTE5MTgtN2JhOC1iYjkyLTYyODZhZjUxM2U5ZiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJzdHJpbmciLCJVc2VySWQiOiIwMTk4MjBlMy0xOTE4LTdiYTgtYmI5Mi02Mjg2YWY1MTNlOWYiLCJVc2VyTmFtZSI6InN0cmluZyIsImV4cCI6MTc1MzA4NzM0OCwiaXNzIjoiQmFjayIsImF1ZCI6ImtyaXB0by5VeiJ9.fJvXKfTj3WridFkmF0fxJDvCnp2bJlFZ4SCjNTenO6E");

                // Query parametrlari
                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(search))
                    queryParams.Add($"search={Uri.EscapeDataString(search)}");

                queryParams.Add($"page={page}");
                queryParams.Add($"pageSize={pageSize}");

                var query = string.Join("&", queryParams);
                var url = $"http://localhost:5530/api/Users?";

                // Foydalanuvchilarni olish so‘rovi
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    // JSON dan modelga deserialize qilish
                    var userListResponse = JsonSerializer.Deserialize<UserListResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return userListResponse ?? new UserListResponse();
                }
                else
                {
                    // Xatolik bo‘lsa log qilamiz
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ GetAllUsers API xatolik: {(int)response.StatusCode} - {response.ReasonPhrase} | {errorContent}");

                    return new UserListResponse();
                }
            }
            catch (Exception ex)
            {
                // Istisno holatlarini log qilamiz
                System.Diagnostics.Debug.WriteLine($"❌ GetAllUsersAsync xatolik: {ex.Message}");
                return new UserListResponse();
            }
        }


        /// <summary>
        /// Online userlar sonini olish
        /// </summary>
        public async Task<int> GetOnlineCountAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/OnlineUsers/count");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<int>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return apiResponse?.Data ?? 0;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"GetOnlineCount API error: {response.StatusCode}");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetOnlineCountAsync xatolik: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// User status tekshirish
        /// </summary>
        public async Task<bool> IsUserOnlineAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/OnlineUsers/check/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<UserStatusDto>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return apiResponse?.Data?.IsOnline ?? false;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IsUserOnlineAsync xatolik: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Multiple userlarning statusini tekshirish
        /// </summary>
        public async Task<List<UserStatusDto>> CheckMultipleUsersStatusAsync(List<string> userIds)
        {
            try
            {
                var json = JsonSerializer.Serialize(userIds);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/OnlineUsers/check-multiple", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<UserStatusDto>>>(responseJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return apiResponse?.Data ?? new List<UserStatusDto>();
                }
                else
                {
                    return new List<UserStatusDto>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckMultipleUsersStatusAsync xatolik: {ex.Message}");
                return new List<UserStatusDto>();
            }
        }

        /// <summary>
        /// Online users statistikasini olish
        /// </summary>
        public async Task<OnlineStatsDto?> GetOnlineStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/OnlineUsers/stats");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<OnlineStatsDto>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return apiResponse?.Data;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetOnlineStatsAsync xatolik: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// API server mavjudligini tekshirish
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"API server bog'lanishi tekshirilmoqda: {_baseUrl}");

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/health");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API connection test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Dispose pattern implementation
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}