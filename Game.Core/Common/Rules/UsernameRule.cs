using System.Text.RegularExpressions;

namespace Game.Core.Common.Rules;

public static partial class UsernameRule
{
    /// <summary>
    /// Expressão regular que define usernames válidos:
    ///  - Entre 3 e 20 caracteres
    ///  - Permitidos: letras (A-Z, a-z), dígitos (0-9), ponto, underscore e traço
    /// </summary>
    private const string Pattern = "^[a-zA-Z0-9._-]{3,20}$";

    /// <summary>
    /// Descrição legível da regra de username.
    /// </summary>
    public static string Description =>
        "O nome de usuário deve ter entre 3 e 20 caracteres e conter apenas letras, números, ponto (.), underline (_) ou traço (-).";

    // Gera em tempo de compilação um Regex estático pré-compilado
    [GeneratedRegex(Pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    public static partial Regex UsernameRegex();

    /// <summary>
    /// Verifica se o input é um username válido.
    /// </summary>
    /// <param name="input">O texto a validar.</param>
    /// <returns>True se for válido; caso contrário, false.</returns>
    public static bool IsValid(string input)
        => !string.IsNullOrWhiteSpace(input) && UsernameRegex().IsMatch(input);

    /// <summary>
    /// Valida o username e retorna, opcionalmente, uma mensagem de erro.
    /// </summary>
    /// <param name="input">O nome de usuário a validar.</param>
    /// <param name="errorMessage">Mensagem de erro caso inválido; null se válido.</param>
    /// <returns>True se válido; false caso contrário.</returns>
    public static bool TryValidate(string input, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = "O nome de usuário não pode ser vazio.";
            return false;
        }

        if (!UsernameRegex().IsMatch(input))
        {
            errorMessage = Description;
            return false;
        }

        errorMessage = null;
        return true;
    }
}