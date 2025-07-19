// Models/DTOs.cs
using System;
using System.Collections.Generic;

namespace kripto.Models
{
    // API Response wrapper
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public int Count { get; set; }
    }

    // User List Response
    public class UserListResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<UserDto> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    // Online User DTO
    public class OnlineUserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int ConnectionCount { get; set; }
        public DateTime ConnectedAt { get; set; }
        public DateTime LastActivity { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    // User DTO
    public class UserDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public string? Role { get; set; }
    }

    // User Status DTO
    public class UserStatusDto
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public int ConnectionCount { get; set; }
        public DateTime CheckedAt { get; set; }
        public DateTime? LastSeen { get; set; }
    }

    // Online Statistics DTO
    public class OnlineStatsDto
    {
        public int TotalOnlineUsers { get; set; }
        public int TotalConnections { get; set; }
        public double AverageConnectionsPerUser { get; set; }
        public DateTime? OldestConnection { get; set; }
        public DateTime? NewestConnection { get; set; }
        public Dictionary<string, int> UsersByHour { get; set; } = new();
        public Dictionary<string, int> ConnectionsByRegion { get; set; } = new();
    }
}