using System.Text.RegularExpressions;

namespace Game.Core.Entities.Common.Rules;

/// <summary>
/// Regras de validação para endereços de e-mail.
/// </summary>
public static partial class EmailRule
{
    /// <summary>
    /// Padrão regex para e-mails válidos: texto@texto.texto, sem espaços.
    /// </summary>
    private const string Pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

    /// <summary>
    /// Descrição legível da regra de e-mail.
    /// </summary>
    public static string Description =>
        "O e-mail deve estar no formato 'usuario@dominio.com' e não pode conter espaços.";

    /// <summary>
    /// Regex pré-compilado para validação inicial.
    /// </summary>
    [GeneratedRegex(Pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    public static partial Regex EmailRegex();

    /// <summary>
    /// Verifica se o e-mail é válido via regex e MailAddress.
    /// </summary>
    public static bool IsValid(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        if (!EmailRegex().IsMatch(input))
            return false;

        try
        {
            _ = new System.Net.Mail.MailAddress(input);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tenta validar o e-mail, retornando mensagem de erro caso inválido.
    /// </summary>
    public static bool TryValidate(string input, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = "O e-mail não pode ser vazio.";
            return false;
        }

        if (!EmailRegex().IsMatch(input))
        {
            errorMessage = Description;
            return false;
        }

        try
        {
            _ = new System.Net.Mail.MailAddress(input);
        }
        catch
        {
            errorMessage = "Formato de e-mail inválido.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}