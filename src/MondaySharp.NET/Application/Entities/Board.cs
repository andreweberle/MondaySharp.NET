using MondaySharp.NET.Application.Common;
using MondaySharp.NET.Application.JsonConverters;
using MondaySharp.NET.Domain.Common.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MondaySharp.NET.Application.Entities;

public record Board
{
    [JsonProperty("id")]
    [JsonConverter(typeof(StringToULongJsonConverter))]
    public ulong Id { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("state")]
    [JsonConverter(typeof(StringEnumConverter))]
    public MondayState? State { get; set; }

    [JsonProperty("board_kind")]
    [JsonConverter(typeof(StringEnumConverter))]
    public MondayBoardKind? BoardKind { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("permissions")]
    [JsonConverter(typeof(StringEnumConverter))]
    public MondayPermission? Permissions { get; set; }

    [JsonProperty("workspace_id")]
    [JsonConverter(typeof(StringToULongJsonConverter))]
    public ulong WorkspaceId { get; set; }

    [JsonProperty("board_folder_id")]
    [JsonConverter(typeof(StringToULongJsonConverter))]
    public ulong BoardFolderId { get; set; }

    [JsonProperty("item_terminology")]
    public string? ItemTerminology { get; set; }

    [JsonProperty("items_count")]
    public int ItemsCount { get; set; }

    [JsonProperty("items_page")]
    public ItemsPageByColumnValue? ItemsPage { get; set; }
}
