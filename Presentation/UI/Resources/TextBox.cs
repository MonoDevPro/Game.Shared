using Godot;

namespace GameClient.Presentation.UI.Resources;

public partial class TextBox : LineEdit
{
    public enum TextBoxType : byte
    {
        CharacterName,
        ChatMessage,
        ItemDescription,
        Other
    }
    
    [Export] public TextBoxType Type { get; set; } = TextBoxType.Other;
    
    public override void _Ready()
    {
        base._Ready();
        
        // Set default placeholder text based on the type of TextBox
        switch (Type)
        {
            case TextBoxType.CharacterName:
                /*PlaceholderText = "Enter character name";
                MaxLength = Validation.MaxCharacterNameLength;
                TooltipText = "Character name must be between " + 
                              Validation.MinCharacterNameLength + " and " + 
                              Validation.MaxCharacterNameLength + " characters long." +
                              " Only alphanumeric characters and underscores are allowed.";*/
                break;
            case TextBoxType.ChatMessage:
                PlaceholderText = "Type your message here";
                break;
            case TextBoxType.ItemDescription:
                PlaceholderText = "Describe the item";
                break;
            default:
                PlaceholderText = "Enter text here";
                break;
        }
        
        // Connect the text changed signal to handle validation or formatting
        TextChanged += OnTextChanged;
    }
    
    private void OnTextChanged(string newText)
    {
        // Optionally handle text changes, e.g., validation or formatting
        if (Type == TextBoxType.CharacterName)
        {
            /*TooltipText = "Character name must be between " + 
                             Validation.MinCharacterNameLength + " and " + 
                             Validation.MaxCharacterNameLength + " characters long." +
                                " Only alphanumeric characters and underscores are allowed.";*/

            /*switch (newText.Length)
            {
                // Validate character name length
                case > Validation.MaxCharacterNameLength:
                    AddThemeColorOverride("font_color", Colors.Red);
                    TooltipText = "Character name is too long.";
                    return;
                case < Validation.MinCharacterNameLength:
                    AddThemeColorOverride("font_color", Colors.Red);
                    TooltipText = "Character name is too short.";
                    return;
            }

            if (!Validation.IsValidCharacterName(newText))
            {
                AddThemeColorOverride("font_color", Colors.Red);
                TooltipText = "Character name contains invalid characters.";
            }
            else
            {
                // Reset color if valid
                AddThemeColorOverride("font_color", Colors.Green);
                TooltipText = "Valid character name.";
            }*/
        }
    }
    
    public override void _ExitTree()
    {
        // Disconnect the signal to avoid memory leaks
        TextChanged -= OnTextChanged;
        base._ExitTree();
    }
    
}
