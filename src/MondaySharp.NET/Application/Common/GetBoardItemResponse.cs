using MondaySharp.NET.Application.Entities;
using Newtonsoft.Json;

namespace MondaySharp.NET.Application.Common;

internal class GetBoardItemsByColumnValuesResponse
{
    [JsonProperty("items_page_by_column_values")]
    public ItemsPageByColumnValue? ItemsPageByColumnValue { get; set; }
}

internal class NextItemsPageResponse
{
    [JsonProperty("next_items_page")]
    public NextItemsPage? NextItemsPage { get; set; }
}

internal class NextItemsPage
{
    public string? Cursor { get; set; }
    public List<Item>? Items { get; set; }
    public bool ContainsMore => !string.IsNullOrEmpty(Cursor);
}

internal class GetBoardItemsResponse
{
    [JsonProperty("boards")]
    public List<Board>? Boards { get; set; }
}

internal class GetBoardsResponse : GetBoardItemsResponse
{
}

public class ItemsPageByColumnValue
{
    public string? Cursor { get; set; }
    public List<Item>? Items { get; set; }
}
