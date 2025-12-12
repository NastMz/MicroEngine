using MicroEngine.Core.Audio;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Scenes;
using MicroEngine.Core.State;
using MicroEngine.Core.Time;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Resources;
using Moq;

namespace MicroEngine.Core.Tests.Scenes;

/// <summary>
/// Unit tests for stack-based <see cref="SceneManager"/>.
/// </summary>
public class SceneManagerTests
{
    private sealed class TestScene : Scene
    {
        public int LoadCallCount { get; private set; }
        public int LoadWithParametersCallCount { get; private set; }
        public SceneParameters? ReceivedParameters { get; private set; }
        public int UnloadCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public int RenderCallCount { get; private set; }

        public TestScene(string name) : base(name)
        {
        }

        public override void OnLoad(SceneContext context)
        {
            base.OnLoad(context);
            LoadCallCount++;
        }

        public override void OnLoad(SceneContext context, SceneParameters parameters)
        {
            base.OnLoad(context, parameters);
            LoadWithParametersCallCount++;
            ReceivedParameters = parameters;
        }

        public override void OnUnload()
        {
            base.OnUnload();
            UnloadCallCount++;
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

    private static SceneContext CreateMockSceneContext()
    {
        var mockRenderer = new Mock<IRenderer2D>();
        var mockWindow = new Mock<IWindow>();
        var mockInputBackend = new Mock<IInputBackend>();
        var mockTimeService = new Mock<ITimeService>();
        var logger = new ConsoleLogger(LogLevel.Error);
        
        // ResourceCache is sealed, so create a real instance with a null loader
        // Tests don't actually load textures, so this is safe
        var mockTextureLoader = new Mock<IResourceLoader<ITexture>>();
        var textureCache = new ResourceCache<ITexture>(mockTextureLoader.Object, logger);
        
        // Create mock audio cache
        var mockAudioLoader = new Mock<IResourceLoader<IAudioClip>>();
        var audioCache = new ResourceCache<IAudioClip>(mockAudioLoader.Object, logger);
        
        // Create mock audio backends
        var mockAudioDevice = new Mock<IAudioDevice>();
        var mockSoundPlayer = new Mock<ISoundPlayer>();
        var mockMusicPlayer = new Mock<IMusicPlayer>();
        
        // Create real GameState for tests
        var gameState = new GameState();
        
        // Create mock service container
        var mockServiceContainer = new Mock<DependencyInjection.IServiceContainer>();
        
        // Create mock navigator
        var mockNavigator = new Mock<ISceneNavigator>();

        return new SceneContext(
            mockWindow.Object,
            mockRenderer.Object,
            mockInputBackend.Object,
            mockTimeService.Object,
            logger,
            textureCache,
            audioCache,
            mockAudioDevice.Object,
            mockSoundPlayer.Object,
            mockMusicPlayer.Object,
            gameState,
            mockServiceContainer.Object,
            mockNavigator.Object
        );
    }

    private static SceneManager CreateSceneManager()
    {
        var manager = new SceneManager(transitionEffect: null);
        var context = CreateMockSceneContext();
        manager.Initialize(context);
        return manager;
    }

    [Fact]
    public void Constructor_InitializesWithEmptyStack()
    {
        var manager = new SceneManager(transitionEffect: null);
        var context = CreateMockSceneContext();
        manager.Initialize(context);

        Assert.NotNull(manager);
        Assert.Null(manager.CurrentScene);
        Assert.Equal(0, manager.SceneCount);
    }

    [Fact]
    public void PushScene_ThrowsWhenSceneIsNull()
    {
        var manager = CreateSceneManager();

        Assert.Throws<ArgumentNullException>(() => manager.PushScene(null!));
    }

    [Fact]
    public void PushScene_AddsToStackAndCallsOnLoad()
    {
        var manager = CreateSceneManager();

        var scene = new TestScene("TestScene");
        manager.PushScene(scene);

        // Transition is pending, process it
        manager.Update(0.016f);

        Assert.Equal(scene, manager.CurrentScene);
        Assert.Equal(1, manager.SceneCount);
        Assert.Equal(1, scene.LoadCallCount);
        Assert.True(scene.IsActive);
    }

    [Fact]
    public void PushScene_MultipleScenesStackCorrectly()
    {
        var manager = CreateSceneManager();

        var scene1 = new TestScene("Scene1");
        var scene2 = new TestScene("Scene2");
        var scene3 = new TestScene("Scene3");

        manager.PushScene(scene1);
        manager.Update(0.016f);

        manager.PushScene(scene2);
        manager.Update(0.016f);

        manager.PushScene(scene3);
        manager.Update(0.016f);

        Assert.Equal(scene3, manager.CurrentScene);
        Assert.Equal(3, manager.SceneCount);
        Assert.Equal(1, scene1.LoadCallCount);
        Assert.Equal(1, scene2.LoadCallCount);
        Assert.Equal(1, scene3.LoadCallCount);
    }

    [Fact]
    public void PopScene_RemovesFromStackAndCallsOnUnload()
    {
        var manager = CreateSceneManager();

        var scene1 = new TestScene("Scene1");
        var scene2 = new TestScene("Scene2");

        manager.PushScene(scene1);
        manager.Update(0.016f);

        manager.PushScene(scene2);
        manager.Update(0.016f);

        Assert.Equal(2, manager.SceneCount);
        Assert.Equal(scene2, manager.CurrentScene);

        manager.PopScene();
        manager.Update(0.016f);

        Assert.Equal(1, manager.SceneCount);
        Assert.Equal(scene1, manager.CurrentScene);
        Assert.Equal(1, scene2.UnloadCallCount);
        Assert.False(scene2.IsActive);
    }

    [Fact]
    public void PopScene_DoesNothingWhenOneSceneRemains()
    {
        var manager = CreateSceneManager();

        var scene = new TestScene("TestScene");
        manager.PushScene(scene);
        manager.Update(0.016f);

        Assert.Equal(1, manager.SceneCount);

        manager.PopScene();
        manager.Update(0.016f);

        // Should still have the scene (cannot pop last scene)
        Assert.Equal(1, manager.SceneCount);
        Assert.Equal(scene, manager.CurrentScene);
        Assert.Equal(0, scene.UnloadCallCount);
    }

    [Fact]
    public void PopScene_DoesNothingWhenStackIsEmpty()
    {
        var manager = CreateSceneManager();

        Assert.Equal(0, manager.SceneCount);

        manager.PopScene();
        manager.Update(0.016f);

        Assert.Equal(0, manager.SceneCount);
        Assert.Null(manager.CurrentScene);
    }

    [Fact]
    public void ReplaceScene_ThrowsWhenSceneIsNull()
    {
        var manager = CreateSceneManager();

        Assert.Throws<ArgumentNullException>(() => manager.ReplaceScene(null!));
    }

    [Fact]
    public void ReplaceScene_SwapsTopSceneWithOnLoadUnload()
    {
        var manager = CreateSceneManager();

        var scene1 = new TestScene("Scene1");
        var scene2 = new TestScene("Scene2");

        manager.PushScene(scene1);
        manager.Update(0.016f);

        Assert.Equal(1, manager.SceneCount);
        Assert.Equal(scene1, manager.CurrentScene);

        manager.ReplaceScene(scene2);
        manager.Update(0.016f);

        Assert.Equal(1, manager.SceneCount);
        Assert.Equal(scene2, manager.CurrentScene);
        Assert.Equal(1, scene1.UnloadCallCount);
        Assert.Equal(1, scene2.LoadCallCount);
        Assert.False(scene1.IsActive);
        Assert.True(scene2.IsActive);
    }

    [Fact]
    public void ReplaceScene_WorksOnEmptyStack()
    {
        var manager = CreateSceneManager();

        var scene = new TestScene("TestScene");

        manager.ReplaceScene(scene);
        manager.Update(0.016f);

        Assert.Equal(1, manager.SceneCount);
        Assert.Equal(scene, manager.CurrentScene);
        Assert.Equal(1, scene.LoadCallCount);
    }

    [Fact]
    public void Update_ProcessesPendingTransitionsAndCallsSceneUpdate()
    {
        var manager = CreateSceneManager();

        var scene = new TestScene("TestScene");
        manager.PushScene(scene);

        // First Update processes the pending push transition
        manager.Update(0.016f);

        Assert.Equal(1, scene.LoadCallCount);
        Assert.Equal(1, scene.UpdateCallCount);

        // Subsequent Updates just call scene update
        manager.Update(0.016f);
        manager.Update(0.016f);

        Assert.Equal(3, scene.UpdateCallCount);
    }

    [Fact]
    public void Render_CallsSceneRender()
    {
        var manager = CreateSceneManager();

        var scene = new TestScene("TestScene");
        manager.PushScene(scene);
        manager.Update(0.016f);

        manager.Render();
        manager.Render();

        Assert.Equal(2, scene.RenderCallCount);
    }

    [Fact]
    public void Shutdown_UnloadsAllScenesInStack()
    {
        var manager = CreateSceneManager();

        var scene1 = new TestScene("Scene1");
        var scene2 = new TestScene("Scene2");
        var scene3 = new TestScene("Scene3");

        manager.PushScene(scene1);
        manager.Update(0.016f);

        manager.PushScene(scene2);
        manager.Update(0.016f);

        manager.PushScene(scene3);
        manager.Update(0.016f);

        Assert.Equal(3, manager.SceneCount);

        manager.Shutdown();

        Assert.Equal(1, scene1.UnloadCallCount);
        Assert.Equal(1, scene2.UnloadCallCount);
        Assert.Equal(1, scene3.UnloadCallCount);
        Assert.Null(manager.CurrentScene);
        Assert.Equal(0, manager.SceneCount);
    }

    [Fact]
    public void PendingTransitions_OnlyLastIsProcessed()
    {
        var manager = CreateSceneManager();

        var scene1 = new TestScene("Scene1");
        var scene2 = new TestScene("Scene2");

        // Queue multiple transitions before processing
        manager.PushScene(scene1);
        manager.PushScene(scene2);

        // Only last pending transition is processed (Push scene2)
        manager.Update(0.016f);

        Assert.Equal(scene2, manager.CurrentScene);
        Assert.Equal(1, manager.SceneCount);
        Assert.Equal(0, scene1.LoadCallCount); // scene1 never loaded
        Assert.Equal(1, scene2.LoadCallCount);
    }

    [Fact]
    public void PushScene_WithParameters_CallsOnLoadWithParameters()
    {
        // Arrange
        var manager = CreateSceneManager();
        var scene = new TestScene("Scene1");
        var parameters = SceneParameters.Create()
            .Add("level", 5)
            .Add("score", 1000)
            .Build();

        // Act
        manager.PushScene(scene, parameters);
        manager.Update(0.016f);

        // Assert
        Assert.Equal(scene, manager.CurrentScene);
        Assert.Equal(1, scene.LoadWithParametersCallCount);
        Assert.Equal(1, scene.LoadCallCount); // Base OnLoad also called
        Assert.NotNull(scene.ReceivedParameters);
        Assert.Equal(5, scene.ReceivedParameters.Get<int>("level"));
        Assert.Equal(1000, scene.ReceivedParameters.Get<int>("score"));
    }

    [Fact]
    public void PushScene_WithNullParameters_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = CreateSceneManager();
        var scene = new TestScene("Scene1");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => manager.PushScene(scene, null!));
    }

