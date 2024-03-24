﻿using MondaySharp.NET.Application.Entities;
using MondaySharp.NET.Domain.Common;

namespace MondaySharp.NET.Application.Interfaces;

public interface IMondayClient
{
    public Task<MondayResponse<T>> GetBoardItemsAsync<T>(ulong boardId, ColumnValue[] columnValues, int limit = 25, CancellationToken cancellationToken = default) where T : MondayRow, new();
    public Task<MondayResponse<T>> GetBoardItemsAsync<T>(ulong boardId, int limit = 25, CancellationToken cancellationToken = default) where T : MondayRow, new();
    public Task<MondayResponse<T>> GetBoardItemAsync<T>(ulong itemId, CancellationToken cancellationToken = default) where T : MondayRow, new();
    public Task<MondayResponse<Item>> CreateBoardItemsAsync(ulong boardId, Item[] items, CancellationToken cancellationToken = default);
    public Task<Application.MondayResponse<T>> CreateBoardItemsAsync<T>(ulong boardId, T[] items, CancellationToken cancellationToken = default) where T : MondayRow, new();
    public Task<MondayResponse<Update>> CreateItemsUpdateAsync(Update[] updates, CancellationToken cancellationToken = default);
    public Task<MondayResponse<Item>> DeleteItemsAsync(Item[] items, CancellationToken cancellationToken = default);
    public Task<Application.MondayResponse<Board>> GetBoardsAsync(ulong[]? boardIds = null, int limit = 10, CancellationToken cancellationToken = default);
    public Task<Application.MondayResponse<Asset>> UploadFileToUpdateAsync(Update[] updates, CancellationToken cancellationToken = default);
    public Task<Application.MondayResponse<Asset>> UploadFileToColumnAsync(Item[] items, CancellationToken cancellationToken = default);
    public Task<Application.MondayResponse<T>> GetBoardItemsAsync<T>(string? cursor, int limit = 25, CancellationToken cancellationToken = default) where T : MondayRow, new();
    public Task<Application.MondayResponse<T>> UpdateBoardItemsAsync<T>(ulong boardId, T[] items, CancellationToken cancellationToken = default) where T : MondayRow, new();
    public void Dispose();
}
