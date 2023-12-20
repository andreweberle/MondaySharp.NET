namespace MondaySharp.NET.Domain.ColumnTypes;

public record ColumnFile : ColumnBaseType
{
    public Uri[]? PrivateFileUrls { get; set; }

    public ColumnFile()
    {
    }

    public ColumnFile(string? id, string? text)
    {
        this.Id = id;

        if (text is not null)
        {
            string[] privateFileUrls = text.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (privateFileUrls.Length > 0)
            {
                this.PrivateFileUrls = new Uri[privateFileUrls.Length];

                for (int i = 0; i < privateFileUrls.Length; i++)
                {
                    if (Uri.TryCreate(privateFileUrls[i], UriKind.Absolute, out Uri? uri))
                    {
                        this.PrivateFileUrls[i] = uri;
                    }
                }
            }
        }
    }
}
