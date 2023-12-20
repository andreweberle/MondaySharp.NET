using MondaySharp.NET.Application.Entities;
using Newtonsoft.Json;

namespace MondaySharp.NET.Application.Common;

internal class GetBoardItemsByColumnValuesResponse
{
    [JsonProperty("items_page_by_column_values")]
    public ItemsPageByColumnValue? ItemsPageByColumnValue { get; set; }
}

internal class GetBoardItemsResponse
{
    [JsonProperty("boards")]
    public List<Board>? Boards { get; set; }
}

public class ItemsPageByColumnValue
{
    public string? Cursor { get; set; }
    public List<Item>? Items { get; set; }
}
