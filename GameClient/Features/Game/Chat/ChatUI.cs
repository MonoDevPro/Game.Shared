using Arch.Bus;
using Godot;
using Shared.Features.Game.Chat.Packets;

namespace GameClient.Features.Game.Chat;

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
		
		Hook();
	}
	
	[Event(order: 0)]
	public void OnChatMessageReceived(ref ChatMessageBroadcast chatMessageReceivedEvent)
	{
		AddChatMessage(chatMessageReceivedEvent.SenderName, chatMessageReceivedEvent.Message, Colors.White);
	}
	
	private void OnTextSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return;

		// Levanta um aviso no quadro a dizer "por favor, enviem isto!"
		var chatMessageRequest = new ChatMessageRequest
		{
			Message = text,
		};
		EventBus.Send(ref chatMessageRequest);
        
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
		
		Unhook();
		base._ExitTree();
	}
}