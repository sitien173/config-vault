using System.Text.RegularExpressions;
using System.Threading.Channels;
using ConfigVault.Api.Models;

namespace ConfigVault.Api.Services;

public sealed class SseClient : IDisposable
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string? FilterPattern { get; }
    public Channel<SseEvent> EventChannel { get; }

    private readonly Regex? _filterRegex;
    private bool _disposed;

    public SseClient(string? filterPattern)
    {
        FilterPattern = filterPattern;
        EventChannel = Channel.CreateBounded<SseEvent>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        if (!string.IsNullOrEmpty(filterPattern))
        {
            var regexPattern = "^" + Regex.Escape(filterPattern)
                .Replace("\\*\\*", ".*")
                .Replace("\\*", "[^/]*") + "$";

            _filterRegex = new Regex(regexPattern, RegexOptions.Compiled);
        }
    }

    public bool MatchesKey(string key)
    {
        if (_filterRegex is null)
        {
            return true;
        }

        return _filterRegex.IsMatch(key);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        EventChannel.Writer.TryComplete();
        _disposed = true;
    }
}
