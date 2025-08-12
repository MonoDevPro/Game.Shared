using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Core.Common.Constants;

namespace Shared.Core.Network.Repository;

public sealed class PeerRepository : IDisposable
{
    private readonly EventBasedNetListener _listener;
    private readonly NetManager _netManager;
    private readonly ILogger<PeerRepository> _logger;

    public PeerRepository(EventBasedNetListener listener, NetManager netManager, ILogger<PeerRepository> logger)
    {
        _listener = listener;
        _netManager = netManager;
        _logger = logger;
        
        _listener.PeerConnectedEvent        += OnPeerConnected;
        _listener.PeerDisconnectedEvent     += OnPeerDisconnected;
        _listener.ConnectionRequestEvent    += OnConnectionRequest;
        _listener.NetworkLatencyUpdateEvent += OnNetworkLatencyUpdate;
        _listener.NetworkErrorEvent         += OnNetworkError;
    }

    public event Action<NetPeer>? PeerConnected;
    public event Action<NetPeer, string>? PeerDisconnected;
    public event Action<int, string>? ConnectionRequest;
    public event Action<NetPeer, int>? PeerLatencyUpdated;
    public event Action<IPEndPoint, SocketError>? NetworkError;
    
    public bool TryGetPeerById(int peerId, out NetPeer peer)
        => _netManager.TryGetPeerById(peerId, out  peer);
    
    private int MaxClients => NetworkConfigurations.MaxClients;

    public bool IsRunning() => _netManager.IsRunning;

    public bool IsConnected(int peerId) => ((_netManager.TryGetPeerById(peerId, out NetPeer peer)
                                             && peer.ConnectionState == ConnectionState.Connected));

    private void OnPeerConnected(NetPeer peer)
    {
        _logger.LogInformation($"Client connected: {peer.Address}");
        PeerConnected?.Invoke(peer);
    }
    private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.LogInformation($"Client disconnected: {peer.Address}, Reason: {disconnectInfo.Reason}");
        PeerDisconnected?.Invoke(peer, disconnectInfo.Reason.ToString());
    }
    private void OnConnectionRequest(ConnectionRequest request)
    {
        var port      = request.RemoteEndPoint.Port;
        var secretKey = NetworkConfigurations.SecretKey;
        var maxClients = NetworkConfigurations.MaxClients;

        if (_netManager.ConnectedPeersCount < maxClients)
        {
            request.AcceptIfKey(secretKey);
            _logger.LogInformation($"Connection request accepted from {request.RemoteEndPoint}.");
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
        _logger.LogInformation($"Network error at {endPoint}: {socketErrorCode}");
        NetworkError?.Invoke(endPoint, socketErrorCode);
    }

    public void Dispose()
    {
        _listener.PeerConnectedEvent        -= OnPeerConnected;
        _listener.PeerDisconnectedEvent     -= OnPeerDisconnected;
        _listener.ConnectionRequestEvent    -= OnConnectionRequest;
        _listener.NetworkLatencyUpdateEvent -= OnNetworkLatencyUpdate;
        _listener.NetworkErrorEvent         -= OnNetworkError;
        _logger.LogInformation("PeerRepositoryRef disposed.");
    }
}