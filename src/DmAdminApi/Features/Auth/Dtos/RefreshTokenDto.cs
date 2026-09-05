using System.ComponentModel.DataAnnotations;

namespace DmAdminApi.Features.Auth.Dtos;

public record RefreshTokenDto(
    [Required] string RefreshToken
);
