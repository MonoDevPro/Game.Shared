namespace GameServer.Infrastructure.EfCore.Hasher;

/// <summary>
/// Implementação do serviço de hashing de senhas usando o algoritmo BCrypt.
/// </summary>
public class BCryptPasswordHasherService : IPasswordHasherService
{
    // O "work factor" determina o quão "caro" é computar o hash.
    // Um valor maior é mais seguro, mas mais lento. 12 é um bom padrão.
    private const int WorkFactor = 12;

    public string HashPassword(string password)
    {
        // O BCrypt.Net gera e embute o "salt" automaticamente no hash final.
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        // A função Verify extrai o salt do hashedPassword e faz a comparação.
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}