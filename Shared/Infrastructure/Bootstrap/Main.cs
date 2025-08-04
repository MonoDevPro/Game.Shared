using Godot;

namespace Game.Shared.Shared.Infrastructure.Bootstrap;

public partial class Main : Node
{
	// Caminhos para as cenas do cliente e do servidor
	[Export]private string ServerScenePath = "res://Server/Infrastructure/Bootstrap/ServerBootstrap.tscn";
	[Export] private string ClientScenePath = "res://Client/Infrastructure/Bootstrap/ClientBootstrap.tscn";
	
	private Button _serverButton;
	private Button _clientButton;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Encontra os nós dos botões na cena
		_serverButton = GetNode<Button>("%StartServerButton");
		_clientButton = GetNode<Button>("%StartClientButton");

		// Conecta o sinal 'pressed' de cada botão a um método
		_serverButton.Pressed += OnStartServerPressed;
		_clientButton.Pressed += OnStartClientPressed;
	}
	
	private void OnStartServerPressed()
	{
		GD.Print("Iniciando como Servidor...");
		// Muda para a cena do servidor
		GetTree().ChangeSceneToFile(ServerScenePath);
	}

	private void OnStartClientPressed()
	{
		GD.Print("Iniciando como Cliente...");
		// Muda para a cena do cliente
		GetTree().ChangeSceneToFile(ClientScenePath);
	}
}