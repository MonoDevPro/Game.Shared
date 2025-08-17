namespace Game.Core.Common;

public abstract class BaseEntity
{
    public int Id { get; private set; } // identidade
    public bool IsActive { get; private set; } = true; // ativo ou inativo
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset LastModified { get; set; }

    // Construtor sem parâmetros é necessário para o EF Core
    protected BaseEntity() { }

    protected BaseEntity(int id)
    {
        Id = id;
    }
    
    public virtual void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
    }
    
    public virtual void Activate()
    {
        if (IsActive) 
            return;

        IsActive = true;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetUnproxiedType(this) != GetUnproxiedType(other)) return false;

        // Somente Id: se ainda não foi atribuído (0), considere diferente
        if (Id == 0 || other.Id == 0) 
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode()
        => (GetUnproxiedType(this).ToString() + Id)
            .GetHashCode(); // hash baseado em Id

    internal static Type GetUnproxiedType(object obj)
    {
        var type = obj.GetType();
        var name = type.ToString();
        if (name.StartsWith("Castle.Proxies.") && type.BaseType is not null)
            return type.BaseType;
        return type;
    }
}