using System.Numerics;

namespace ECS.Components;

public struct WallComponent
{
    public Vector2 Start;
    public Vector2 End;
    public int WallId;
}
