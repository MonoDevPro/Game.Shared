using Godot;

namespace GameClient.Presentation.Entities.Character.Infos;

[GlobalClass]
public partial class PlayerInfoDisplay : Control
{
    [Export] private NodePath _nameLabelPath;
    [Export] private NodePath _backgroundPanelPath;

    private Label _nameLabel;
    private Panel _backgroundPanel;

    public override void _Ready()
    {
        _nameLabel = GetNode<Label>(_nameLabelPath);
        _backgroundPanel = GetNode<Panel>(_backgroundPanelPath);

        // O painel de fundo começa invisível
        _backgroundPanel.Visible = false;

        // Conectamos os sinais ao próprio Control raiz.
        // Assim, a área de detecção é a do nosso componente inteiro.
        
        _nameLabel.MouseEntered += OnMouseEntered;
        _nameLabel.MouseExited += OnMouseExited;
    }

    /// <summary>
    /// Atualiza as informações exibidas e ajusta o tamanho do componente.
    /// </summary>
    public void UpdateInfo(string playerName)
    {
        _nameLabel.Text = playerName;
        Callable.From(UpdateSizing).CallDeferred();
    }

    private void UpdateSizing()
    {
        // 1. Obtemos o tamanho que o Label precisa.
        var newSize = _nameLabel.GetMinimumSize();

        // 2. Garantimos que o painel de fundo preencha o Control raiz.
        _backgroundPanel.Size = newSize + new Vector2(8, 8); // Pequeno padding
        _backgroundPanel.Size = new Vector2(Mathf.Max(_backgroundPanel.Size.X, 50), _backgroundPanel.Size.Y); // Largura mínima
        _backgroundPanel.Position -= new Vector2(4, 4); // Pequeno padding
    }

    private void OnMouseEntered()
    {
        _backgroundPanel.Visible = true;
    }

    private void OnMouseExited()
    {
        _backgroundPanel.Visible = false;
    }
    
    public override void _ExitTree()
    {
        // Desconectamos os sinais para evitar vazamentos de memória.
        _nameLabel.MouseEntered -= OnMouseEntered;
        _nameLabel.MouseExited -= OnMouseExited;

        base._ExitTree();
    }
}