using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;

namespace Shared.Core.Network.Transport;

public class NetworkSender(NetManager netManager, NetPacketProcessor packetProcessor, ILogger<NetworkSender> logger) : IDisposable
{
    // --- BUFFERS DE BROADCAST (JÁ EXISTENTES) ---
    private readonly NetDataWriter _reliableBroadcastWriter = new();
    private readonly NetDataWriter _unreliableBroadcastWriter = new();

    // --- BUFFERS DE UNICAST (NOVOS) ---
    private readonly Dictionary<int, NetDataWriter> _reliablePeerWriters = new();
    private readonly Dictionary<int, NetDataWriter> _unreliablePeerWriters = new();
    
    // --- PISCINA DE OBJECTOS PARA EVITAR GC ---
    private readonly Stack<NetDataWriter> _writerPool = new();
    
    /// <summary>
    /// Obtém um NetDataWriter da piscina ou cria um novo se a piscina estiver vazia.
    /// </summary>
    private NetDataWriter GetWriter()
    {
        if (_writerPool.TryPop(out var writer))
        {
            return writer;
        }
        return new NetDataWriter();
    }
    
    /// <summary>
    /// Limpa um NetDataWriter e devolve-o à piscina para ser reutilizado.
    /// </summary>
    private void ReturnWriter(NetDataWriter writer)
    {
        writer.Reset();
        _writerPool.Push(writer);
    }
    
    // --- MÉTODOS COM BATCHING PARA BROADCAST ---

    /// <summary>
    /// Enfileira um pacote para ser enviado a todos de forma CONFIÁVEL (reliable).
    /// Use para dados críticos que não podem ser perdidos (eventos, comandos, etc.).
    /// </summary>
    public void EnqueueReliableBroadcast<T>(ref T packet) 
        where T : struct, INetSerializable
    {
        packetProcessor.WriteNetSerializable(_reliableBroadcastWriter, ref packet);
    }

    /// <summary>
    /// Enfileira um pacote para ser enviado a todos de forma NÃO CONFIÁVEL (unreliable).
    /// Use para dados de alta frequência que podem ser perdidos (ex: posições contínuas).
    /// </summary>
    public void EnqueueUnreliableBroadcast<T>(ref T packet) 
        where T : struct, INetSerializable
    {
        packetProcessor.WriteNetSerializable(_unreliableBroadcastWriter, ref packet);
    }
    
    /// <summary>
    /// Enfileira um pacote para ser enviado a todos os peers, EXCETO um específico.
    /// A entrega é CONFIÁVEL (reliable).
    /// </summary>
    public void EnqueueReliableBroadcastExcept<T>(int peerIdToExclude, ref T packet) where T : struct, INetSerializable
    {
        // Itera sobre todos os peers conectados
        foreach (var peer in netManager.ConnectedPeerList)
        {
            // Se o peer atual não for o que queremos excluir...
            if (peer.Id != peerIdToExclude)
            {
                // ...enfileira a mensagem no buffer de unicast confiável dele.
                EnqueueReliableSend(peer.Id, ref packet);
            }
        }
    }
    
    public void EnqueueReliableBroadcast<T>(T[] packets) 
        where T : struct, INetSerializable
    {
        for (var i = 0; i < packets.Length; i++)
            EnqueueReliableBroadcast(ref packets[i]);
    }
    
    // --- MÉTODOS DE BROADCAST COM EXCLUSÃO (NOVOS) ---
    
    /// <summary>
    /// Enfileira um pacote para ser enviado a todos os peers, EXCETO um específico.
    /// A entrega é NÃO CONFIÁVEL (unreliable).
    /// </summary>
    public void EnqueueUnreliableBroadcastExcept<T>(int peerIdToExclude, ref T packet) where T : struct, INetSerializable
    {
        foreach (var peer in netManager.ConnectedPeerList)
            if (peer.Id != peerIdToExclude)
                EnqueueUnreliableSend(peer.Id, ref packet);
    }
    
    // --- MÉTODOS DE UNICAST (NOVOS) ---

