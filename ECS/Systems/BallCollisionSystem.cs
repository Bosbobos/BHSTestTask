using ECS.Components;
using Leopotam.EcsLite;
using System;
using System.Numerics;

namespace ECS.Systems
{
    public class BallCollisionSystem : IEcsInitSystem, IEcsRunSystem
    {
        private EcsFilter _ballFilter;
        private EcsFilter _wallFilter;

        private EcsPool<PositionComponent> _positionPool;
        private EcsPool<VelocityComponent> _velocityPool;
        private EcsPool<RadiusComponent> _radiusPool;
        private EcsPool<WallComponent> _wallPool;

        public void Init(IEcsSystems systems)
        {
            // �������� ��� ECS
            var world = systems.GetWorld();

            // ������������� ����� �����������
            _positionPool = world.GetPool<PositionComponent>();
            _velocityPool = world.GetPool<VelocityComponent>();
            _radiusPool = world.GetPool<RadiusComponent>();
            _wallPool = world.GetPool<WallComponent>();

            // ������� ������ ��� ������� (PositionComponent, VelocityComponent, RadiusComponent)
            _ballFilter = world.Filter<PositionComponent>()
                               .Inc<VelocityComponent>()
                               .Inc<RadiusComponent>()
                               .End();

            // ������� ������ ��� ���� (������ WallComponent)
            _wallFilter = world.Filter<WallComponent>().End();
        }

        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();

            // ���������� ��� ��������, ������� ������������� ������� �������
            foreach (var ballEntity in _ballFilter)
            {
                ref var ballPosition = ref _positionPool.Get(ballEntity);
                ref var ballVelocity = ref _velocityPool.Get(ballEntity);
                ref var ballRadius = ref _radiusPool.Get(ballEntity);

                // ���������� ��� ��������, ������� ������������� ������� ����
                foreach (var wallEntity in _wallFilter)
                {
                    ref var wall = ref _wallPool.Get(wallEntity);

                    // �������� ������������ � ������ "�������������"
                    if (WillCollide(ballPosition.Position, ballVelocity.Velocity, ballRadius.Radius, wall.Start, wall.End, out var collisionPoint, out var wallNormal))
                    {
                        // ������� ������������� �����
                        Console.WriteLine($"Collision detected with wall Id: {wall.WallId}");

                        // ������������ ������� ������ �� ����� ������������
                        ballPosition.Position = collisionPoint;

                        // ������ ����������� �������� (������)
                        ballVelocity.Velocity = Reflect(ballVelocity.Velocity, wallNormal);

                        // �� ��������� ����������� ������ � �����, �������� ��� ������� � ������� �� �����
                        ballPosition.Position += ballVelocity.Velocity * ballRadius.Radius / 10;
                    }
                }
            }
        }

        /// <summary>
        /// ���������, ���������� �� ������������ ������ �� ������ �� �������� �������.
        /// </summary>
        private bool WillCollide(Vector2 oldPos, Vector2 velocity, float radius, Vector2 wallStart, Vector2 wallEnd, out Vector2 collisionPoint, out Vector2 wallNormal)
        {
            var newPos = oldPos + velocity;
            collisionPoint = Vector2.Zero;
            wallNormal = Vector2.Zero;

            // ������������ ������ ����������� �������� ������
            var ballMovementVector = newPos - oldPos;

            // ������ ����������� �����
            var wallVector = wallEnd - wallStart;

            // ������ ������� � �����
            wallNormal = new Vector2(-wallVector.Y, wallVector.X);
            wallNormal = Vector2.Normalize(wallNormal);

            // ������ �� ������ ����� �� ������ ������
            var wallToBallStart = oldPos - wallStart;

            // �������� ������� �� ����� �� ������ �� ������� �����
            var projectionLength = Vector2.Dot(wallToBallStart, wallNormal);
            var penetrationDistance = Math.Abs(projectionLength) - radius;

            // ���� ����� ��� ������������ �� ������
            if (penetrationDistance <= 0)
            {
                // ������������� ����� ������������ � ��������� ������� � ������� �� ����� (�� ��������� ����������� � �����)
                collisionPoint = oldPos;
                return true;
            }

            // ���������, ��������� �� ����� ����� �� ����� ����
            var movementProjection = Vector2.Dot(ballMovementVector, wallNormal);

            // ���� ����� �������� � ����� (�������� ����� ��������)
            if (movementProjection < 0)
            {
                // ��������� ����� ������������
                var tCollision = penetrationDistance / -movementProjection;

                if (tCollision >= 0 && tCollision <= 1)
                {
                    // ������� ����� ������������
                    collisionPoint = oldPos + tCollision * ballMovementVector;
                    return true;
                }
            }

            if (AreSegmentsIntersecting(oldPos, newPos, wallStart, wallEnd, out var intersection))
            {
                collisionPoint = intersection - Vector2.Normalize(velocity) * radius * 1.1f;

                return true;
            }

            return false;
        }

        // �������, ����������� ����������� ���� ��������
        static bool AreSegmentsIntersecting(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
        {
            intersection = new Vector2();

            // ������������ ����������� ��������
            Vector2 d1 = p2 - p1;
            Vector2 d2 = p4 - p3;

            // ��������� �����������
            float denominator = d1.X * d2.Y - d1.Y * d2.X;

            // ���� ����������� ����� 0, ������� ����������� ��� ���������
            if (Math.Abs(denominator) < 1e-6)
            {
                return false;
            }

            // ��������� ��������� ��� �������� �����������
            float t = ((p3.X - p1.X) * d2.Y - (p3.Y - p1.Y) * d2.X) / denominator;
            float u = ((p3.X - p1.X) * d1.Y - (p3.Y - p1.Y) * d1.X) / denominator;

            // ���������, ��������� �� ����������� � �������� ��������
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                // ��������� ����� �����������
                intersection = p1 + t * d1;
                return true;
            }

            return false;
        }

        // ������� ���������� ����� ����������� (������������, ��� ����������� ����������)
        static Vector2 GetIntersectionPoint(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            Vector2 d1 = p2 - p1;
            Vector2 d2 = p4 - p3;

            float denominator = d1.X * d2.Y - d1.Y * d2.X;

            // ���������� �������� t, ����� ����� ����� �����������
            float t = ((p3.X - p1.X) * d2.Y - (p3.Y - p1.Y) * d2.X) / denominator;

            return p1 + t * d1;
        }


        /// <summary>
        /// �������� ������ �������� ������������ �������� �������.
        /// </summary>
        private Vector2 Reflect(Vector2 velocity, Vector2 normal)
        {
            // ������� ���������: V' = V - 2 * (V � N) * N
            return velocity - 2 * Vector2.Dot(velocity, normal) * normal;
        }
    }
}
