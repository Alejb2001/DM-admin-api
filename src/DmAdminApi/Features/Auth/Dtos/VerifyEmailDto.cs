using System.ComponentModel.DataAnnotations;

namespace DmAdminApi.Features.Auth.Dtos;

public record VerifyEmailDto([Required] string Token);
