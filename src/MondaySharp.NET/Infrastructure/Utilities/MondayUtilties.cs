using MondaySharp.NET.Application.Attributes;
using MondaySharp.NET.Application.Entities;
using MondaySharp.NET.Domain.ColumnTypes;
using MondaySharp.NET.Domain.Common;
using MondaySharp.NET.Domain.Common.Enums;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MondaySharp.NET.Infrastructure.Utilities;

public static partial class MondayUtilties
{
    [GeneratedRegexAttribute(@"(http|ftp|https):\/\/([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:\/~+#-]*[\w@?^=%&\/~+#-])", RegexOptions.Compiled)]
    private static partial Regex UrlFromStringExtractor();

    /// <summary>
    /// Extact's A Url From The Given String Type.
    /// </summary>
    private static readonly Regex UrlRegex = UrlFromStringExtractor();


    public static readonly Dictionary<Type, string> GetItemsQueryBuilder = new()
    {
        { typeof(MondaySharp.NET.Application.Entities.Group), @"group { id title color archived deleted position }" },
        { typeof(List<MondaySharp.NET.Application.Entities.Asset>), @"assets { id name public_url url_thumbnail created_at }" },
        { typeof(List<MondaySharp.NET.Application.Entities.Update>), @"updates (limit: 100) { id text_body }" }
    };

