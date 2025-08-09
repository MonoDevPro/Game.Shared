using GameClient.Infrastructure.Events;
using Godot;
using Shared.Infrastructure.Network.Data.Chat;

namespace GameClient.Presentation.UI.Chat;

public partial class ChatUI : Control
{
	private RichTextLabel _chatLog;
	private LineEdit _inputBox;

	public override void _Ready()
	{
		_chatLog = GetNode<RichTextLabel>("VBoxContainer/RichTextLabel");
		_inputBox = GetNode<LineEdit>("VBoxContainer/LineEdit");

		// Conecta o sinal 'text_submitted' do LineEdit
		_inputBox.TextSubmitted += OnTextSubmitted;
		
		// A UI ouve o aviso "mensagem recebida"
		ChatEvents.OnChatMessageReceived += OnChatMessageReceived;
	}
	
	// Método chamado quando o evento de mensagem recebida é levantado
	private void OnChatMessageReceived(ChatMessageBroadcast packet)
	{
		// A lógica de adicionar a mensagem permanece a mesma
		AddChatMessage(packet.SenderName, packet.Message, Colors.White);
	}

	private void OnTextSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return;

		// Levanta um aviso no quadro a dizer "por favor, enviem isto!"
		ChatEvents.RaiseSendMessageRequested(text);
        
		// Limpa a caixa de texto e a foca novamente
		_inputBox.Clear();
		_inputBox.GrabFocus();
	}

	/// <summary>
	/// Adiciona uma nova mensagem ao log de chat, formatada com BBCode.
	/// </summary>
	public void AddChatMessage(string author, string message, Color authorColor)
	{
		_chatLog.AppendText($"[color=#{authorColor.ToHtml(false)}]{author}[/color]: {message}\n");
	}
	
	public override void _ExitTree()
	{
		// Desconecta o sinal para evitar vazamentos de memória
		_inputBox.TextSubmitted -= OnTextSubmitted;
		ChatEvents.OnChatMessageReceived -= OnChatMessageReceived;
		base._ExitTree();
	}
}