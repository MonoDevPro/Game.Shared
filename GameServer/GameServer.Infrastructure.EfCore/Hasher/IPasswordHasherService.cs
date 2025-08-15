namespace GameServer.Infrastructure.EfCore.Hasher;

/// <summary>
/// Define o contrato para um serviço de hashing e verificação de senhas.
/// </summary>
public interface IPasswordHasherService
{
    /// <summary>
    /// Gera um hash a partir de uma senha de texto plano.
    /// </summary>
    /// <param name="password">A senha a ser hasheada.</param>
    /// <returns>A string do hash gerado (que inclui o salt).</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifica se uma senha de texto plano corresponde a um hash existente.
    /// </summary>
    /// <param name="password">A senha de texto plano fornecida pelo usuário.</param>
    /// <param name="hashedPassword">O hash armazenado no banco de dados.</param>
    /// <returns>True se a senha corresponder, caso contrário, false.</returns>
    bool VerifyPassword(string password, string hashedPassword);
}