namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnEmail : ColumnBaseType
{
    public string? Email { get; set; }
    public string? Message { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="emailAddress"></param>
    /// <param name="message"></param>
    public ColumnEmail(string? id, string? emailAddress, string? message)
    {
        this.Id = id;
        this.Email = emailAddress;

        // If There Is No Message Defined,
        // We Will Add The Email Address There By Default.
        this.Message = message ?? emailAddress;
    }

    public ColumnEmail() { }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        if (string.IsNullOrEmpty(this.Email))
        {
            return "\"" + this.Id + "\" : null";
        }

        return "\"" + this.Id + "\":{\"email\":\"" + this.Email + "\",\"text\":\"" + (this.Message ?? this.Email) + "\"}";
    }
}
