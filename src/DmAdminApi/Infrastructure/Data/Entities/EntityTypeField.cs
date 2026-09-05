namespace DmAdminApi.Infrastructure.Data.Entities;

public class EntityTypeField
{
    public Guid Id { get; set; }
    public Guid EntityTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;  // text|number|date|boolean|reference|richtext|url
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }

    public EntityType EntityType { get; set; } = null!;
}

public static class FieldTypes
{
    public const string Text = "text";
    public const string Number = "number";
    public const string Date = "date";
    public const string Boolean = "boolean";
    public const string Reference = "reference";
    public const string RichText = "richtext";
    public const string Url = "url";
}
