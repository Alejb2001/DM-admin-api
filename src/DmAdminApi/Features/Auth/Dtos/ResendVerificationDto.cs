using System.ComponentModel.DataAnnotations;

namespace DmAdminApi.Features.Auth.Dtos;

public record ResendVerificationDto([Required, EmailAddress] string Email);
