namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnLink : ColumnBaseType
{
    public ColumnLink()
    {
    }

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
        Id = id;
        Uri = uri;

        // If There Is No Message,
        // We Will Use The Uri As Default.
        Text = text ?? uri.OriginalString;
    }

    public ColumnLink(string? id)
    {
        Id = id;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="uri"></param>
    /// <param name="text"></param>
    public ColumnLink(string? id, string? uri, string? text)
    {
        Id = id;

        if (Uri.TryCreate(uri, UriKind.Absolute, out Uri? result))
        {
            Uri = result;
        }

        // If There Is No Message,
        // We Will Use The Uri As Default.
        Text = text ?? uri;
    }

    public override string ToString()
    {
        if (Uri == null)
        {
            return "\"" + Id + "\" : null";
        }

        return "\"" + Id + "\" : {\"url\" : \"" + Uri?.OriginalString + "\", \"text\":\"" + Text + "\"}";
    }
}