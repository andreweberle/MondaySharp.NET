using System.Text.Json;
using MondaySharp.NET.Domain.Common.Enums;

namespace MondaySharp.NET.Domain.ColumnTypes;

public sealed record PeopleAndTeamsEntry(MondayPeopleEntityType Kind, string? Text = null );

public record ColumnPeopleAndTeams : ColumnBaseType
{
    public Dictionary<ulong, PeopleAndTeamsEntry> PeopleAndTeams { get; init; } = new();

    public ColumnPeopleAndTeams(string? id = null, Dictionary<ulong, PeopleAndTeamsEntry>? peopleAndTeams = null)
    {
        Id = id;
        if (peopleAndTeams is not null)
            PeopleAndTeams = peopleAndTeams;
    }

    public ColumnPeopleAndTeams()
    {
    }

    public ColumnPeopleAndTeams(string? id)
    {
        Id = id;
    }
    
    public override string ToString()
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new InvalidOperationException($"{nameof(ColumnPeopleAndTeams)} requires {nameof(Id)} to be set.");

        if (PeopleAndTeams.Count == 0) return $"\"{Id}\": null";

        var payload = new
        {
            personsAndTeams = PeopleAndTeams.Select(kv => new
            {
                id = kv.Key,
                kind = kv.Value.Kind.ToString().ToLowerInvariant()
            })
        };

        return $"\"{Id}\": {JsonSerializer.Serialize(payload)}";
    }
}

