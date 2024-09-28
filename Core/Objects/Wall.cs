using System.Numerics;

namespace Core.Objects
{
    public class Wall : SceneObject2D
    {
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }

        public Wall(int id, Vector2 start, Vector2 end)
        {
            Id = id;
            Start = start;
            End = end;
        }
    }
}
