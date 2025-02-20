using Newtonsoft.Json;

namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnPhone : ColumnBaseType
{
    public string? Phone { get; set; }

    /// <summary>
    /// 2-letter country code
    /// see https://developer.monday.com/api-reference/reference/phone
    /// </summary>
    [JsonProperty("countryShortName")]
    public string? CountryShortName { get; set; }

    public ColumnPhone() { }

    public ColumnPhone(string? id, string? phone)
    {
        Id = id;
        Phone = phone;
    }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Phone))
        {
            return "\"" + Id + "\" : null";
        }

        if (string.IsNullOrEmpty(CountryShortName))
        {
            return "\"" + Id + "\":{\"phone\":\"" + Phone + "\"}";
        }

        return "\"" + Id + "\":{\"phone\":\"" + Phone + "\",\"countryShortName\":\"" + CountryShortName + "\"}";
    }
}