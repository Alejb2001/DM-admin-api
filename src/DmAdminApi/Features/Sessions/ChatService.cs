using System.Text.Json;
using DmAdminApi.Features.Sessions.Dtos;
using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;

namespace DmAdminApi.Features.Sessions;

public class ChatService(AppDbContext db)
{
    public async Task<ChatMessage> ProcessMessageAsync(Guid sessionId, Guid userId, SendMessageDto dto)
    {
        DiceResultDto? diceResult = null;
        var type = dto.Type;
        var isSecret = dto.IsSecret;

        if (type is "roll" or "secret_roll")
        {
            var parsed = DiceParser.Parse(dto.Content);
            if (parsed.IsSecret) { type = "secret_roll"; isSecret = true; }
            diceResult = DiceRoller.Roll(parsed, dto.Content);
        }

        var message = new ChatMessage
        {
            SessionId = sessionId,
            UserId = userId,
            Type = type,
            Content = dto.Content,
            DiceResultJson = diceResult is not null
                ? JsonSerializer.Serialize(diceResult, JsonSerializerOptions.Web)
                : null,
            IsSecret = isSecret,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.ChatMessages.Add(message);
        await db.SaveChangesAsync();

        return message;
    }

    public static DiceResultDto? DeserializeDiceResult(string? json)
    {
        if (json is null) return null;
        return JsonSerializer.Deserialize<DiceResultDto>(json, JsonSerializerOptions.Web);
    }
}
