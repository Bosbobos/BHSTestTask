using ECS.Components;
using Leopotam.EcsLite;
using System;

namespace ECS.Systems
{
    public class BallMovementSystem : IEcsInitSystem, IEcsRunSystem
    {
        private EcsFilter _ballFilter;
        private EcsPool<PositionComponent> _positionPool;
        private EcsPool<VelocityComponent> _velocityPool;

        public void Init(IEcsSystems systems)
        {
            var world = systems.GetWorld();

            // ������������� ����� �����������
            _positionPool = world.GetPool<PositionComponent>();
            _velocityPool = world.GetPool<VelocityComponent>();

            // ������� ������, ������� �������� ���������� PositionComponent � VelocityComponent,
            // � ��������� ���������� WallComponent (���� ��� ����������)
            _ballFilter = world.Filter<PositionComponent>()
                               .Inc<VelocityComponent>()
                               .End();
        }

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _ballFilter)
            {
                // �������� ���������� ��� ������� ��������
                ref var position = ref _positionPool.Get(entity);
                ref var velocity = ref _velocityPool.Get(entity);

                // ��������� ������� ������ �� ������ ��� ��������
                position.Position += velocity.Velocity;

                Console.WriteLine($"Ball position: {position.Position}");
            }
        }
    }
}
