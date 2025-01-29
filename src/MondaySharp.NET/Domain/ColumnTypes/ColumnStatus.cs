namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnStatus : ColumnBaseType
{
    public string? Status { get; set; }
    public int? StatusId { get; set; }

    public ColumnStatus(string? id)
    {
        Id = id;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="status"></param>
    public ColumnStatus(string? id, string? status)
    {
        Id = id;
        Status = status;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="statusId"></param>
    public ColumnStatus(string? id, int? statusId)
    {
        Id = id;
        StatusId = statusId;
    }

    public ColumnStatus()
    {
    }

    public override string ToString()
    {
        if (Status == null && StatusId == null)
        {
            return "\"" + Id + "\" : null";
        }

        if (string.IsNullOrEmpty(Status))
        {
            return "\"" + Id + "\" : {\"index\" : \"" + StatusId + "\"}";
        }
        else
        {
            return "\"" + Id + "\" : {\"label\" : \"" + Status + "\"}";
        }
    }
}