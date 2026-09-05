using System.Text;
using DmAdminApi.Common.Middleware;
using DmAdminApi.Features.Auth;
using DmAdminApi.Features.Campaigns;
using DmAdminApi.Features.Hubs;
using DmAdminApi.Features.Permissions;
using DmAdminApi.Features.Subscriptions;
using DmAdminApi.Features.World;
using DmAdminApi.Infrastructure.Auth;
using DmAdminApi.Infrastructure.Email;
using DmAdminApi.Infrastructure.Stripe;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace DmAdminApi.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        services.Configure<JwtSettings>(jwtSection);
        services.AddSingleton<JwtService>();

        var settings = jwtSection.Get<JwtSettings>()!;
        var key = Encoding.UTF8.GetBytes(settings.Key);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero,
                };

                // Support JWT in SignalR WebSocket connections
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var accessToken = ctx.Request.Query["access_token"];
                        var path = ctx.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            ctx.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<CampaignService>();
        services.AddScoped<PermissionService>();
        services.AddScoped<WorldEntityService>();
        services.AddScoped<EntityTypeService>();
        services.AddScoped<RelationshipService>();
        services.AddScoped<SubscriptionService>();
        services.AddSingleton<PlanLimits>();
        services.AddSingleton<PresenceTracker>();
        services.AddScoped<ExportService>();
        services.Configure<StripeSettings>(configuration.GetSection("Stripe"));
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.AddScoped<IEmailService, SmtpEmailService>();
        return services;
    }

    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:4200"];

        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials(); // Needed for SignalR
            });
        });

        return services;
    }
}
