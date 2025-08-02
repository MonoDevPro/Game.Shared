using System;
using System.Net;
using System.Net.Sockets;
using Game.Shared.Shared.Infrastructure.Network.Config;
using Godot;
using LiteNetLib;

namespace Game.Shared.Shared.Infrastructure.Network.Repository;

public sealed class PeerRepositoryRef(EventBasedNetListener listener, NetManager netManager)
{
    public event Action<NetPeer> PeerConnected;
    public event Action<NetPeer, string> PeerDisconnected;
    public event Action<int, string> ConnectionRequest;
    public event Action<NetPeer, int> PeerLatencyUpdated;
    public event Action<IPEndPoint, SocketError> NetworkError;
    
    public bool TryGetPeerById(int peerId, out NetPeer peer)
        => netManager.TryGetPeerById(peerId, out  peer);
    
    private int MaxClients => NetworkConfigurations.MaxClients;

    public void Start()
    {
        listener.PeerConnectedEvent    += OnPeerConnected;
        listener.PeerDisconnectedEvent += OnPeerDisconnected;
        listener.ConnectionRequestEvent += OnConnectionRequest;
        listener.NetworkLatencyUpdateEvent += OnNetworkLatencyUpdate;
        listener.NetworkErrorEvent     += OnNetworkError;
        GD.Print("PeerRepositoryRef started.");
    }
    
    public void Stop()
    {
        listener.PeerConnectedEvent    -= OnPeerConnected;
        listener.PeerDisconnectedEvent -= OnPeerDisconnected;
        listener.ConnectionRequestEvent -= OnConnectionRequest;
        listener.NetworkLatencyUpdateEvent -= OnNetworkLatencyUpdate;
        GD.Print("PeerRepositoryRef stopped.");
    }
    
    public bool IsRunning() => netManager.IsRunning;

    public bool IsConnected(int peerId) => ((netManager.TryGetPeerById(peerId, out NetPeer peer)
                                             && peer.ConnectionState == ConnectionState.Connected));

    private void OnPeerConnected(NetPeer peer)
    {
        GD.Print($"Client connected: {peer.Address}");
        PeerConnected?.Invoke(peer);
    }
    private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        GD.Print($"Client disconnected: {peer.Address}, Reason: {disconnectInfo.Reason}");
        PeerDisconnected?.Invoke(peer, disconnectInfo.Reason.ToString());
    }
    private void OnConnectionRequest(ConnectionRequest request)
    {
        var port      = request.RemoteEndPoint.Port;
        var secretKey = NetworkConfigurations.SecretKey;
        var maxClients = NetworkConfigurations.MaxClients;

        if (netManager.ConnectedPeersCount < maxClients)
        {
            request.AcceptIfKey(secretKey);
            GD.Print($"Connection request accepted from {request.RemoteEndPoint}.");
            ConnectionRequest?.Invoke(request.RemoteEndPoint.Port, request.RemoteEndPoint.Address.ToString());
        }
        else
            request.Reject();
    }
    
    private void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        PeerLatencyUpdated?.Invoke(peer, latency);
    }
    
    private void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
    {
        GD.PrintErr($"Network error at {endPoint}: {socketErrorCode}");
        NetworkError?.Invoke(endPoint, socketErrorCode);
    }
}
