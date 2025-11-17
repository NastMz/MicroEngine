using MicroEngine.Core.Logging;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Core.Tests.Scenes;

/// <summary>
/// Unit tests for <see cref="SceneManager"/>.
/// </summary>
public class SceneManagerTests
{
    private sealed class TestScene : Scene
    {
        public int LoadCallCount { get; private set; }
        public int UnloadCallCount { get; private set; }
        public int FixedUpdateCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public int RenderCallCount { get; private set; }

        public TestScene(string name) : base(name)
        {
        }

        public override void OnLoad()
        {
            base.OnLoad();
            LoadCallCount++;
        }

        public override void OnUnload()
        {
            base.OnUnload();
            UnloadCallCount++;
        }

        public override void OnFixedUpdate(float fixedDeltaTime)
        {
            FixedUpdateCallCount++;
        }

        public override void OnUpdate(float deltaTime)
        {
            UpdateCallCount++;
        }

        public override void OnRender()
        {
            RenderCallCount++;
        }
    }

    private static SceneManager CreateSceneManager()
    {
        var logger = new ConsoleLogger(LogLevel.Error);
        return new SceneManager(logger);
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        var logger = new ConsoleLogger();
        var manager = new SceneManager(logger);

        Assert.NotNull(manager);
        Assert.Null(manager.CurrentScene);
        Assert.False(manager.IsTransitioning);
    }

    [Fact]
    public void RegisterScene_AddsSceneSuccessfully()
    {
        var manager = CreateSceneManager();
        var scene = new TestScene("TestScene");

        manager.RegisterScene(scene);

        // Scene is registered but not loaded
        Assert.Null(manager.CurrentScene);
    }

    [Fact]
    public void RegisterScene_ThrowsWhenSceneIsNull()
    {
        var manager = CreateSceneManager();

        Assert.Throws<ArgumentNullException>(() => manager.RegisterScene(null!));
    }

    [Fact]
    public void RegisterScene_ThrowsWhenDuplicateSceneName()
    {
        var manager = CreateSceneManager();
        var scene1 = new TestScene("TestScene");
        var scene2 = new TestScene("TestScene");

        manager.RegisterScene(scene1);

        Assert.Throws<InvalidOperationException>(() => manager.RegisterScene(scene2));
    }

    [Fact]
    public void LoadScene_ActivatesScene()
    {
        var manager = CreateSceneManager();
        manager.Initialize();

        var scene = new TestScene("TestScene");
        manager.RegisterScene(scene);

        manager.LoadScene("TestScene");

        // Transition is pending
        Assert.True(manager.IsTransitioning);

        // Process transition
        manager.FixedUpdate(0.016f);

        Assert.False(manager.IsTransitioning);
        Assert.Equal(scene, manager.CurrentScene);
        Assert.Equal(1, scene.LoadCallCount);
        Assert.True(scene.IsActive);
    }

    [Fact]
    public void LoadScene_ThrowsWhenSceneNotRegistered()
    {
        var manager = CreateSceneManager();
        manager.Initialize();

        Assert.Throws<InvalidOperationException>(() => manager.LoadScene("NonExistent"));
    }

    [Fact]
    public void LoadScene_ThrowsWhenTransitionInProgress()
    {
        var manager = CreateSceneManager();
        manager.Initialize();

        var scene1 = new TestScene("Scene1");
        var scene2 = new TestScene("Scene2");

        manager.RegisterScene(scene1);
        manager.RegisterScene(scene2);

        manager.LoadScene("Scene1");

        Assert.Throws<InvalidOperationException>(() => manager.LoadScene("Scene2"));
    }

    [Fact]
    public void SceneTransition_UnloadsPreviousScene()
    {
        var manager = CreateSceneManager();
        manager.Initialize();

        var scene1 = new TestScene("Scene1");
        var scene2 = new TestScene("Scene2");

        manager.RegisterScene(scene1);
        manager.RegisterScene(scene2);

        // Load first scene
        manager.LoadScene("Scene1");
        manager.FixedUpdate(0.016f);

        Assert.Equal(1, scene1.LoadCallCount);
        Assert.True(scene1.IsActive);

        // Load second scene
        manager.LoadScene("Scene2");
        manager.FixedUpdate(0.016f);

        Assert.Equal(1, scene1.UnloadCallCount);
        Assert.False(scene1.IsActive);
        Assert.Equal(1, scene2.LoadCallCount);
        Assert.True(scene2.IsActive);
        Assert.Equal(scene2, manager.CurrentScene);
    }

    [Fact]
    public void FixedUpdate_CallsSceneFixedUpdate()
    {
        var manager = CreateSceneManager();
        manager.Initialize();

        var scene = new TestScene("TestScene");
        manager.RegisterScene(scene);
        manager.LoadScene("TestScene");

        manager.FixedUpdate(0.016f);
        manager.FixedUpdate(0.016f);

        Assert.Equal(2, scene.FixedUpdateCallCount);
    }

    [Fact]
    public void Update_CallsSceneUpdate()
    {
        var manager = CreateSceneManager();
        manager.Initialize();

        var scene = new TestScene("TestScene");
        manager.RegisterScene(scene);
        manager.LoadScene("TestScene");

        manager.FixedUpdate(0.016f);
        manager.Update(0.016f);
        manager.Update(0.016f);

        Assert.Equal(2, scene.UpdateCallCount);
    }

    [Fact]
    public void Render_CallsSceneRender()
    {
        var manager = CreateSceneManager();
        manager.Initialize();

        var scene = new TestScene("TestScene");
        manager.RegisterScene(scene);
        manager.LoadScene("TestScene");

        manager.FixedUpdate(0.016f);
        manager.Render();
        manager.Render();

        Assert.Equal(2, scene.RenderCallCount);
    }

    [Fact]
    public void UnregisterScene_RemovesScene()
    {
        var manager = CreateSceneManager();
        manager.Initialize();

        var scene = new TestScene("TestScene");
        manager.RegisterScene(scene);

        bool result = manager.UnregisterScene("TestScene");

        Assert.True(result);
    }

    [Fact]
    public void UnregisterScene_ReturnsFalseForActiveScene()
    {
        var manager = CreateSceneManager();
        manager.Initialize();

        var scene = new TestScene("TestScene");
        manager.RegisterScene(scene);
        manager.LoadScene("TestScene");
        manager.FixedUpdate(0.016f);

        bool result = manager.UnregisterScene("TestScene");

        Assert.False(result);
    }

    [Fact]
    public void Shutdown_UnloadsCurrentScene()
    {
        var manager = CreateSceneManager();
        manager.Initialize();

        var scene = new TestScene("TestScene");
        manager.RegisterScene(scene);
        manager.LoadScene("TestScene");
        manager.FixedUpdate(0.016f);

        manager.Shutdown();

        Assert.Equal(1, scene.UnloadCallCount);
        Assert.Null(manager.CurrentScene);
    }
}
