using Core.Objects;

namespace Core;

public class Scene
{
    public List<SceneObject2D> SceneObjects { get; } = new List<SceneObject2D>();

    public void AddObject(SceneObject2D obj)
    {
        SceneObjects.Add(obj);
    }
}
