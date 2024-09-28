using System.Numerics;

namespace Core.Objects
{
    public class Ball : SceneObject2D
    {
        public Vector2 CenterPosition { get; set; }
        public Vector2 Velocity { get; set; }
        public float Radius { get; set; }

        public Ball(int id, Vector2 centerPosition, Vector2 velocity, float radius)
        {
            Id = id;
            CenterPosition = centerPosition;
            Velocity = velocity;
            Radius = radius;
        }
    }
}
