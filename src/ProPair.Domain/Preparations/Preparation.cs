using System.Security.Cryptography;
using ProPair.Domain.Common;
using ProPair.Domain.Ratings;

namespace ProPair.Domain.Preparations;

/// <summary>Secret token for link-based access without login.</summary>
public readonly record struct ShareToken(string Value)
{
    public static ShareToken New() => new(Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16)));
    public override string ToString() => Value;
}

public enum AccessLevel { None, Read, Edit }

/// <summary>Our team's preparation for one tournament. The rating scale is copied from the team and fixed.</summary>
public sealed class Preparation : AggregateRoot<PreparationId>
{
    private readonly Dictionary<PlayerId, Note> _playerNotes;
    private readonly Dictionary<ListLineKey, ListMark> _listMarks;

    public TeamId TeamId { get; }
    public TournamentId TournamentId { get; }
    public ParticipantId OwnParticipantId { get; }
    public RatingScale RatingScale { get; }
    public ShareToken EditToken { get; }
    public ShareToken ReadToken { get; }

    /// <summary>Notes on players' lists (own and opponents), shared by all matrices of this tournament.</summary>
    public IReadOnlyDictionary<PlayerId, Note> PlayerNotes => _playerNotes;

    /// <summary>Coloured marks on single lines of players' lists.</summary>
    public IReadOnlyDictionary<ListLineKey, ListMark> ListMarks => _listMarks;

    public Preparation(PreparationId id, TeamId teamId, TournamentId tournamentId, ParticipantId ownParticipantId,
        RatingScale ratingScale, ShareToken editToken, ShareToken readToken,
        IDictionary<PlayerId, Note>? playerNotes = null, IDictionary<ListLineKey, ListMark>? listMarks = null) : base(id)
    {
        _playerNotes = new Dictionary<PlayerId, Note>(playerNotes ?? new Dictionary<PlayerId, Note>());
        _listMarks = new Dictionary<ListLineKey, ListMark>(listMarks ?? new Dictionary<ListLineKey, ListMark>());
        TeamId = teamId;
        TournamentId = tournamentId;
        OwnParticipantId = ownParticipantId;
        RatingScale = ratingScale;
        EditToken = editToken;
        ReadToken = readToken;
    }

    public static Preparation Start(Teams.Team team, Tournaments.Tournament tournament, ParticipantId ownParticipantId)
    {
        tournament.GetParticipant(ownParticipantId); // must exist
        return new Preparation(PreparationId.New(), team.Id, tournament.Id, ownParticipantId,
            team.DefaultRatingScale, ShareToken.New(), ShareToken.New());
    }

    /// <summary>Sets the note on a player's list; empty text removes it. The caller checks the player is in the tournament.</summary>
    public void SetPlayerNote(PlayerId playerId, string? text, string author, DateTimeOffset now)
    {
        var note = Note.Create(text, author, now);
        if (note is null) _playerNotes.Remove(playerId);
        else if (_playerNotes.GetValueOrDefault(playerId)?.Text != note.Text) _playerNotes[playerId] = note;
    }

    /// <summary>
    /// Marks a line of a player's list (colour + optional short note), or removes the mark with color null.
    /// <paramref name="listText"/> is the player's current list; the line must exist and match <paramref name="lineText"/>.
    /// </summary>
    public void MarkListLine(PlayerId playerId, string listText, int lineIndex, string lineText, MarkColor? color,
        string? note, string author, DateTimeOffset now)
    {
        var lines = listText.Split('\n');
        if (lineIndex < 0 || lineIndex >= lines.Length || lines[lineIndex].Trim() != lineText.Trim())
            throw new DomainException("List line not found – the list may have changed.");
        if (string.IsNullOrWhiteSpace(author)) throw new DomainException("Author is required.");

        var key = new ListLineKey(playerId, lineIndex);
        if (color is null)
        {
            _listMarks.Remove(key);
            return;
        }

        var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmedNote?.Length > ListMark.MaxNoteLength)
            throw new DomainException($"Mark note is longer than {ListMark.MaxNoteLength} characters.");
        _listMarks[key] = new ListMark(lineIndex, lineText.Trim(), color.Value, trimmedNote, author.Trim(), now);
    }

    /// <summary>Marks for a player's current list. Lines that moved after a re-import are found again by text.</summary>
    public IReadOnlyList<ListMark> MarksFor(PlayerId playerId, string listText)
    {
        var lines = listText.Split('\n').Select(l => l.Trim()).ToArray();
        var result = new List<ListMark>();
        var taken = new HashSet<int>();
        foreach (var mark in _listMarks.Where(m => m.Key.PlayerId == playerId).Select(m => m.Value).OrderBy(m => m.LineIndex))
        {
            var index = mark.LineIndex < lines.Length && lines[mark.LineIndex] == mark.LineText && !taken.Contains(mark.LineIndex)
                ? mark.LineIndex
                : Enumerable.Range(0, lines.Length).FirstOrDefault(i => lines[i] == mark.LineText && !taken.Contains(i), -1);
            if (index < 0) continue; // line no longer in the list
            taken.Add(index);
            result.Add(mark with { LineIndex = index });
        }
        return result;
    }

    public AccessLevel AccessFor(string token) =>
        token == EditToken.Value ? AccessLevel.Edit
        : token == ReadToken.Value ? AccessLevel.Read
        : AccessLevel.None;
}
