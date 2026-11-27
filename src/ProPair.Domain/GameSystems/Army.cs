namespace ProPair.Domain.GameSystems;

/// <summary>Army of a game system, identified by a stable slug (e.g. "sylvan-elves").</summary>
public sealed record Army(string Id, string Name, string Short);
