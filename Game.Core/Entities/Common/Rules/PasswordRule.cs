using System.Text.RegularExpressions;

namespace Game.Core.Entities.Common.Rules;

/// <summary>
/// Regras de validação para senhas.
/// </summary>
public static partial class PasswordRule
{
    /// <summary>
    /// Padrão regex para senhas fortes: 6-30 caracteres, contendo ao menos 1 maiúscula, 1 minúscula e 1 número.
    /// </summary>
    private const string Pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d]{6,30}$";

    /// <summary>
    /// Descrição legível da regra de senha.
    /// </summary>
    public static string Description =>
        "A senha deve ter entre 6 e 30 caracteres e conter ao menos uma letra maiúscula, uma minúscula e um número.";

    /// <summary>
    /// Regex pré-compilado para validação.
    /// </summary>
    [GeneratedRegex(Pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    public static partial Regex PasswordRegex();

    /// <summary>
    /// Verifica se a senha atende aos critérios de complexidade.
    /// </summary>
    public static bool IsValid(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return PasswordRegex().IsMatch(input);
    }

    /// <summary>
    /// Tenta validar a senha, retornando uma mensagem de erro caso inválida.
    /// </summary>
    public static bool TryValidate(string input, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = "A senha não pode ser vazia.";
            return false;
        }

        if (!PasswordRegex().IsMatch(input))
        {
            errorMessage = Description; // Reutiliza a descrição como a mensagem de erro
            return false;
        }

        errorMessage = null;
        return true;
    }
}