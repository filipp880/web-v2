using TramWebApp.Domain.Enums;

namespace TramWebApp.Application.DTOs;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    UserRole Role,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    bool IsActive
);

public record UserProfileDto(
    Guid Id,
    string Username,
    string Email,
    UserRole Role,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public record RegisterDto(
    string Username,
    string Email,
    string Password
);

public record LoginDto(
    string Username,
    string Password
);

public record LoginResponseDto(
    string AccessToken,
    UserDto User
);

public record ChangePasswordDto(
    string CurrentPassword,
    string NewPassword
);
