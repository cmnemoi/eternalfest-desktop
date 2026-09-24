namespace EternalfestDesktop.Domain;

public readonly record struct GameId(Guid Value)
{
    public static GameId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}

public readonly record struct BlobId(Guid Value)
{
    public static BlobId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}

public readonly record struct RunId(Guid Value)
{
    public static RunId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
