namespace Veeme.Models;

public sealed class FirebirdDatabaseOptions
{
    public const string SectionName = "Firebird";

    public string? Server { get; init; }
    public int Port { get; init; } = 3050;
    public string? Database { get; init; }
    public string? User { get; init; }
    public string? Password { get; init; }
    public string? Charset { get; init; }
    public int Dialect { get; init; } = 3;
}