    // --- MÉTODOS DE UNICAST ---
    public void EnqueueReliableSend<T>(int peerId, ref T packet) where T : struct, INetSerializable
    {
        if (!_reliablePeerWriters.TryGetValue(peerId, out var writer))
        {
            writer = GetWriter(); // <-- Obtém da piscina em vez de "new"
            _reliablePeerWriters[peerId] = writer;
        }
        packetProcessor.WriteNetSerializable(writer, ref packet);
    }
    
    public void EnqueueUnreliableSend<T>(int peerId, ref T packet) where T : struct, INetSerializable
    {
        if (!_unreliablePeerWriters.TryGetValue(peerId, out var writer))
        {
            writer = GetWriter(); // <-- Obtém da piscina em vez de "new"
            _unreliablePeerWriters[peerId] = writer;
        }
        packetProcessor.WriteNetSerializable(writer, ref packet);
    }
    
    // --- MÉTODOS DE ENVIO IMEDIATO (SEM BATCHING) ---
    // ... (O resto da classe, com Send, SendToServer, etc., pode permanecer o mesmo)
    
    public void SendNow<T>(ref T packet, int peerId, DeliveryMethod method = DeliveryMethod.ReliableOrdered) 
        where T : struct, INetSerializable
    {
        if (!netManager.TryGetPeerById(peerId, out var peer))
        {
            OnSendError(peerId);
            return;
        }
        
        var writer = GetWriter(); // <-- Obtém da piscina em vez de "new"
        packetProcessor.WriteNetSerializable(writer, ref packet);
        peer.Send(writer, method);
        ReturnWriter(writer);
    }
    
    public void SendArrayNow<T>(T[] packet, int peerId, DeliveryMethod method = DeliveryMethod.ReliableOrdered) 
        where T : struct, INetSerializable
    {
        if (!netManager.TryGetPeerById(peerId, out var peer))
        {
            OnSendError(peerId);
            return;
        }
        
        var writer = GetWriter(); // <-- Obtém da piscina em vez de "new"
        for (var i = 0; i < packet.Length; i++)
            packetProcessor.WriteNetSerializable(writer, ref packet[i]);
        peer.Send(writer, method);
        ReturnWriter(writer);
    }
    
    public void FlushAllBuffers()
    {
        // Envia broadcasts
        if (_reliableBroadcastWriter.Length > 0)
        {
            netManager.SendToAll(_reliableBroadcastWriter, DeliveryMethod.ReliableOrdered);
            _reliableBroadcastWriter.Reset();
        }
        if (_unreliableBroadcastWriter.Length > 0)
        {
            netManager.SendToAll(_unreliableBroadcastWriter, DeliveryMethod.Unreliable);
            _unreliableBroadcastWriter.Reset();
        }

        // Envia pacotes para peers específicos (reliable)
        foreach (var (peerId, writer) in _reliablePeerWriters)
        {
            if (writer.Length > 0 && netManager.TryGetPeerById(peerId, out var peer))
            {
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
            ReturnWriter(writer);
        }
        _reliablePeerWriters.Clear();

        // Envia pacotes para peers específicos (unreliable)
        foreach (var (peerId, writer) in _unreliablePeerWriters)
        {
            if (writer.Length > 0 && netManager.TryGetPeerById(peerId, out var peer))
            {
                peer.Send(writer, DeliveryMethod.Unreliable);
            }
            ReturnWriter(writer);
        }
        _unreliablePeerWriters.Clear();
    }
    
    protected void OnSendError(int peerId)
    {
        logger.LogError($"[NetworkSender] Peer {peerId} não encontrado.");
    }
    
    public void Dispose()
    {
        // Limpa os buffers de broadcast
        _reliableBroadcastWriter.Reset();
        _unreliableBroadcastWriter.Reset();

        // Limpa os buffers de unicast
        foreach (var writer in _reliablePeerWriters.Values)
            ReturnWriter(writer);
        _reliablePeerWriters.Clear();

        foreach (var writer in _unreliablePeerWriters.Values)
            ReturnWriter(writer);
        _unreliablePeerWriters.Clear();

        // Limpa a piscina de writers
        _writerPool.Clear();
    }
}