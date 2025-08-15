using System.Threading.Channels;

namespace GameServer.Infrastructure.EfCore.Worker;

public interface IBackgroundPersistence
{
    // Producers (Systems) chamam estes métodos para enfileirar.
    ValueTask<bool> EnqueueSaveAsync(SaveRequest req, CancellationToken ct = default);
    ValueTask<bool> EnqueueLoginAsync(LoginRequest req, CancellationToken ct = default);

    // Systems leem resultados destes readers
    ChannelReader<SaveResult> SaveResults { get; }
    ChannelReader<LoginResult> LoginResults { get; }

    // DatabaseWorker (consumer) lê requests a partir destes readers
    ChannelReader<SaveRequest> SaveRequestsReader { get; }
    ChannelReader<LoginRequest> LoginRequestsReader { get; }
    
    // adicionadas para o worker publicar resultados de volta
    ValueTask PublishSaveResultAsync(SaveResult result, CancellationToken ct = default);
    ValueTask PublishLoginResultAsync(LoginResult result, CancellationToken ct = default);
}