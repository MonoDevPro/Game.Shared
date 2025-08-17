using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;

namespace Shared.Network.Transport;

/// <summary>
/// Responsável por receber e processar mensagens da rede no contexto do ECS
/// </summary>
public class NetworkReceiver : IDisposable
{
    private readonly NetPacketProcessor _processor;
    private readonly EventBasedNetListener _listener;
    private readonly ILogger<NetworkReceiver> _logger;

    /// <summary>
    /// Responsável por receber e processar mensagens da rede no contexto do ECS
    /// </summary>
    public NetworkReceiver(NetPacketProcessor processor, EventBasedNetListener listener, ILogger<NetworkReceiver> logger)
    {
        _processor = processor;
        _listener = listener;
        _logger = logger;
        
        // Registra o evento de recebimento de pacotes
        _listener.NetworkReceiveEvent += OnNetworkReceive;
    }

    private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            // Processa o pacote e enfileira a mensagem
            _processor.ReadAllPackets(reader, peer);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[NetworkReceiver] Erro ao processar pacote: {ex.Message}");
        }
    }
    
    public IDisposable RegisterMessageHandler<T>(Action<T, NetPeer> callback) where T : struct, INetSerializable
    {
        _processor.SubscribeNetSerializable(callback);
        return new DisposableAction(() =>
        {
            _processor.RemoveSubscription<T>();
            _logger.LogInformation($"[NetworkReceiver] Unregistered handler for {typeof(T).Name}");
        });
    }

    public void Dispose()
    {
        _listener.NetworkReceiveEvent -= OnNetworkReceive;
        _logger.LogInformation("[NetworkReceiver] Disposed and stopped listening for network events.");
    }
}