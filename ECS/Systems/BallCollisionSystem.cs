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

                    // ����������� ������������ � ������������ ��� �������������� ������
                    if (IsColliding(ballPosition.Position, ballRadius.Radius, wall.Start, wall.End))
                    {
                        // ������� ������������� �����
                        Console.WriteLine($"Collision detected with wall Id: {wall.WallId}");

                        // ������ ����������� �������� (������)
                        ballVelocity.Velocity = Reflect(ballVelocity.Velocity, wall.Start, wall.End);
                    }
                }
            }
        }

        private bool IsColliding(Vector2 ballPos, float ballRadius, Vector2 wallStart, Vector2 wallEnd)
        {
            // �������� ������������ (��� ���������, ������ � �����)
            if (wallStart.X == wallEnd.X) // ������������ �����
            {
                return Math.Abs(ballPos.X - wallStart.X) <= ballRadius;
            }
            else if (wallStart.Y == wallEnd.Y) // �������������� �����
            {
                return Math.Abs(ballPos.Y - wallStart.Y) <= ballRadius;
            }
            return false;
        }

        private Vector2 Reflect(Vector2 velocity, Vector2 wallStart, Vector2 wallEnd)
        {
            // �������� ������ ��������
            if (wallStart.X == wallEnd.X) // ������������ �����
            {
                return new Vector2(-velocity.X, velocity.Y);
            }
            else if (wallStart.Y == wallEnd.Y) // �������������� �����
            {
                return new Vector2(velocity.X, -velocity.Y);
            }
            return velocity;
        }
    }
}
