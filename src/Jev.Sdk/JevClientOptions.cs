namespace Jev.Sdk;

public sealed class JevClientOptions
{
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
    public Uri? BaseUrl { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);
    public int MaxRetries { get; set; } = 2;
}
