// Local: Core/UI/BaseWindow.cs

using System;
using Godot;

namespace GameClient.Core.UI;

/// <summary>
/// Uma classe base para todas as janelas do projeto.
/// Fornece funcionalidade comum para mostrar, esconder e comunicar eventos.
/// </summary>
public partial class BaseWindow : Window
{
    /// <summary>
    /// Disparado quando a janela é exibida.
    /// </summary>
    public event Action OnWindowOpened;

    /// <summary>
    /// Disparado quando a janela é fechada (seja pelo código ou pelo usuário).
    /// </summary>
    public event Action OnWindowClosed;

    public override void _Ready()
    {
        // Garante que a janela comece escondida.
        // Você pode configurar a visibilidade inicial no editor, mas isso é uma segurança extra.
        Hide();

        // Conecta o sinal padrão do Godot para quando o usuário clica no botão 'X' de fechar.
        // Quando isso acontecer, nosso método OnCloseRequested será chamado.
        this.CloseRequested += OnCloseRequested;
    }

    /// <summary>
    /// Mostra a janela. Pode ser estendido para incluir animações de entrada.
    /// </summary>
    public virtual void ShowWindow()
    {
        // Futuramente, você pode adicionar uma animação de "fade in" ou "scale up" aqui.
        Show();
        OnWindowOpened?.Invoke(); // Dispara o evento para quem estiver ouvindo.
        OnWindowShown(); // Chama o método virtual para a lógica da classe filha.
    }

    /// <summary>
    /// Esconde a janela. Pode ser estendido para incluir animações de saída.
    /// </summary>
    public virtual void HideWindow()
    {
        // Futuramente, você pode adicionar uma animação de "fade out" ou "scale down" aqui.
        Hide();
        OnWindowClosed?.Invoke(); // Dispara o evento.
        OnWindowHidden(); // Chama o método virtual.
    }

    /// <summary>
    /// Alterna a visibilidade da janela.
    /// </summary>
    public void Toggle()
    {
        if (Visible)
            HideWindow();
        else
            ShowWindow();
    }

    /// <summary>
    /// Este método é chamado automaticamente pelo Godot quando o usuário clica no 'X'.
    /// </summary>
    protected virtual void OnCloseRequested()
    {
        // O comportamento padrão é simplesmente esconder a janela.
        HideWindow();
    }

    // --- Métodos para serem sobrescritos (overridden) pelas classes filhas ---

    /// <summary>
    /// "Gancho" para código que deve ser executado quando a janela é mostrada.
    /// </summary>
    protected virtual void OnWindowShown() { }

    /// <summary>
    /// "Gancho" para código que deve ser executado quando a janela é escondida.
    /// </summary>
    protected virtual void OnWindowHidden() { }
    
    public override void _ExitTree()
    {
        // Desconecta o sinal para evitar chamadas após a remoção do nó.
        this.CloseRequested -= OnCloseRequested;
        base._ExitTree();
    }
}