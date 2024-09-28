using Core;
using Core.Objects;
using ECS.Components;
using ECS.Systems;
using Leopotam.EcsLite;
using System.Numerics;

namespace BHSTestTask
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // 1. Инициализация сцены
            Scene scene = new Scene();

            // Создаем 4 стены
            scene.AddObject(new Wall(1, new Vector2(0, 0), new Vector2(0, 10)));
            scene.AddObject(new Wall(2, new Vector2(0, 10), new Vector2(10, 10)));
            scene.AddObject(new Wall(3, new Vector2(10, 10), new Vector2(10, 0)));
            scene.AddObject(new Wall(4, new Vector2(10, 0), new Vector2(0, 0)));

            // Добавляем шарик в центр коробки
            scene.AddObject(new Ball(5, new Vector2(5, 5), new Vector2(0.1f, 0.1f), 0.5f));

            // 2. Инициализация ECS мира
            var ecsWorld = new EcsWorld();
            var ecsSystems = new EcsSystems(ecsWorld);

            // Создаем пулы для компонентов
            var wallPool = ecsWorld.GetPool<WallComponent>();
            var positionPool = ecsWorld.GetPool<PositionComponent>();
            var velocityPool = ecsWorld.GetPool<VelocityComponent>();
            var radiusPool = ecsWorld.GetPool<RadiusComponent>();

            // Добавляем объекты сцены в ECS как сущности
            foreach (var sceneObject in scene.SceneObjects)
            {
                var entity = ecsWorld.NewEntity();

                switch (sceneObject)
                {
                    case Wall wall:
                        wallPool.Add(entity) = new WallComponent
                        {
                            Start = wall.Start,
                            End = wall.End,
                            WallId = wall.Id
                        };
                        break;

                    case Ball ball:
                        positionPool.Add(entity) = new PositionComponent { Position = ball.CenterPosition };
                        velocityPool.Add(entity) = new VelocityComponent { Velocity = ball.Velocity };
                        radiusPool.Add(entity) = new RadiusComponent { Radius = ball.Radius };
                        break;
                }
            }

            // 3. Добавляем системы
            ecsSystems.Add(new BallMovementSystem());
            ecsSystems.Add(new BallCollisionSystem());

            // 4. Инициализация и выполнение
            ecsSystems.Init();

            // 5. Основной цикл приложения
            while (true)
            {
                ecsSystems.Run();
                System.Threading.Thread.Sleep(100); // Задержка для видимого перемещения
            }

            // 6. Очистка ресурсов
            ecsSystems.Destroy();
            ecsWorld.Destroy();
        }
    }
}
