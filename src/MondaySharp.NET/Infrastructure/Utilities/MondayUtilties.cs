using MondaySharp.NET.Application.Attributes;
using MondaySharp.NET.Application.Entities;
using MondaySharp.NET.Domain.ColumnTypes;
using MondaySharp.NET.Domain.Common;
using MondaySharp.NET.Domain.Common.Enums;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using GraphQL;
using MondaySharp.NET.Application.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Collections;

namespace MondaySharp.NET.Infrastructure.Utilities;

internal static partial class MondayUtilities
{
    [GeneratedRegexAttribute(@"(http|ftp|https):\/\/([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:\/~+#-]*[\w@?^=%&\/~+#-])", RegexOptions.Compiled)]
    private static partial Regex UrlFromStringExtractor();

    private static CultureInfo Culture => CultureInfo.CurrentCulture;

    /// <summary>
    /// Extact's A Url From The Given String Type.
    /// </summary>
    private static readonly Regex UrlRegex = UrlFromStringExtractor();

    // Define the supported types and their corresponding query builders
    internal static readonly Dictionary<Type, string> GetItemsQueryBuilder = new()
    {
        { typeof(Application.Entities.Group), @"group { id title color archived deleted position }" },
        { typeof(List<Asset>), @"assets { id name public_url url_thumbnail created_at }" },
        { typeof(List<Update>), @"updates (limit: 100) { id text_body }" },
        { typeof(MondaySubRowLite),
            @"subitems { 
                id 
                name
            }"
        },
        { typeof(MondaySubRow),
            @"subitems { 
                id 
                name
                column_values {
                  id
                  text
                  type
                  value
                }
            }"
        }
    };

    // Define the supported types and their corresponding error messages
    internal static readonly Dictionary<Type, string> UnsupportedTypes = new()
    {
        { typeof(Application.Entities.Group), "Multiple Group Properties Are Not Supported." },
        { typeof(List<Asset>), "Multiple Asset Properties Are Not Supported." },
        { typeof(List<Update>), "Multiple Update Properties Are Not Supported." }
    };

    // https://developer.monday.com/api-reference/reference/column-values-v2#using-fragments-to-get-column-specific-fields
    internal static readonly Dictionary<Type, string> ColumnValueFragments = new()
    {
        {
            typeof(ColumnPeopleAndTeams),
            """
            ... on PeopleValue {
                persons_and_teams {
                    id
                    kind
                }
            }
            """
        }
    };

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item"></param>
    /// <param name="destination"></param>
    /// <returns></returns>
    internal static bool TryBindColumnData<T>(
        IReadOnlyDictionary<string, string> columnPropertyMap,
        IMondayBindableSource source,
        ref T destination)
        where T : IMondayIdentity, new()
    {
        destination.Id = source.Id;
        destination.Name = source.Name;

        Type destinationType = destination.GetType();

        // Set the common properties if they exist in the destination type.
        SetPropertyIfExists(destinationType, "Group", source.Group, destination);
        SetPropertyIfExists(destinationType, "Assets", source.Assets, destination);
        SetPropertyIfExists(destinationType, "Updates", source.Updates, destination);

        // Loop the main row column values and set the properties if they exist in the destination type.
        foreach (ColumnValue columnValue in source.ColumnValues.Where(x => x.Type != MondayColumnType.Subtasks))
        {
            _ = TryBindColumnValue(columnPropertyMap, destination, destinationType, columnValue);
        }

        // Attempt to get any List of SubItems properties on the destination type. If they exist, we will attempt to bind the subitems to them.
        PropertyInfo? subItemsProperty = destinationType.GetProperties()
            .FirstOrDefault(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(List<>)
            && (
                p.PropertyType.GetGenericArguments().FirstOrDefault()?.BaseType == typeof(MondaySubRow)) || 
                p.PropertyType.GetGenericArguments().FirstOrDefault() == typeof(MondaySubRow)
            );

        // Get the subItems Type.
        Type? subItemsDestinationType = subItemsProperty?.PropertyType.GetGenericArguments()
            .FirstOrDefault(x => x.BaseType == typeof(MondaySubRow) || x == typeof(MondaySubRow));

        // Check if the subItem Type was found.
        if (subItemsDestinationType == null) return true;

        // Create the blueprint.
        Type listType = typeof(List<>).MakeGenericType(subItemsDestinationType);

        // Create the list instance.
        IList? subItemList = (IList?)Activator.CreateInstance(listType);

        // Check if the list was created.
        if (subItemList == null) return true;

        // If there are no subitem properties, we can return true at this point since there is nothing left to bind.
        if (subItemsProperty == null || subItemsDestinationType == null) return true;

        // Assign the subItemsList to the destination.
        subItemsProperty.SetValue(destination, subItemList);

        // Get the MondaySubRow default properties so we can ignore them when binding subitem column values.
        HashSet<string> subItemDefaultProperties = [.. typeof(MondaySubRow).GetProperties().Select(p => p.Name)];

        // Loop the subitem column values and set the properties if they exist in the destination type.
        foreach (SubItem subItem in source.SubItems)
        {
            // Create a new instance of the subitem type and attempt to bind the column values to it.
            object? subItemInstance = Activator.CreateInstance(subItemsDestinationType);

            // If the subitem instance is null, continue to the next subitem.
            if (subItemInstance == null) continue;

            // Loop each of the default properties of the MondaySubRow and attempt
            // to set them on the subitem instance if they exist in the destination type.
            // We want to do this before setting the column values because the column values may have the same name as the default properties,
            foreach (string defaultProperty in subItemDefaultProperties)
            {
                // Attempt to get the property value from the subitem.
                PropertyInfo? property = subItemInstance.GetType().GetProperty(defaultProperty);

                // Check if it was found, should be.
                if (property == null) continue;

                // Attempt to get the value from the subitem.
               property.SetValue(subItemInstance, subItem.GetType().GetProperty(defaultProperty)?.GetValue(subItem));
            }

            // Loop the column values of the subitem and set the properties if they exist in the destination type.
            foreach (ColumnValue columnValue in subItem.ColumnValues.Where(x => x.Type != MondayColumnType.Subtasks))
            {
                _ = TryBindColumnValue(columnPropertyMap, subItemInstance, subItemsDestinationType, columnValue);
            }

            // Add the subitem instance to the list of subitems on the destination instance.
            subItemList.Add(subItemInstance);
        }
        
        return true;

        static bool TryBindColumnValue(IReadOnlyDictionary<string, string> columnPropertyMap, object destination, Type destinationType, ColumnValue columnValue)
        {
            string? rawId = columnValue.Id;
            if (string.IsNullOrWhiteSpace(rawId)) return false;

            // normalize variants once
            string noSpaces = rawId.Replace(" ", "");
            string pascal = rawId.Length == 0 ? rawId : char.ToUpper(rawId[0], Culture) + rawId[1..];

            if (!TryResolvePropertyName(columnPropertyMap, rawId, pascal, noSpaces, out string? propertyName)) return false;
            if (string.IsNullOrWhiteSpace(propertyName)) return false;

            PropertyInfo? prop = destinationType.GetProperty(propertyName);
            if (prop == null || !prop.CanWrite) return false;

            prop.SetValue(destination, CreateColumnTypeInstance(columnValue.Type, columnValue));
            return true;
        }
    }
    
    private static bool TryResolvePropertyName(
        IReadOnlyDictionary<string, string> map,
        string rawId,
        string pascalId,
        string noSpacesId,
        out string? propertyName)
    {
        if (map.TryGetValue(rawId, out propertyName)) return true;
        if (map.TryGetValue(pascalId, out propertyName)) return true;
        if (map.TryGetValue(noSpacesId, out propertyName)) return true;

        propertyName = null;
        return false;
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
    private static void SetPropertyIfExists<TValue, TDestination>(Type type, string propertyName, TValue value,
        TDestination destination)
    {
        PropertyInfo? property = type.GetProperty(propertyName);
        property?.SetValue(destination, value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    internal static Dictionary<string, string> GetColumnPropertyMap<T>()
    {
        // Create A Map Of Column Ids To Property Names.
        Dictionary<string, string> columnPropertyMap = [];

        // Flatten The Type's Properties And Subitem Properties Into A Single List.
        List<PropertyInfo> propertyInfos = [];

        // Loop Through All Properties In The Type.
        foreach (PropertyInfo property in typeof(T).GetProperties())
        {
            // Check If The Property Is A List Of Subitems.
            if (property.PropertyType.IsGenericType 
                && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>)
                && property.PropertyType.GetGenericArguments().FirstOrDefault()?.BaseType == typeof(MondaySubRow))
            {
                foreach (PropertyInfo subItemProperty in property.PropertyType.GetGenericArguments().FirstOrDefault()?.GetProperties() ?? [])
                {
                    propertyInfos.Add(subItemProperty);
                }

                continue;
            }

            propertyInfos.Add(property);
        }

        // Loop Through All Properties In The Type.
        foreach (PropertyInfo property in propertyInfos)
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
    internal static object CreateColumnTypeInstance(MondayColumnType? columnType, ColumnValue column)
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
                return new ColumnNumber(column.Id,
                    !string.IsNullOrEmpty(column.Text) ? Convert.ToSingle(column.Text ?? "0") : null);

            case MondayColumnType.Date:
                return new ColumnDateTime(column.Id,
                    !string.IsNullOrEmpty(column.Text) ? Convert.ToDateTime(column.Text) : null);

            case MondayColumnType.Checkbox:
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

            case MondayColumnType.File:
                return new ColumnFile(column.Id, !string.IsNullOrEmpty(column.Text) ? column.Text : null);

            case MondayColumnType.Email:
                if (!string.IsNullOrEmpty(column.Text))
                {
                    string[] parts = column.Text.Split(" - ",
                        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    return new ColumnEmail(column.Id, parts.LastOrDefault(), parts.FirstOrDefault());
                }
                else
                {
                    return new ColumnEmail(column.Id, null, null);
                }

            case MondayColumnType.Phone:
                return new ColumnPhone(column.Id, !string.IsNullOrEmpty(column.Text) ? column.Text : null);

            case MondayColumnType.Rating:

                // If The Column Text Is Null Or Empty, Return A Column Rating With A None Rating.
                if (string.IsNullOrEmpty(column.Text))
                {
                    return new ColumnRating(column.Id, MondayRating.None);
                }

                // If The Column Text Is Not An Integer, Or The Parse Result Is Less Than 0 Or Greater Than 5, Throw An Exception.
                if (!int.TryParse(column.Text, out int parseResult) || parseResult < 0 || parseResult > 5)
                {
                    throw new ArgumentException("The rating must be an integer between 0 and 5!");
                }

                // Parse it to Rating enum
                MondayRating rating = (MondayRating)parseResult;

                // Return the Column Rating.
                return new ColumnRating(column.Id, rating);
            
            case MondayColumnType.People:
            {
                if (string.IsNullOrEmpty(column.Text))
                {
                    return new ColumnPeopleAndTeams(column.Id, []);
                }

                // Split and clean the display values
                string[] displayValues = column.Text
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim())
                    .ToArray();

                // Get the structured entities
                IEnumerable<PeopleEntity> entities = column.PeopleAndTeams;

                // Critical safety check: lengths must match
                if (displayValues.Length != entities.Count())
                {
                    throw new InvalidOperationException(
                        $"Mismatch between people display names ({displayValues.Length}) and entities ({entities.Count()}) " +
                        $"in column '{column.Id}'. Data may be corrupted or API changed.");
                }

                // Create the dictionary of people and teams
                Dictionary<ulong, PeopleAndTeamsEntry> peopleAndTeams = [];

                // Loop through the entities and create the dictionary
                for (int i = 0; i < displayValues.Length; i++)
                {
                    // Attempt to get the entity by index
                    PeopleEntity entity = entities.ElementAt(i);
                    
                    // Assign the display name to the entity
                    string displayName = displayValues[i];

                    // Attempt to parse the person ID
                    if (!ulong.TryParse(entity.Id, out ulong personId))
                    {
                        // This should rarely happen if API is consistent, but still guard
                        throw new ArgumentException($"Invalid person ID '{entity.Id}' at position {i}: '{displayName}'");
                    }

                    // Add the entity to the dictionary
                    peopleAndTeams[personId] = new PeopleAndTeamsEntry(entity.Kind, displayName);
                }

                // Return the column people and teams
                return new ColumnPeopleAndTeams(column.Id, peopleAndTeams);
            }
            
            default:
                throw new ArgumentException($"Unsupported column type: {columnType}");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="columnTypes"></param>
    /// <returns></returns>
    internal static string ToColumnValuesJson(this List<ColumnBaseType>? columnTypes)
    {
        if (columnTypes == null || columnTypes.Count == 0) return string.Empty;

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
            if (columnTypesSpan[i] == null) continue;
            
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

        // Add the closing bracket.
        jsonChars[currentIndex++] = '}';

        // Create a new string from the jsonChars span.
        string jsonString = new(jsonChars);

        // Remove any Json invalid characters.
        jsonString = jsonString.Replace("\r", string.Empty).Replace("\n", string.Empty);

        // If the json string is not valid, throw an exception.
        return !IsValidJson(jsonString) ? throw new JsonException("Invalid JSON format!") 
            : jsonString;
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

    internal static IEnumerable<string> BuildErrorMessages(
        IEnumerable<GraphQLError>? errors,
        IReadOnlyDictionary<string, object>? extensions)
    {
        if (errors is null)
            yield break;

        foreach (GraphQLError error in errors)
        {
            // Base message
            if (!string.IsNullOrWhiteSpace(error.Message)) yield return error.Message;

            // Try to extract Monday specific error details.
            if (error.Extensions is not null &&
                error.Extensions.TryGetValue("error_data", out object? errorDataObj) &&
                errorDataObj is IDictionary<string, object> errorData)
            {
                if (errorData.TryGetValue("column_id", out object? columnId))
                    yield return $"Invalid column ID: '{columnId}'";

                if (errorData.TryGetValue("error_reason", out object? reason))
                    yield return $"Reason: {reason}";
            }

            // Optional: request id (useful for support/debugging)
            if (extensions != null &&
                extensions.TryGetValue("request_id", out object? requestId))
            {
                yield return $"Request ID: {requestId}";
            }
        }
    }
}