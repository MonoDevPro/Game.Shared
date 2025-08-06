using Godot;

namespace Game.Shared.Client.Presentation.UI.Chat;

public partial class ChatUI : Control
{
	// Sinal que será emitido quando o jogador enviar uma mensagem
	[Signal]
	public delegate void MessageSentEventHandler(string message);

	private RichTextLabel _chatLog;
	private LineEdit _inputBox;

	public override void _Ready()
	{
		_chatLog = GetNode<RichTextLabel>("VBoxContainer/RichTextLabel");
		_inputBox = GetNode<LineEdit>("VBoxContainer/LineEdit");

		// Conecta o sinal 'text_submitted' do LineEdit
		_inputBox.TextSubmitted += OnTextSubmitted;
	}

	private void OnTextSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return;

		// Emite o sinal com a mensagem para a lógica do jogo capturar
		EmitSignal(ChatUI.SignalName.MessageSent, text);
        
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
		
		base._ExitTree();
	}
}