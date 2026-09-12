namespace DmAdminApi.Infrastructure.Data.Entities;

public class TokenCondition
{
    public Guid Id { get; set; }
    public Guid TokenId { get; set; }
    public string Condition { get; set; } = string.Empty;

    public MapToken Token { get; set; } = null!;
}
