namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnStatus :ColumnBaseType
{
    public string? Status { get; set; }
    public int? StatusId { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="status"></param>
    public ColumnStatus(string? id, string? status)
    {
        this.Id = id;
        this.Status = status;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="statusId"></param>
    public ColumnStatus(string? id, int? statusId)
    {
        this.Id = id;
        this.StatusId = statusId;
    }

    public ColumnStatus()
    {
    }

    public override string ToString() => string.IsNullOrEmpty(Status)
            ? "\"" + this.Id + "\" : {\"index\" : \"" + this.StatusId + "\"}"
            : "\"" + this.Id + "\" : {\"label\" : \"" + this.Status + "\"}";
}
