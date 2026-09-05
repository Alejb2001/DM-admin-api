using System.ComponentModel.DataAnnotations;

namespace DmAdminApi.Features.Auth.Dtos;

public record RegisterDto(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(100)] string Password,
    [Required, MaxLength(100)] string DisplayName
);
