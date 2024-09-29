using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Core;
using Core.Objects;
using ECS.Components;
using ECS.Systems;
using Leopotam.EcsLite;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace UI
{
    public partial class MainWindow : Window
    {
        private EcsWorld _ecsWorld;
        private EcsSystems _ecsSystems;
        private Ellipse _ballRepresentation;
        private Dictionary<int, Line> _wallRepresentations = new();
        private int _ballEntityId;  // Поле для хранения идентификатора сущности шарика

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            InitializeEcs();
            InitializeScene();
            StartSimulation();
        }

        private void InitializeEcs()
        {
            // Инициализация ECS мира и систем
            _ecsWorld = new EcsWorld();
            _ecsSystems = new EcsSystems(_ecsWorld);
            _ecsSystems.Add(new BallMovementSystem());
            _ecsSystems.Add(new BallCollisionSystem());
            _ecsSystems.Init();
        }

        private void InitializeScene()
        {
            // Инициализация объектов сцены и добавление их в ECS
            var scene = new Scene();
            scene.AddObject(new Wall(1, new Vector2(100, 100), new Vector2(100, 300)));
            scene.AddObject(new Wall(2, new Vector2(100, 300), new Vector2(300, 300)));
            scene.AddObject(new Wall(3, new Vector2(300, 300), new Vector2(300, 100)));
            scene.AddObject(new Wall(4, new Vector2(300, 100), new Vector2(100, 100)));
            scene.AddObject(new Ball(5, new Vector2(150, 150), new Vector2(6, 1), 15f)); // Начальная позиция и радиус

            foreach (var sceneObject in scene.SceneObjects)
            {
                var entity = _ecsWorld.NewEntity();

                switch (sceneObject)
                {
                    case Wall wall:
                        var wallComponent = new WallComponent
                        {
                            Start = wall.Start,
                            End = wall.End,
                            WallId = wall.Id
                        };
                        _ecsWorld.GetPool<WallComponent>().Add(entity) = wallComponent;

                        // Создаем представление стены в UI
                        var wallRepresentation = new Line
                        {
                            StartPoint = new Avalonia.Point(wall.Start.X, wall.Start.Y),
                            EndPoint = new Avalonia.Point(wall.End.X, wall.End.Y),
                            Stroke = Avalonia.Media.Brushes.Black,
                            StrokeThickness = 2
                        };
                        _wallRepresentations[wall.Id] = wallRepresentation;
                        SceneCanvas.Children.Add(wallRepresentation);
                        break;

                    case Ball ball:
                        // Сохраняем идентификатор сущности шарика
                        _ballEntityId = entity;

                        var positionPool = _ecsWorld.GetPool<PositionComponent>();
                        var velocityPool = _ecsWorld.GetPool<VelocityComponent>();
                        var radiusPool = _ecsWorld.GetPool<RadiusComponent>();

                        // Создаем и добавляем компоненты в ECS
                        positionPool.Add(entity) = new PositionComponent { Position = ball.CenterPosition };
                        velocityPool.Add(entity) = new VelocityComponent { Velocity = ball.Velocity };
                        radiusPool.Add(entity) = new RadiusComponent { Radius = ball.Radius };

                        // Создаем представление шарика в UI
                        _ballRepresentation = new Ellipse
                        {
                            Width = ball.Radius * 2,
                            Height = ball.Radius * 2,
                            Fill = Avalonia.Media.Brushes.Blue
                        };
                        Canvas.SetLeft(_ballRepresentation, ball.CenterPosition.X - ball.Radius);
                        Canvas.SetTop(_ballRepresentation, ball.CenterPosition.Y - ball.Radius);
                        SceneCanvas.Children.Add(_ballRepresentation);
                        break;
                }
            }
        }

        private void StartSimulation()
        {
            // Запуск цикла обновления ECS и графики
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            timer.Tick += (sender, e) =>
            {
                // Обновление состояния ECS систем
                _ecsSystems.Run();

                // Получаем актуальный компонент позиции шарика из ECS
                var positionPool = _ecsWorld.GetPool<PositionComponent>();
                if (positionPool.Has(_ballEntityId))
                {
                    var ballPosition = positionPool.Get(_ballEntityId);

                    // Обновление UI элементов на основе измененных компонентов
                    if (_ballRepresentation != null)
                    {
                        Canvas.SetLeft(_ballRepresentation, ballPosition.Position.X - _ballRepresentation.Width / 2);
                        Canvas.SetTop(_ballRepresentation, ballPosition.Position.Y - _ballRepresentation.Height / 2);
                    }
                }
            };
            timer.Start();
        }
    }
}
