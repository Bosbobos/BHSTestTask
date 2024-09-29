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
            // Получаем мир ECS
            var world = systems.GetWorld();

            // Инициализация пулов компонентов
            _positionPool = world.GetPool<PositionComponent>();
            _velocityPool = world.GetPool<VelocityComponent>();
            _radiusPool = world.GetPool<RadiusComponent>();
            _wallPool = world.GetPool<WallComponent>();

            // Создаем фильтр для шариков (PositionComponent, VelocityComponent, RadiusComponent)
            _ballFilter = world.Filter<PositionComponent>()
                               .Inc<VelocityComponent>()
                               .Inc<RadiusComponent>()
                               .End();

            // Создаем фильтр для стен (только WallComponent)
            _wallFilter = world.Filter<WallComponent>().End();
        }

        public void Run(IEcsSystems systems)
        {
            var world = systems.GetWorld();

            // Перебираем все сущности, которые соответствуют фильтру шариков
            foreach (var ballEntity in _ballFilter)
            {
                ref var ballPosition = ref _positionPool.Get(ballEntity);
                ref var ballVelocity = ref _velocityPool.Get(ballEntity);
                ref var ballRadius = ref _radiusPool.Get(ballEntity);

                // Перебираем все сущности, которые соответствуют фильтру стен
                foreach (var wallEntity in _wallFilter)
                {
                    ref var wall = ref _wallPool.Get(wallEntity);

                    // Проверка столкновения с учетом "проскакивания"
                    if (WillCollide(ballPosition.Position, ballVelocity.Velocity, ballRadius.Radius, wall.Start, wall.End, out var collisionPoint, out var wallNormal))
                    {
                        // Выводим идентификатор стены
                        Console.WriteLine($"Collision detected with wall Id: {wall.WallId}");

                        // Корректируем позицию шарика до точки столкновения
                        ballPosition.Position = collisionPoint;

                        // Меняем направление скорости (отскок)
                        ballVelocity.Velocity = Reflect(ballVelocity.Velocity, wallNormal);

                        // Во избежание застревания шарика в стене, сдвигаем его немного в сторону от стены
                        ballPosition.Position += ballVelocity.Velocity * ballRadius.Radius / 10;
                    }
                }
            }
        }

        /// <summary>
        /// Проверяет, произойдет ли столкновение шарика со стеной на заданном отрезке.
        /// </summary>
        private bool WillCollide(Vector2 oldPos, Vector2 velocity, float radius, Vector2 wallStart, Vector2 wallEnd, out Vector2 collisionPoint, out Vector2 wallNormal)
        {
            var newPos = oldPos + velocity;
            collisionPoint = Vector2.Zero;
            wallNormal = Vector2.Zero;

            // Рассчитываем вектор направления движения шарика
            var ballMovementVector = newPos - oldPos;

            // Вектор направления стены
            var wallVector = wallEnd - wallStart;

            // Вектор нормали к стене
            wallNormal = new Vector2(-wallVector.Y, wallVector.X);
            wallNormal = Vector2.Normalize(wallNormal);

            // Вектор от начала стены до центра шарика
            var wallToBallStart = oldPos - wallStart;

            // Проекция вектора от стены до шарика на нормаль стены
            var projectionLength = Vector2.Dot(wallToBallStart, wallNormal);
            var penetrationDistance = Math.Abs(projectionLength) - radius;

            // Если шарик уже пересекается со стеной
            if (penetrationDistance <= 0)
            {
                // Устанавливаем точку столкновения с небольшим сдвигом в сторону от стены (во избежание застревания в стене)
                collisionPoint = oldPos;
                return true;
            }

            // Проверяем, пересечет ли шарик стену на своем пути
            var movementProjection = Vector2.Dot(ballMovementVector, wallNormal);

            // Если шарик движется к стене (проверка знака проекции)
            if (movementProjection < 0)
            {
                // Вычисляем время столкновения
                var tCollision = penetrationDistance / -movementProjection;

                if (tCollision >= 0 && tCollision <= 1)
                {
                    // Находим точку столкновения
                    collisionPoint = oldPos + tCollision * ballMovementVector;
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Отражает вектор скорости относительно заданной нормали.
        /// </summary>
        private Vector2 Reflect(Vector2 velocity, Vector2 normal)
        {
            // Формула отражения: V' = V - 2 * (V · N) * N
            return velocity - 2 * Vector2.Dot(velocity, normal) * normal;
        }
    }
}
