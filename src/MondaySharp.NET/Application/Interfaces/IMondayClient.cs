using MondaySharp.NET.Application.Entities;
using MondaySharp.NET.Domain.Common;

namespace MondaySharp.NET.Application.Interfaces;

public interface IMondayClient
{
    public IAsyncEnumerable<MondayResponse<T?>> GetBoardItemsAsEnumerableAsync<T>(ulong boardId, ColumnValue[] columnValues, int limit = 25, CancellationToken cancellationToken = default) where T : MondayRow, new();
    public IAsyncEnumerable<MondayResponse<T?>> GetBoardItemsAsEnumerableAsync<T>(ulong boardId, int limit = 25, CancellationToken cancellationToken = default) where T : MondayRow, new();
    public Task<MondayResponse<Dictionary<string, Item>?>> CreateBoardItemsAsync(ulong boardId, Item[] items, CancellationToken cancellationToken = default);
    public Task<MondayResponse<Dictionary<string, Update>?>> CreateItemsUpdateAsync(Update[] updates, CancellationToken cancellationToken = default);
    public Task<MondayResponse<Dictionary<string, Item>?>> DeleteItemsAsync(Item[] items, CancellationToken cancellationToken = default);
    public Task<Application.MondayResponse<List<Board>>> GetBoardsAsync(ulong[]? boardIds = null, int limit = 10, CancellationToken cancellationToken = default);
    public Task<Application.MondayResponse<Dictionary<string, Asset>>> UploadFileToUpdateAsync(Update[] updates, CancellationToken cancellationToken = default);
    public Task<Application.MondayResponse<Dictionary<string, Asset>>> UploadFileToColumnAsync(Item[] items, CancellationToken cancellationToken = default);
    public void Dispose();
}
