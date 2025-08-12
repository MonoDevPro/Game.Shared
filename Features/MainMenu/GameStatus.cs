using System.Net;
using System.Net.Sockets;
using GameClient.Core.Services;
using Godot;
using LiteNetLib;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Network;

namespace GameClient.Features.MainMenu;

public partial class GameStatus : Control
{
    public enum GameStatusType : byte
    {
        Disconnected,
        Connecting,
        Connected,
        Error,
    }
    [Export] public GameStatusType StatusType
    {
        get => _gameStatusType;
        set
        {
            if (_gameStatusType == value)
                return;
            SetStatus(_gameStatusType);
        }
    }
    [Export] public NodePath StatusLabelPath;
    [Export] public NodePath PingLabelPath;
    
    private Label StatusLabel { get; set; }
    private Label PingLabel { get; set; }
    private NetworkManager _networkManager;
    private GameStatusType _gameStatusType = GameStatusType.Disconnected;
    
    public override void _Ready()
    {
        base._Ready();
        
        StatusLabel = GetNode<Label>(StatusLabelPath);
        PingLabel = GetNode<Label>(PingLabelPath);
        _networkManager = GameServiceProvider.Instance.Services.GetRequiredService<NetworkManager>();
        
        // Connect signals for peer connection events
        _networkManager.PeerRepository.PeerConnected += OnPeerConnected;
        _networkManager.PeerRepository.PeerDisconnected += OnPeerDisconnected;
        _networkManager.PeerRepository.NetworkError += OnPeerConnectionError;
        
        // Initialize the status
        SetStatus(StatusType);
        
        GD.Print($"PopupGameStatus initialized with status: {StatusType}");
    }
    
    private void OnPeerConnected(NetPeer peer)
    {
        // Update the status when a peer connects
        SetStatus(GameStatusType.Connected);

        // Show the ping label since the peer is connected
        PingLabel.Text = "Ping: Calculating...";
        PingLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        PingLabel.Visible = true;
        
        // Connect the ping update signal
        _networkManager.PeerRepository.PeerLatencyUpdated += UpdatePingLabel;
    }
    
    private void OnPeerDisconnected(NetPeer peer, string reason)
    {
        // Update the status when a peer disconnects
        SetStatus(GameStatusType.Disconnected);
        
        // Hide the ping label since the peer is disconnected
        PingLabel.Visible = false;
        // Disconnect the ping update signal
        _networkManager.PeerRepository.PeerLatencyUpdated -= UpdatePingLabel;
    }
    
    private void OnPeerConnectionError(IPEndPoint endPoint, SocketError error)
    {
        // Update the status when there is a connection error
        SetStatus(GameStatusType.Error);
        
        GD.PrintErr($"Connection error at {endPoint}: {error}");
    }
    
    private void UpdatePingLabel(NetPeer peer, int ping)
    {
        PingLabel.Text = $"Ping: {ping} ms";

        switch (ping)
        {
            case < 100:
                PingLabel.AddThemeColorOverride("font_color", Colors.Green);
                break;
            case < 200:
                PingLabel.AddThemeColorOverride("font_color", Colors.Yellow);
                break;
            default:
                PingLabel.AddThemeColorOverride("font_color", Colors.Red);
                break;
        }
    }
    
    public void SetStatus(GameStatusType statusType)
    {
        _gameStatusType = statusType;
        
        switch (statusType)
        {
            case GameStatusType.Disconnected:
                StatusLabel.Text = "Disconnected from the server.";
                StatusLabel.AddThemeColorOverride("font_color", Colors.Red);
                PingLabel.Visible = false;
                break;
            case GameStatusType.Connecting:
                StatusLabel.Text = "Connecting to the server...";
                StatusLabel.AddThemeColorOverride("font_color", Colors.Yellow);
                PingLabel.Visible = false;
                break;
            case GameStatusType.Connected:
                StatusLabel.Text = "Successfully connected to the server!";
                StatusLabel.AddThemeColorOverride("font_color", Colors.Green);
                PingLabel.Visible = true;
                break;
            case GameStatusType.Error:
                StatusLabel.Text = "An error occurred while connecting to the server.";
                StatusLabel.AddThemeColorOverride("font_color", Colors.Red);
                PingLabel.Visible = false;
                break;
        }
    }
    
    public override void _ExitTree()
    {
        base._ExitTree();
        
        // Disconnect signals to avoid memory leaks
        if (_networkManager == null)
            return;

        _networkManager.PeerRepository.PeerConnected -= OnPeerConnected;
        _networkManager.PeerRepository.PeerDisconnected -= OnPeerDisconnected;
        _networkManager.PeerRepository.NetworkError -= OnPeerConnectionError;
    }
    
}