    // Define the supported types and their corresponding error messages
    public static readonly Dictionary<Type, string> UnsupportedTypes = new()
    {
        { typeof(MondaySharp.NET.Application.Entities.Group), "Multiple Group Properties Are Not Supported." },
        { typeof(List<MondaySharp.NET.Application.Entities.Asset>), "Multiple Asset Properties Are Not Supported." },
        { typeof(List<MondaySharp.NET.Application.Entities.Update>), "Multiple Update Properties Are Not Supported." }
    };

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item"></param>
    /// <param name="destination"></param>
    /// <returns></returns>
    public static bool TryBindColumnDataAsync<T>(
        Dictionary<string, string> columnPropertyMap, Item item, ref T destination) where T : MondayRow, new()
    {
        // Get The Destination Type.
        Type? destinationType = destination?.GetType();

        // If The Destination Type Is Null, Return False.
        if (destinationType == null || destination == null) return false;

        // Assign Default Values.
        destination.Id = item.Id;
        destination.Name = new ColumnText(nameof(item.Name), item.Name);

        SetPropertyIfExists(destinationType, nameof(item.Group), item.Group, destination);
        SetPropertyIfExists(destinationType, nameof(item.Assets), item.Assets, destination);
        SetPropertyIfExists(destinationType, nameof(item.Updates), item.Updates, destination);

        // Loop Through All Columns.
        foreach (ColumnValue? columnValue in item.ColumnValues
            .Where(x => x.Type != Domain.Common.Enums.MondayColumnType.Subtasks))
        {
            if (columnPropertyMap.TryGetValue(columnValue.Id, out string? propertyName) || (columnValue.Id?.Replace(" ", "") == propertyName))
            {
                PropertyInfo? property = destinationType.GetProperty(propertyName);
                property?.SetValue(destination, CreateColumnTypeInstance(columnValue.Type, columnValue));
            }
        }

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="type"></param>
    /// <param name="propertyName"></param>
    /// <param name="value"></param>
    /// <param name="destination"></param>
    private static void SetPropertyIfExists<TValue, TDestination>(Type type, string propertyName, TValue value, TDestination destination)
    {
        PropertyInfo? property = type.GetProperty(propertyName);
        property?.SetValue(destination, value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Dictionary<string, string> GetColumnPropertyMap<T>()
    {
        // Create A Map Of Column Ids To Property Names.
        Dictionary<string, string> columnPropertyMap = [];

        // Loop Through All Properties In The Type.
        foreach (PropertyInfo property in typeof(T).GetProperties())
        {
            // Attempt to get the MondayColumnHeaderAttribute.
            MondayColumnHeaderAttribute? mondayColumnHeaderAttribute =
                property.GetCustomAttribute<MondayColumnHeaderAttribute>();

            // If the attribute is not null, add the column id to the map.
            if (mondayColumnHeaderAttribute != null)
            {
                // If the attribute is not null, add the column id to the map.
                columnPropertyMap[mondayColumnHeaderAttribute.ColumnId] = property.Name;
            }
            else
            {
                // If the attribute is null, add the property name to the map.
                columnPropertyMap[property.Name] = property.Name;
            }
        }

        // Return The Map.
        return columnPropertyMap;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="columnType"></param>
    /// <param name="column"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static object CreateColumnTypeInstance(MondayColumnType? columnType, ColumnValue column)
    {
        // Create The Column Type Instance.
        switch (columnType)
        {
            // Create The Column Type Instance.
            case MondayColumnType.Color_Picker:
                return new ColumnColorPicker(column.Id, !string.IsNullOrEmpty(column.Text) ? column.Text : null);

            case MondayColumnType.Text:
                return new ColumnText(column.Id, !string.IsNullOrEmpty(column.Text) ? column.Text : null);

            case MondayColumnType.Numbers:
                return new ColumnNumber(column.Id, !string.IsNullOrEmpty(column.Text) ? Convert.ToSingle(column.Text ?? "0") : null);

            case MondayColumnType.Date:
                return new ColumnDateTime(column.Id, !string.IsNullOrEmpty(column.Text) ? Convert.ToDateTime(column.Text) : null);

            case  MondayColumnType.Checkbox:
                return new ColumnCheckBox(column.Id, !string.IsNullOrEmpty(column.Text) && column.Text == "v");

            case MondayColumnType.Status:
                return new ColumnStatus(column.Id, !string.IsNullOrEmpty(column.Text) ? column.Text : null);

            case MondayColumnType.Timeline:

                // If The Column Text Is Not Null Or Empty, Split The Text.
                if (!string.IsNullOrEmpty(column.Text))
                {
                    // Split The Text.
                    string[] data = column.Text.Split(" - ");

                    if (data.Length != 2) throw new ArgumentException("Invalid timeline format!");

                    // Return The Column Time Range.
                    return new ColumnTimeline(column.Id, data[0], data[1]);
                }
                else
                {
                    // Return The Column Time Range.
                    return new ColumnTimeline(column.Id);
                }

            case MondayColumnType.Link:

                // If The Column Text Is Not Null Or Empty, Split The Text.
                if (!string.IsNullOrEmpty(column.Text))
                {
                    // Get The Url.
                    string? url = UrlRegex.Match(column.Text).Value;

                    // Get The Text.
                    string? _text = column.Text.Replace(url, "");

                    // Get The Text.
                    string? text = string.Empty;

                    // If The Text Length Is Greater Than 0, Set The Text.
                    if (_text.Length > 0)
                    {
                        // Set The Text.
                        text = column.Text.Replace(url, "")[..^3];
                    }

                    // Return The Column Link.
                    return new ColumnLink(column.Id, url, text);
                }
                else
                {
                    // Return The Column Link.
                    return new ColumnLink(column.Id);
                }

            case MondayColumnType.Dropdown:
                return new ColumnDropDown(column.Id, !string.IsNullOrEmpty(column.Text) ? column.Text : null);

            case MondayColumnType.Long_Text:
                return new ColumnLongText(column.Id, !string.IsNullOrEmpty(column.Text) ? column.Text : null);

            case MondayColumnType.Tags:
                return new ColumnTag(column.Id, !string.IsNullOrEmpty(column.Text) ? column.Text : null);

            default:
                throw new ArgumentException($"Unsupported column type: {columnType}");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="columnTypes"></param>
    /// <returns></returns>
    public static string ToColumnValuesJson(this List<ColumnBaseType>? columnTypes)
    {
        if (columnTypes == null) return string.Empty;
        if (columnTypes.Count == 0) return string.Empty;

        // Get the total count of all column types.
        int totalCount = columnTypes.Sum(GetColumnTypeLength);

        // Get the total length of the json string.
        int totalLength = totalCount + (columnTypes.Count - 1) + 2;

        // Create a span for the column types.
        Span<ColumnBaseType> columnTypesSpan = columnTypes.ToArray();

        // Create a span for the json string.
        Span<char> jsonChars = totalLength <= 256 ? stackalloc char[totalLength] : new char[totalLength];

        // Create a current index.
        int currentIndex = 0;

        // Add the opening bracket.
        jsonChars[currentIndex++] = '{';

        // Loop through all column types.
        for (int i = 0; i < columnTypesSpan.Length; i++)
        {
            // If the column type is not null, add it to the json string.
            if (columnTypesSpan[i] != null)
            {
                // ToString will have an override that will return the correct JSON format.
                string columnTypeString = columnTypesSpan[i].ToString();

                // Copy the string to the jsonChars span.
                columnTypeString.AsSpan().CopyTo(jsonChars[currentIndex..]);

                // Increment the current index by the length of the column type string.
                currentIndex += columnTypeString.Length;

                // If the current index is less than the column types span length - 1, add a comma.
                if (i < columnTypesSpan.Length - 1)
                {
                    // Add a comma.
                    jsonChars[currentIndex++] = ',';
                }
            }
        }

        // Add the closing bracket.
        jsonChars[currentIndex++] = '}';

        // Create a new string from the jsonChars span.
        string jsonString = new(jsonChars);

        // Remove any Json invalid characters.
        jsonString = jsonString.Replace("\r", string.Empty).Replace("\n", string.Empty);

        // If the json string is not valid, throw an exception.
        if (!IsValidJson(jsonString)) throw new System.Text.Json.JsonException("Invalid JSON format!");

        // Return the json string.
        return jsonString;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="columnType"></param>
    /// <returns></returns>
    private static int GetColumnTypeLength(ColumnBaseType columnType)
    {
        // If the column type is null, return 0.
        if (columnType == null) return 0;

        // Return the length of the column type.
        return columnType.ToString().Length;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="jsonString"></param>
    /// <returns></returns>
    private static bool IsValidJson(string jsonString)
    {
        try
        {
            // Attempt to parse the json string.
            using JsonDocument document = JsonDocument.Parse(jsonString);

            // Return true.
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
