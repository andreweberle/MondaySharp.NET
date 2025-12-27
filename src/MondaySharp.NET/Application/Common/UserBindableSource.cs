using MondaySharp.NET.Application.Entities;
using MondaySharp.NET.Application.Interfaces;
using MondaySharp.NET.Domain.Common;
using MondaySharp.NET.Domain.Common.Enums;

namespace MondaySharp.NET.Application.Common;

public sealed class UserBindableSource(MondayUser user) : IMondayBindableSource
{
    public ulong Id { get; } = user.Id;
    public string? Name { get; } = user.Name;
    public MondayState? State { get; } = null;
    public Board? Board { get; } = null;
    public Group? Group { get; } = null;
    public IReadOnlyList<ColumnValue> ColumnValues { get; } = [];
    public IReadOnlyList<Asset> Assets { get; } 
    public IReadOnlyList<Update> Updates { get; }
    public FileUpload? FileUpload { get; }
}