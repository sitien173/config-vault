namespace ConfigVault.Core;

public class ConfigurationChangedEventArgs : EventArgs
{
    public IReadOnlyList<string> ChangedKeys { get; init; } = Array.Empty<string>();

    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;
}
