namespace AIPractice.BuildingBlock;

public sealed record StandardSource(string Id, string Title, string Text, string Revision);

public sealed record SourceHit(StandardSource Source, double Score);
