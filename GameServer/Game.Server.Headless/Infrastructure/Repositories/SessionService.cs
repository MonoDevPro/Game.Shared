using System.Collections.Concurrent;
using LiteNetLib;
using Shared.Core.Network.Repository;

namespace Game.Server.Headless.Infrastructure.Repositories;

public class SessionService
{
    // Map peerId -> accountId
    private readonly ConcurrentDictionary<int, int> _accountByPeer = new();
    // Map accountId -> peerId
    private readonly ConcurrentDictionary<int, int> _peerByAccount = new();

    public SessionService(PeerRepository peers)
    {
        peers.PeerDisconnected += (peer, reason) => Unbind(peer);
    }

    public void Bind(NetPeer peer, int accountId)
    {
        _accountByPeer[peer.Id] = accountId;
        _peerByAccount[accountId] = peer.Id;
    }

    public void Unbind(NetPeer peer)
    {
        if (_accountByPeer.TryRemove(peer.Id, out var accountId))
            _peerByAccount.TryRemove(accountId, out _);
    }

    public bool TryGetAccount(NetPeer peer, out int accountId) => _accountByPeer.TryGetValue(peer.Id, out accountId);
}
