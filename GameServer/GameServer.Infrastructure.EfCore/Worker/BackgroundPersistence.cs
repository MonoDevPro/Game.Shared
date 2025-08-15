using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace GameServer.Infrastructure.EfCore.Worker;

public sealed class BackgroundPersistence : IBackgroundPersistence, IDisposable
{
    private readonly Channel<SaveRequest> _saveRequests;
    private readonly Channel<LoginRequest> _loginRequests;
    private readonly Channel<SaveResult> _saveResults;
    private readonly Channel<LoginResult> _loginResults;
    private readonly ILogger<BackgroundPersistence> _logger;
    private bool _disposed;

    public BackgroundPersistence(ILogger<BackgroundPersistence> logger)
    {
        _logger = logger;

        var savesOpts = new BoundedChannelOptions(capacity: 5000)
        {
            SingleReader = false,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        };
        _saveRequests = Channel.CreateBounded<SaveRequest>(savesOpts);

        var loginOpts = new BoundedChannelOptions(capacity: 1000)
        {
            SingleReader = false,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        };
        _loginRequests = Channel.CreateBounded<LoginRequest>(loginOpts);

        _saveResults = Channel.CreateUnbounded<SaveResult>();
        _loginResults = Channel.CreateUnbounded<LoginResult>();
    }

    // Producers -> Writers
    public async ValueTask<bool> EnqueueSaveAsync(SaveRequest req, CancellationToken ct = default)
    {
        // aguarda até que seja possível escrever (respeita bounded capacity)
        if (await _saveRequests.Writer.WaitToWriteAsync(ct).ConfigureAwait(false))
        {
            await _saveRequests.Writer.WriteAsync(req, ct).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    public async ValueTask<bool> EnqueueLoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        if (await _loginRequests.Writer.WaitToWriteAsync(ct).ConfigureAwait(false))
        {
            await _loginRequests.Writer.WriteAsync(req, ct).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    // Readers expostos para consumers (systems/worker)
    public ChannelReader<SaveResult> SaveResults => _saveResults.Reader;
    public ChannelReader<LoginResult> LoginResults => _loginResults.Reader;

    public ChannelReader<SaveRequest> SaveRequestsReader => _saveRequests.Reader;
    public ChannelReader<LoginRequest> LoginRequestsReader => _loginRequests.Reader;

    // Worker chama estes métodos para publicar resultados
    public ValueTask PublishSaveResultAsync(SaveResult result, CancellationToken ct = default)
        => _saveResults.Writer.WriteAsync(result, ct);

    public ValueTask PublishLoginResultAsync(LoginResult result, CancellationToken ct = default)
        => _loginResults.Writer.WriteAsync(result, ct);

    // Dispose apenas marca os channels como completos
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _saveRequests.Writer.TryComplete();
        _loginRequests.Writer.TryComplete();
        _saveResults.Writer.TryComplete();
        _loginResults.Writer.TryComplete();
    }
}