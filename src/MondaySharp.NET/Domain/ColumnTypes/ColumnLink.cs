namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnLink : ColumnBaseType
{
    public ColumnLink() { }
    public Uri? Uri { get; set; }
    public string? Text { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="uri"></param>
    /// <param name="text"></param>
    public ColumnLink(string? id, Uri uri, string? text)
    {
        this.Id = id;
        this.Uri = uri;

        // If There Is No Message,
        // We Will Use The Uri As Default.
        this.Text = text ?? uri.OriginalString;
    }

    public ColumnLink(string? id)
    {
        this.Id = id;
        this.Uri = null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="uri"></param>
    /// <param name="text"></param>
    public ColumnLink(string? id, string? uri, string? text)
    {
        this.Id = id;

        if (Uri.TryCreate(uri, UriKind.Absolute, out Uri? result))
        {
            this.Uri = result;
        }

        // If There Is No Message,
        // We Will Use The Uri As Default.
        this.Text = text ?? uri;
    }

    public override string ToString()
    {
        return "\"" + this.Id + "\" : {\"url\" : \"" + this.Uri?.OriginalString + "\", \"text\":\"" + this.Text + "\"}";
    }
}
