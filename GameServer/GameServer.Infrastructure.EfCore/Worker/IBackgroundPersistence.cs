using System.Threading.Channels;

namespace GameServer.Infrastructure.EfCore.Worker;

public interface IBackgroundPersistence
{
    // Producers (Systems) chamam estes métodos para enfileirar.
    ValueTask<bool> EnqueueSaveAsync(SaveRequest req, CancellationToken ct = default);
    ValueTask<bool> EnqueueLoginAsync(LoginRequest req, CancellationToken ct = default);
    ValueTask<bool> EnqueueAccountCreationAsync(AccountCreationRequestMsg req, CancellationToken ct = default);
    ValueTask<bool> EnqueueCharacterListAsync(CharacterListRequestMsg req, CancellationToken ct = default);
    ValueTask<bool> EnqueueCharacterCreationAsync(CharacterCreationRequestMsg req, CancellationToken ct = default);
    ValueTask<bool> EnqueueCharacterSelectionAsync(CharacterSelectionRequestMsg req, CancellationToken ct = default);

    // Systems leem resultados destes readers
    ChannelReader<SaveResult> SaveResults { get; }
    ChannelReader<LoginResult> LoginResults { get; }
    ChannelReader<AccountCreationResult> AccountCreationResults { get; }
    ChannelReader<CharacterListResult> CharacterListResults { get; }
    ChannelReader<CharacterCreationResult> CharacterCreationResults { get; }
    ChannelReader<CharacterSelectionResult> CharacterSelectionResults { get; }

    // DatabaseWorker (consumer) lê requests a partir destes readers
    ChannelReader<SaveRequest> SaveRequestsReader { get; }
    ChannelReader<LoginRequest> LoginRequestsReader { get; }
    ChannelReader<AccountCreationRequestMsg> AccountCreationRequestsReader { get; }
    ChannelReader<CharacterListRequestMsg> CharacterListRequestsReader { get; }
    ChannelReader<CharacterCreationRequestMsg> CharacterCreationRequestsReader { get; }
    ChannelReader<CharacterSelectionRequestMsg> CharacterSelectionRequestsReader { get; }

    // adicionadas para o worker publicar resultados de volta
    ValueTask PublishSaveResultAsync(SaveResult result, CancellationToken ct = default);
    ValueTask PublishLoginResultAsync(LoginResult result, CancellationToken ct = default);
    ValueTask PublishAccountCreationResultAsync(AccountCreationResult result, CancellationToken ct = default);
    ValueTask PublishCharacterListResultAsync(CharacterListResult result, CancellationToken ct = default);
    ValueTask PublishCharacterCreationResultAsync(CharacterCreationResult result, CancellationToken ct = default);
    ValueTask PublishCharacterSelectionResultAsync(CharacterSelectionResult result, CancellationToken ct = default);
}