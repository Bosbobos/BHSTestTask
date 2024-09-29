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
                        //ballPosition.Position += ballVelocity.Velocity * 0.1f; // Сдвиг на 10% от скорости для избегания застревания
                    }
                }
            }
        }

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

            // Проверка пересечения окружности (края шарика) с линией стены
            if (LineSegmentIntersectsCircle(wallStart, wallEnd, oldPos, radius, out var intersectionPoint))
            {
                collisionPoint = intersectionPoint;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Проверяет, пересекается ли отрезок (wallStart - wallEnd) с окружностью (центр - circleCenter, радиус - circleRadius).
        /// </summary>
        private bool LineSegmentIntersectsCircle(Vector2 wallStart, Vector2 wallEnd, Vector2 circleCenter, float circleRadius, out Vector2 intersectionPoint)
        {
            intersectionPoint = Vector2.Zero;

            // Вектор стены
            var wallVector = wallEnd - wallStart;
            var wallLengthSquared = wallVector.LengthSquared();

            // Вектор от начала стены до центра окружности
            var toCenterVector = circleCenter - wallStart;

            // Проекция центра окружности на направляющий вектор стены
            var projection = Vector2.Dot(toCenterVector, wallVector) / wallLengthSquared;
            projection = Math.Clamp(projection, 0, 1); // Ограничиваем проекцию на отрезке [0, 1]

            // Ближайшая точка на стене к центру окружности
            var closestPoint = wallStart + projection * wallVector;

            // Проверяем расстояние от ближайшей точки на стене до центра окружности
            var distanceSquared = (closestPoint - circleCenter).LengthSquared();

            if (distanceSquared <= circleRadius * circleRadius)
            {
                intersectionPoint = closestPoint;
                return true;
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
