using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace GameServer.Infrastructure.EfCore.Worker;

public sealed class BackgroundPersistence : IBackgroundPersistence, IDisposable
{
    private readonly Channel<SaveRequest> _saveRequests;
    private readonly Channel<LoginRequest> _loginRequests;
    private readonly Channel<AccountCreationRequestMsg> _accountCreationRequests;
    private readonly Channel<CharacterListRequestMsg> _characterListRequests;
    private readonly Channel<CharacterCreationRequestMsg> _characterCreationRequests;
    private readonly Channel<CharacterSelectionRequestMsg> _characterSelectionRequests;
    private readonly Channel<SaveResult> _saveResults;
    private readonly Channel<LoginResult> _loginResults;
    private readonly Channel<AccountCreationResult> _accountCreationResults;
    private readonly Channel<CharacterListResult> _characterListResults;
    private readonly Channel<CharacterCreationResult> _characterCreationResults;
    private readonly Channel<CharacterSelectionResult> _characterSelectionResults;
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
        _accountCreationResults = Channel.CreateUnbounded<AccountCreationResult>();
        _characterListResults = Channel.CreateUnbounded<CharacterListResult>();
        _characterCreationResults = Channel.CreateUnbounded<CharacterCreationResult>();
        _characterSelectionResults = Channel.CreateUnbounded<CharacterSelectionResult>();

        var smallBounded = new BoundedChannelOptions(capacity: 2000)
        {
            SingleReader = false,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        };
        _accountCreationRequests = Channel.CreateBounded<AccountCreationRequestMsg>(smallBounded);
        _characterListRequests = Channel.CreateBounded<CharacterListRequestMsg>(smallBounded);
        _characterCreationRequests = Channel.CreateBounded<CharacterCreationRequestMsg>(smallBounded);
        _characterSelectionRequests = Channel.CreateBounded<CharacterSelectionRequestMsg>(smallBounded);
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

    public async ValueTask<bool> EnqueueAccountCreationAsync(AccountCreationRequestMsg req, CancellationToken ct = default)
    {
        if (await _accountCreationRequests.Writer.WaitToWriteAsync(ct).ConfigureAwait(false))
        {
            await _accountCreationRequests.Writer.WriteAsync(req, ct).ConfigureAwait(false);
            return true;
        }
        return false;
    }

    public async ValueTask<bool> EnqueueCharacterListAsync(CharacterListRequestMsg req, CancellationToken ct = default)
    {
        if (await _characterListRequests.Writer.WaitToWriteAsync(ct).ConfigureAwait(false))
        {
            await _characterListRequests.Writer.WriteAsync(req, ct).ConfigureAwait(false);
            return true;
        }
        return false;
    }

    public async ValueTask<bool> EnqueueCharacterCreationAsync(CharacterCreationRequestMsg req, CancellationToken ct = default)
    {
        if (await _characterCreationRequests.Writer.WaitToWriteAsync(ct).ConfigureAwait(false))
        {
            await _characterCreationRequests.Writer.WriteAsync(req, ct).ConfigureAwait(false);
            return true;
        }
        return false;
    }

    public async ValueTask<bool> EnqueueCharacterSelectionAsync(CharacterSelectionRequestMsg req, CancellationToken ct = default)
    {
        if (await _characterSelectionRequests.Writer.WaitToWriteAsync(ct).ConfigureAwait(false))
        {
            await _characterSelectionRequests.Writer.WriteAsync(req, ct).ConfigureAwait(false);
            return true;
        }
        return false;
    }

    // Readers expostos para consumers (systems/worker)
    public ChannelReader<SaveResult> SaveResults => _saveResults.Reader;
    public ChannelReader<LoginResult> LoginResults => _loginResults.Reader;
    public ChannelReader<AccountCreationResult> AccountCreationResults => _accountCreationResults.Reader;
    public ChannelReader<CharacterListResult> CharacterListResults => _characterListResults.Reader;
    public ChannelReader<CharacterCreationResult> CharacterCreationResults => _characterCreationResults.Reader;
    public ChannelReader<CharacterSelectionResult> CharacterSelectionResults => _characterSelectionResults.Reader;

    public ChannelReader<SaveRequest> SaveRequestsReader => _saveRequests.Reader;
    public ChannelReader<LoginRequest> LoginRequestsReader => _loginRequests.Reader;
    public ChannelReader<AccountCreationRequestMsg> AccountCreationRequestsReader => _accountCreationRequests.Reader;
    public ChannelReader<CharacterListRequestMsg> CharacterListRequestsReader => _characterListRequests.Reader;
    public ChannelReader<CharacterCreationRequestMsg> CharacterCreationRequestsReader => _characterCreationRequests.Reader;
    public ChannelReader<CharacterSelectionRequestMsg> CharacterSelectionRequestsReader => _characterSelectionRequests.Reader;

    // Worker chama estes métodos para publicar resultados
    public ValueTask PublishSaveResultAsync(SaveResult result, CancellationToken ct = default)
        => _saveResults.Writer.WriteAsync(result, ct);

    public ValueTask PublishLoginResultAsync(LoginResult result, CancellationToken ct = default)
        => _loginResults.Writer.WriteAsync(result, ct);

    public ValueTask PublishAccountCreationResultAsync(AccountCreationResult result, CancellationToken ct = default)
        => _accountCreationResults.Writer.WriteAsync(result, ct);

    public ValueTask PublishCharacterListResultAsync(CharacterListResult result, CancellationToken ct = default)
        => _characterListResults.Writer.WriteAsync(result, ct);

    public ValueTask PublishCharacterCreationResultAsync(CharacterCreationResult result, CancellationToken ct = default)
        => _characterCreationResults.Writer.WriteAsync(result, ct);

    public ValueTask PublishCharacterSelectionResultAsync(CharacterSelectionResult result, CancellationToken ct = default)
        => _characterSelectionResults.Writer.WriteAsync(result, ct);

    // Dispose apenas marca os channels como completos
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _saveRequests.Writer.TryComplete();
        _loginRequests.Writer.TryComplete();
        _saveResults.Writer.TryComplete();
        _loginResults.Writer.TryComplete();
        _accountCreationResults.Writer.TryComplete();
        _characterListResults.Writer.TryComplete();
        _characterCreationResults.Writer.TryComplete();
        _characterSelectionResults.Writer.TryComplete();
    }
}