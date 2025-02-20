
namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnPhone : ColumnBaseType
{
    public string? Phone { get; set; }

    public ColumnPhone() { }

    public ColumnPhone(string? id, string? phone)
    {
        base.Id = id;
        Phone = phone;
    }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Phone))
        {
            return "\"" + base.Id + "\" : null";
        }

        return "\"" + base.Id + "\":{\"phone\":\"" + Phone + "\"}";
    }
}