    [Fact]
    public void ReplaceScene_WithParameters_CallsOnLoadWithParameters()
    {
        // Arrange
        var manager = CreateSceneManager();
        var scene1 = new TestScene("Scene1");
        var scene2 = new TestScene("Scene2");
        var parameters = SceneParameters.Create()
            .Add("difficulty", "Hard")
            .Build();

        // Act
        manager.ReplaceScene(scene1);
        manager.Update(0.016f);
        
        manager.ReplaceScene(scene2, parameters);
        manager.Update(0.016f);

        // Assert
        Assert.Equal(scene2, manager.CurrentScene);
        Assert.Equal(1, scene2.LoadWithParametersCallCount);
        Assert.NotNull(scene2.ReceivedParameters);
        Assert.Equal("Hard", scene2.ReceivedParameters.Get<string>("difficulty"));
        Assert.Equal(1, scene1.UnloadCallCount);
    }

    [Fact]
    public void ReplaceScene_WithNullParameters_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = CreateSceneManager();
        var scene = new TestScene("Scene1");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => manager.ReplaceScene(scene, null!));
    }

    [Fact]
    public void PushScene_WithoutParameters_DoesNotCallParameterizedOnLoad()
    {
        // Arrange
        var manager = CreateSceneManager();
        var scene = new TestScene("Scene1");

        // Act
        manager.PushScene(scene);
        manager.Update(0.016f);

        // Assert
        Assert.Equal(scene, manager.CurrentScene);
        Assert.Equal(1, scene.LoadCallCount);
        Assert.Equal(0, scene.LoadWithParametersCallCount);
        Assert.Null(scene.ReceivedParameters);
    }
}
