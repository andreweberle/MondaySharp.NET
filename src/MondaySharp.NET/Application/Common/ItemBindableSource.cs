using MondaySharp.NET.Application.Entities;
using MondaySharp.NET.Application.Interfaces;
using MondaySharp.NET.Domain.Common.Enums;

namespace MondaySharp.NET.Application.Common;

public sealed class ItemBindableSource(Item item) : IMondayBindableSource
{
    public ulong Id { get; } = item.Id;
    public string? Name { get; } = item.Name;
    public MondayState? State { get; } = item.State;
    public Board? Board { get; } = item.Board;
    public Group? Group { get; } = item.Group;
    public IReadOnlyList<ColumnValue> ColumnValues { get; } = item.ColumnValues;
    public IReadOnlyList<Asset> Assets { get; } = item.Assets;
    public IReadOnlyList<Update> Updates { get; } = item.Updates;
    public FileUpload? FileUpload { get; } = item.FileUpload;
}