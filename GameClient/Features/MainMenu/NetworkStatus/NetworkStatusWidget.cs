// Local: Features/MainMenu/NetworkStatus/NetworkStatusWidget.cs

using GameClient.Core.Services;
using Godot;
using LiteNetLib;
using Microsoft.Extensions.DependencyInjection;
using Shared.Network;
using Shared.Network.Repository;

// Onde seu NetworkManager está

// Onde seu PeerRepository está

namespace GameClient.Features.MainMenu.NetworkStatus;

public partial class NetworkStatusWidget : Control
{
    // Enum de estado simplificado
    private enum NetworkState
    {
        Connected,
        Connecting,
        Disconnected
    }

    #region Nós da Cena (Configure no Inspetor)
    [Export] private Label _statusLabel;
    [Export] private Label _pingLabel;
    [Export] private Label _retryLabel;
    #endregion

    #region Configurações de Reconexão
    private const float INITIAL_RECONNECT_DELAY_S = 2.0f; // 2 segundos
    private const float MAX_RECONNECT_DELAY_S = 30.0f;    // 30 segundos
    private const float BACKOFF_FACTOR = 1.5f;            // Multiplica o delay por 1.5 a cada falha
    #endregion

    // Dependências (obtidas via Autoload)
    private PeerRepository _peerRepository;
    private NetworkManager _networkManager;

    private Timer _reconnectTimer;
    private float _currentReconnectDelay = INITIAL_RECONNECT_DELAY_S;

    public override void _Ready()
    {
        // 1. Obter serviços essenciais (Autoloads)
        _networkManager = GameServiceProvider.Instance.Services.GetRequiredService<NetworkManager>();
        _peerRepository = _networkManager.PeerRepository;

        // 2. Criar e configurar o Timer programaticamente
        _reconnectTimer = new Timer();
        _reconnectTimer.OneShot = true; // Queremos que ele dispare apenas uma vez por tentativa
        _reconnectTimer.Timeout += AttemptReconnect; // Conecta o sinal de timeout ao nosso método
        AddChild(_reconnectTimer);

        // 3. Inscrever-se nos eventos da rede
        _peerRepository.PeerConnected += OnPeerConnected;
        _peerRepository.PeerDisconnected += OnPeerDisconnected;
        _peerRepository.PeerLatencyUpdated += OnPingUpdate;

        // 4. Configurar estado inicial da UI
        SetState(NetworkState.Disconnected);
    }

    public override void _Process(double delta)
    {
        // Usa o _Process apenas para atualizar a contagem regressiva da UI.
        // É simples e não executa lógica pesada.
        if (_reconnectTimer.IsStopped())
        {
            _retryLabel.Visible = false;
        }
        else
        {
            _retryLabel.Text = $"Tentando reconectar em {Mathf.CeilToInt(_reconnectTimer.TimeLeft)}s...";
            _retryLabel.Visible = true;
        }
    }

    // --- Handlers dos Eventos de Rede ---

    private void OnPeerConnected(NetPeer peer)
    {
        GD.Print("NetworkStatus: Conectado!");
        _reconnectTimer.Stop(); // Para qualquer tentativa de reconexão pendente
        _currentReconnectDelay = INITIAL_RECONNECT_DELAY_S; // Reseta o delay para a próxima vez
        SetState(NetworkState.Connected);
    }

    private void OnPeerDisconnected(NetPeer peer, string reason)
    {
        GD.Print($"NetworkStatus: Desconectado. Razão: {reason}. Iniciando reconexão...");
        SetState(NetworkState.Connecting);
        
        // Tenta reconectar imediatamente uma vez.
        // As próximas tentativas seguirão o delay do timer.
        AttemptReconnect();
    }

    private void OnPingUpdate(NetPeer peer, int ping)
    {
        _pingLabel.Text = $"Ping: {ping} ms";
        _pingLabel.AddThemeColorOverride("font_color", ping < 100 ? Colors.Green : (ping < 200 ? Colors.Yellow : Colors.Red));
    }

    // --- Lógica de Reconexão ---

    private void AttemptReconnect()
    {
        // Se já estivermos conectados (ex: o jogador conectou manualmente), não faz nada.
        if (_peerRepository.IsConnected(0))
        {
            OnPeerConnected(null); // Apenas atualiza a UI
            return;
        }
        
        if (_reconnectTimer.TimeLeft > 0)
        {
            GD.Print("NetworkStatus: Já tentando reconectar, aguardando o timer.");
            return; // Já está tentando reconectar, não faz nada
        }
        
        GD.Print("NetworkStatus: Tentando reconectar...");
        SetState(NetworkState.Connecting);

        // AQUI você chama o método de conexão do seu NetworkManager
        // Exemplo: _networkManager.Connect("127.0.0.1", 7777, "MySecretConnectionKey");
        _networkManager.Start(); // Garante que o NetManager está rodando antes de conectar

        // Agenda a PRÓXIMA tentativa de reconexão
        _reconnectTimer.WaitTime = _currentReconnectDelay;
        _reconnectTimer.Start();
        
        GD.Print($"NetworkStatus: Próxima tentativa em {_currentReconnectDelay} segundos.");

        // Aumenta o delay para a próxima vez, até o máximo
        _currentReconnectDelay = Mathf.Min(_currentReconnectDelay * BACKOFF_FACTOR, MAX_RECONNECT_DELAY_S);
    }

    // --- Controle da UI ---

    private void SetState(NetworkState newState)
    {
        switch (newState)
        {
            case NetworkState.Connected:
                _statusLabel.Text = "Conectado";
                _statusLabel.AddThemeColorOverride("font_color", Colors.Green);
                _pingLabel.Visible = true;
                _retryLabel.Visible = false;
                break;
            case NetworkState.Connecting:
                _statusLabel.Text = "Conectando...";
                _statusLabel.AddThemeColorOverride("font_color", Colors.Yellow);
                _pingLabel.Visible = false;
                _retryLabel.Visible = true;
                break;
            case NetworkState.Disconnected:
                _statusLabel.Text = "Desconectado";
                _statusLabel.AddThemeColorOverride("font_color", Colors.Red);
                _pingLabel.Visible = false;
                _retryLabel.Visible = false;
                break;
        }
    }
    
    public override void _ExitTree()
    {
        // Limpa as inscrições para evitar vazamentos de memória
        _peerRepository.PeerConnected -= OnPeerConnected;
        _peerRepository.PeerDisconnected -= OnPeerDisconnected;
        _peerRepository.PeerLatencyUpdated -= OnPingUpdate;
    }
}