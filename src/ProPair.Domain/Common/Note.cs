namespace ProPair.Domain.Common;

/// <summary>Free-text note ("Watch out for X") with who wrote it last.</summary>
public sealed record Note(string Text, string Author, DateTimeOffset UpdatedAt)
{
    public const int MaxLength = 2000;

    /// <summary>Returns null for empty text, which means "remove the note".</summary>
    public static Note? Create(string? text, string author, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(author)) throw new DomainException("Author is required.");
        var trimmed = text?.Trim() ?? "";
        if (trimmed.Length > MaxLength) throw new DomainException($"Note is longer than {MaxLength} characters.");
        return trimmed.Length == 0 ? null : new Note(trimmed, author.Trim(), now);
    }
}
