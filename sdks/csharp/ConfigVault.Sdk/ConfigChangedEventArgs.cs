using System;
using System.Collections.Generic;

namespace ConfigVault.Sdk;

public class ConfigChangedEventArgs : EventArgs
{
    public IReadOnlyList<string> Keys { get; init; } = Array.Empty<string>();
    public DateTimeOffset Timestamp { get; init; }
}
