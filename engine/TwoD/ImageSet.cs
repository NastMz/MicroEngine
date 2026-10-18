using MicroEngine.Loop;
namespace MicroEngine.TwoD;

/// <summary>One-provider lazy image ownership. Failed cleanup remains retryable.</summary>
public sealed class ImageSet : IDisposable
{
    private readonly Dictionary<string, PixelImage> _sources = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IImageResource> _cache = new(StringComparer.Ordinal);
    private readonly List<string> _order = [];
    private IImageCanvas? _provider;
    private bool _closing;
    public ImageSet(IReadOnlyDictionary<string, PixelImage> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        foreach (var pair in sources)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key); ArgumentNullException.ThrowIfNull(pair.Value);
            _sources.Add(pair.Key, pair.Value);
        }
    }
    public IImageResource Get(IImageCanvas canvas, string key)
    {
        ObjectDisposedException.ThrowIf(_closing, this);
        ArgumentNullException.ThrowIfNull(canvas); ArgumentNullException.ThrowIfNull(key);
        if (!_sources.TryGetValue(key, out var source)) throw new KeyNotFoundException(key);
        if (_provider is not null && !ReferenceEquals(_provider, canvas)) throw new InvalidOperationException("ImageSet belongs to its first successful provider.");
        if (_cache.TryGetValue(key, out var existing)) return existing;
        var image = canvas.CreateImage(source) ?? throw new InvalidOperationException("Provider returned no image.");
        try { _cache.Add(key, image); _order.Add(key); }
        catch { _cache.Remove(key); image.Dispose(); throw; }
        _provider = canvas;
        return image;
    }
    public void Dispose()
    {
        _closing = true;
        List<Exception> errors = [];
        for (int i = _order.Count - 1; i >= 0; i--)
        {
            var key = _order[i];
            try { _cache[key].Dispose(); _cache.Remove(key); _order.RemoveAt(i); }
            catch (Exception error) { errors.Add(error); }
        }
        if (errors.Count > 0) throw new AggregateException("Some image leases remain pending cleanup; Dispose may be retried.", errors);
    }
}
