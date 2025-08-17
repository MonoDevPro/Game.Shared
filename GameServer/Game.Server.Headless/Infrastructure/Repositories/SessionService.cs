using System.Collections.Concurrent;
using LiteNetLib;
using Shared.Network.Packets.MainMenu.Character;
using Shared.Network.Repository;

namespace Game.Server.Headless.Infrastructure.Repositories;

public class SessionService
{
    // Map peerId -> accountId
    private readonly ConcurrentDictionary<int, int> _accountByPeer = new();
    // Map accountId -> peerId
    private readonly ConcurrentDictionary<int, int> _peerByAccount = new();
    // Selected character per peer
    private readonly ConcurrentDictionary<int, CharacterData> _selectedCharacterByPeer = new();

    public SessionService(PeerRepository peers)
    {
        peers.PeerDisconnected += (peer, reason) => Unbind(peer);
    }

    public void Bind(NetPeer peer, int accountId)
    {
        _accountByPeer[peer.Id] = accountId;
        _peerByAccount[accountId] = peer.Id;
    }

    // Overloads based on peerId to avoid requiring NetPeer instance
    public void Bind(int peerId, int accountId)
    {
        _accountByPeer[peerId] = accountId;
        _peerByAccount[accountId] = peerId;
    }

    public void Unbind(NetPeer peer)
    {
        if (_accountByPeer.TryRemove(peer.Id, out var accountId))
            _peerByAccount.TryRemove(accountId, out _);
        _selectedCharacterByPeer.TryRemove(peer.Id, out _);
    }

    public void Unbind(int peerId)
    {
        if (_accountByPeer.TryRemove(peerId, out var accountId))
            _peerByAccount.TryRemove(accountId, out _);
        _selectedCharacterByPeer.TryRemove(peerId, out _);
    }

    public bool TryGetAccount(NetPeer peer, out int accountId) => _accountByPeer.TryGetValue(peer.Id, out accountId);

    public bool TryGetAccount(int peerId, out int accountId) => _accountByPeer.TryGetValue(peerId, out accountId);

    // Selected character helpers
    public void SetSelectedCharacter(int peerId, CharacterData data) => _selectedCharacterByPeer[peerId] = data;

    public bool TryGetSelectedCharacter(int peerId, out CharacterData data) => _selectedCharacterByPeer.TryGetValue(peerId, out data);

    public void ClearSelectedCharacter(int peerId) => _selectedCharacterByPeer.TryRemove(peerId, out _);
}
