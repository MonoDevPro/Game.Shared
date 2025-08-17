using System.Text.RegularExpressions;

namespace Game.Core.Common.Rules;

/// <summary>
/// Regras de validação para nomes de personagens.
/// </summary>
public static partial class CharacterNameRule
{
    /// <summary>
    /// Padrão regex para nomes de personagens: 3 a 20 caracteres, permitindo apenas letras, espaços e apóstrofos.
    /// </summary>
    private const string Pattern = @"^[a-zA-Z' ]{3,20}$";

    /// <summary>
    /// Descrição legível da regra de nome de personagem.
    /// </summary>
    public static string Description =>
        "O nome do personagem deve ter entre 3 e 20 caracteres e pode conter apenas letras, espaços e apóstrofos (').";

    /// <summary>
    /// Regex pré-compilado para validação.
    /// </summary>
    [GeneratedRegex(Pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    public static partial Regex CharacterNameRegex();

    /// <summary>
    /// Verifica se o nome do personagem é válido.
    /// </summary>
    public static bool IsValid(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;
        
        // Verifica se o nome não contém espaços duplos ou mais,
        // ou se começa/termina com espaço ou apóstrofo, o que a regex permite.
        if (input.Contains("  ") || input.StartsWith(" ") || input.EndsWith(" ") || input.StartsWith("'") || input.EndsWith("'"))
            return false;

        return CharacterNameRegex().IsMatch(input);
    }

    /// <summary>
    /// Tenta validar o nome do personagem, retornando uma mensagem de erro caso inválido.
    /// </summary>
    public static bool TryValidate(string input, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = "O nome do personagem não pode ser vazio.";
            return false;
        }

        if (!CharacterNameRegex().IsMatch(input))
        {
            errorMessage = Description; // A descrição geral cobre a maioria dos casos de regex.
            return false;
        }
        
        if (input.Contains("  ") || input.StartsWith(" ") || input.EndsWith(" ") || input.StartsWith("'") || input.EndsWith("'"))
        {
            errorMessage = "O nome não pode conter espaços duplos, nem começar ou terminar com espaços ou apóstrofos.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}