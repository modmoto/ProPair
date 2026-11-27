using ProPair.Domain.Common;

namespace ProPair.Domain.Preparations;

/// <summary>Highlight colours for list lines. No fixed meaning – the team decides.</summary>
public enum MarkColor { Red, Orange, Yellow, Green, Blue, Pink }

/// <summary>A marked line of a player's army list, e.g. "State Militia" in red with "kann Flammenwerfer".</summary>
public sealed record ListMark(int LineIndex, string LineText, MarkColor Color, string? Note, string Author, DateTimeOffset UpdatedAt)
{
    public const int MaxNoteLength = 200;
}

public readonly record struct ListLineKey(PlayerId PlayerId, int LineIndex);
