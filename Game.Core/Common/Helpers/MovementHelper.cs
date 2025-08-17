using Game.Core.Common.Enums;
using Game.Core.Common.ValueObjetcs;

namespace Game.Core.Common.Helpers;

public class MovementHelper
{
    public static float CalculateMovementDuration(MapPosition start, MapPosition target, float speed)
    {
        var posStart = start.ToWorldPosition();
        var posTarget = target.ToWorldPosition();
        
        // Calcula a distância entre os pontos
        var distance = MathF.Sqrt(
            MathF.Pow(posTarget.X - posStart.X, 2) + MathF.Pow(posTarget.Y - posStart.Y, 2)
        );

        // Calcula o tempo necessário para percorrer essa distância com a velocidade dada
        return distance / speed;
    }
    
    public static MapPosition CalculateTargetPosition(MapPosition start, MapPosition direction)
    {
        // Calcula a nova posição baseada na direção
        return start + direction;
    }
    
    public static MapPosition CalculateTargetPosition(MapPosition start, DirectionEnum direction)
    {
        // Converte a direção para um vetor de movimento
        var moveVector = direction.ToMapPosition();
        return start + moveVector;
    }
    
    public static DirectionEnum CalculateDirection(MapPosition start, MapPosition target)
    {
        // Calcula a direção baseada na posição inicial e na posição alvo
        var directionVector = target - start;
        return directionVector.ToDirection();
    }
    
    public static DirectionEnum CalculateDirection(MapPosition start, DirectionEnum direction)
    {
        // Converte a direção para um vetor de movimento
        var moveVector = direction.ToMapPosition();
        return moveVector.ToDirection();
    }
    
    public static MapPosition CalculateStartPosition(MapPosition target, DirectionEnum direction)
    {
        // Inverte a direção para calcular a posição inicial
        var moveVector = direction.ToMapPosition();
        return target - moveVector;
    }
}