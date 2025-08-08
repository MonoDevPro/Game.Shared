namespace Shared.Infrastructure.ECS.Tags;

/// <summary>
/// Tag para indicar que a entidade está atualmente se movendo de um tile para outro.
/// Usada para prevenir novos movimentos até que o atual termine.
/// </summary>
public struct IsMovingTag {}