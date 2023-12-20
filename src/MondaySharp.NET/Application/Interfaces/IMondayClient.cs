using MondaySharp.NET.Application.Entities;
using MondaySharp.NET.Domain.Common;

namespace MondaySharp.NET.Application.Interfaces;

public interface IMondayClient
{
    public IAsyncEnumerable<T?> GetBoardItemsAsync<T>(ulong boardId, ColumnValue[] columnValues, int limit = 25, CancellationToken cancellationToken = default) where T : MondayRow, new();
    public IAsyncEnumerable<T?> GetBoardItemsAsync<T>(ulong boardId, int limit = 25, CancellationToken cancellationToken = default) where T : MondayRow, new();
    public Task<Dictionary<string, Item>?> CreateBoardItemsAsync(ulong boardId, Item[] items, CancellationToken cancellationToken = default);
    public Task<Dictionary<string, Update>?> CreateItemsUpdateAsync(Update[] updates, CancellationToken cancellationToken = default);
    public void Dispose();
}
