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

            // Инициализация пулов компонентов
            _positionPool = world.GetPool<PositionComponent>();
            _velocityPool = world.GetPool<VelocityComponent>();

            // Создаем фильтр, который включает компоненты PositionComponent и VelocityComponent,
            // и исключает компоненты WallComponent (если это необходимо)
            _ballFilter = world.Filter<PositionComponent>()
                               .Inc<VelocityComponent>()
                               .End();
        }

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _ballFilter)
            {
                // Получаем компоненты для текущей сущности
                ref var position = ref _positionPool.Get(entity);
                ref var velocity = ref _velocityPool.Get(entity);

                // Обновляем позицию шарика на основе его скорости
                position.Position += velocity.Velocity;

                Console.WriteLine($"Ball position: {position.Position}");
            }
        }
    }
}
