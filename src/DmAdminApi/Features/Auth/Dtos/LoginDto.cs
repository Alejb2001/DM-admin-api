using System.ComponentModel.DataAnnotations;

namespace DmAdminApi.Features.Auth.Dtos;

public record LoginDto(
    [Required, EmailAddress] string Email,
    [Required] string Password
);
