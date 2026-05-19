    using MondaySharp.NET.Application.Attributes;

    namespace MondaySharp.NET.Domain.ColumnTypes;

    [MondayColumnTypeUnsupportedWrite]
    public record ColumnLastUpdated : ColumnBaseType
    {
        public DateTimeOffset? UpdatedAt { get; set; }
        public ulong? UpdaterId { get; set; }

        public ColumnLastUpdated(string? id)
        {
            Id = id;
        }

        public ColumnLastUpdated(string? id, DateTimeOffset? updatedAt, ulong? updaterId)
        {
            Id = id;
            UpdatedAt = updatedAt;
            UpdaterId = updaterId;
        }

        public ColumnLastUpdated()
        {
        }

        public override string ToString()
        {
            throw new NotSupportedException(nameof(ColumnLastUpdated));
        }
    }