using MicroEngine.Core.Scenes;

namespace MicroEngine.Core.Tests.Scenes;

public sealed class SceneCacheTests
{
    private sealed class TestScene : IScene
    {
        public string Name => "TestScene";
        public bool IsActive => true;
        public bool OnLoadCalled { get; private set; }
        public bool OnUnloadCalled { get; private set; }

        public void OnLoad(SceneContext context)
        {
            OnLoadCalled = true;
        }

        public void OnLoad(SceneContext context, SceneParameters parameters)
        {
            OnLoadCalled = true;
        }

        public void OnUnload()
        {
            OnUnloadCalled = true;
        }

        public void OnFixedUpdate(float fixedDeltaTime)
        {
        }

        public void OnUpdate(float deltaTime)
        {
        }

        public void OnRender()
        {
        }
    }

    [Fact]
    public void Constructor_WithValidSize_CreatesCache()
    {
        // Act
        var cache = new SceneCache(5);

        // Assert
        Assert.Equal(5, cache.MaxCacheSize);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Constructor_WithInvalidSize_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SceneCache(0));
        Assert.Throws<ArgumentException>(() => new SceneCache(-1));
    }

    [Fact]
    public void Constructor_WithoutParameter_UsesDefaultSize()
    {
        // Act
        var cache = new SceneCache();

        // Assert
        Assert.Equal(10, cache.MaxCacheSize);
    }

    [Fact]
    public void GetOrCreate_NewScene_CreatesAndCaches()
    {
        // Arrange
        var cache = new SceneCache();
        var factoryCalled = false;

        // Act
        var scene = cache.GetOrCreate("test", () =>
        {
            factoryCalled = true;
            return new TestScene();
        });

        // Assert
        Assert.NotNull(scene);
        Assert.True(factoryCalled);
        Assert.Equal(1, cache.Count);
        Assert.True(cache.Contains("test"));
    }

    [Fact]
    public void GetOrCreate_ExistingScene_ReturnsCache()
    {
        // Arrange
        var cache = new SceneCache();
        var factoryCallCount = 0;

        var scene1 = cache.GetOrCreate("test", () =>
        {
            factoryCallCount++;
            return new TestScene();
        });

        // Act
        var scene2 = cache.GetOrCreate("test", () =>
        {
            factoryCallCount++;
            return new TestScene();
        });

        // Assert
        Assert.Same(scene1, scene2);
        Assert.Equal(1, factoryCallCount);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void GetOrCreate_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        var cache = new SceneCache();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            cache.GetOrCreate<TestScene>(null!, () => new TestScene()));
        Assert.Throws<ArgumentException>(() =>
            cache.GetOrCreate<TestScene>("", () => new TestScene()));
        Assert.Throws<ArgumentException>(() =>
            cache.GetOrCreate<TestScene>("   ", () => new TestScene()));
    }

    [Fact]
    public void GetOrCreate_WithNullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new SceneCache();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            cache.GetOrCreate<TestScene>("test", null!));
    }

    [Fact]
    public void GetOrCreate_FactoryReturnsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var cache = new SceneCache();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            cache.GetOrCreate<TestScene>("test", () => null!));
        
        Assert.Contains("null", exception.Message);
        Assert.Contains("test", exception.Message);
    }

    [Fact]
    public void Contains_ExistingScene_ReturnsTrue()
    {
        // Arrange
        var cache = new SceneCache();
        cache.GetOrCreate("test", () => new TestScene());

        // Act
        var result = cache.Contains("test");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Contains_NonExistingScene_ReturnsFalse()
    {
        // Arrange
        var cache = new SceneCache();

        // Act
        var result = cache.Contains("missing");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Contains_WithNullKey_ReturnsFalse()
    {
        // Arrange
        var cache = new SceneCache();

        // Act & Assert
        Assert.False(cache.Contains(null!));
        Assert.False(cache.Contains(""));
        Assert.False(cache.Contains("   "));
    }

    [Fact]
    public void Remove_ExistingScene_RemovesAndCallsOnUnload()
    {
        // Arrange
        var cache = new SceneCache();
        var scene = cache.GetOrCreate("test", () => new TestScene());

        // Act
        var result = cache.Remove("test");

        // Assert
        Assert.True(result);
        Assert.Equal(0, cache.Count);
        Assert.False(cache.Contains("test"));
        Assert.True(scene.OnUnloadCalled);
    }

    [Fact]
    public void Remove_NonExistingScene_ReturnsFalse()
    {
        // Arrange
        var cache = new SceneCache();

        // Act
        var result = cache.Remove("missing");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Remove_WithNullKey_ReturnsFalse()
    {
        // Arrange
        var cache = new SceneCache();

        // Act & Assert
        Assert.False(cache.Remove(null!));
        Assert.False(cache.Remove(""));
        Assert.False(cache.Remove("   "));
    }

    [Fact]
    public void Clear_RemovesAllScenes()
    {
        // Arrange
        var cache = new SceneCache();
        var scene1 = cache.GetOrCreate("test1", () => new TestScene());
        var scene2 = cache.GetOrCreate("test2", () => new TestScene());
        var scene3 = cache.GetOrCreate("test3", () => new TestScene());

        // Act
        cache.Clear();

        // Assert
        Assert.Equal(0, cache.Count);
        Assert.True(scene1.OnUnloadCalled);
        Assert.True(scene2.OnUnloadCalled);
        Assert.True(scene3.OnUnloadCalled);
    }

    [Fact]
    public void GetCachedKeys_ReturnsAllKeys()
    {
        // Arrange
        var cache = new SceneCache();
        cache.GetOrCreate("scene1", () => new TestScene());
        cache.GetOrCreate("scene2", () => new TestScene());
        cache.GetOrCreate("scene3", () => new TestScene());

        // Act
        var keys = cache.GetCachedKeys().ToList();

        // Assert
        Assert.Equal(3, keys.Count);
        Assert.Contains("scene1", keys);
        Assert.Contains("scene2", keys);
        Assert.Contains("scene3", keys);
    }

    [Fact]
    public void GetCachedKeys_EmptyCache_ReturnsEmpty()
    {
        // Arrange
        var cache = new SceneCache();

        // Act
        var keys = cache.GetCachedKeys();

        // Assert
        Assert.Empty(keys);
    }

    [Fact]
    public void Preload_NewScene_CachesWithoutReturning()
    {
        // Arrange
        var cache = new SceneCache();
        var factoryCalled = false;

        // Act
        cache.Preload("test", () =>
        {
            factoryCalled = true;
            return new TestScene();
        });

        // Assert
        Assert.True(factoryCalled);
        Assert.Equal(1, cache.Count);
        Assert.True(cache.Contains("test"));
    }

    [Fact]
    public void Preload_ExistingScene_DoesNotRecreate()
    {
        // Arrange
        var cache = new SceneCache();
        var factoryCallCount = 0;

        cache.GetOrCreate("test", () =>
        {
            factoryCallCount++;
            return new TestScene();
        });

        // Act
        cache.Preload("test", () =>
        {
            factoryCallCount++;
            return new TestScene();
        });

        // Assert
        Assert.Equal(1, factoryCallCount);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void Preload_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        var cache = new SceneCache();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            cache.Preload<TestScene>(null!, () => new TestScene()));
        Assert.Throws<ArgumentException>(() =>
            cache.Preload<TestScene>("", () => new TestScene()));
    }

    [Fact]
    public void Preload_WithNullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new SceneCache();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            cache.Preload<TestScene>("test", null!));
    }

    [Fact]
    public void TryGet_ExistingScene_ReturnsTrue()
    {
        // Arrange
        var cache = new SceneCache();
        var original = cache.GetOrCreate("test", () => new TestScene());

        // Act
        var result = cache.TryGet<TestScene>("test", out var scene);

        // Assert
        Assert.True(result);
        Assert.Same(original, scene);
    }

    [Fact]
    public void TryGet_NonExistingScene_ReturnsFalse()
    {
        // Arrange
        var cache = new SceneCache();

        // Act
        var result = cache.TryGet<TestScene>("missing", out var scene);

        // Assert
        Assert.False(result);
        Assert.Null(scene);
    }

    [Fact]
    public void TryGet_WithNullKey_ReturnsFalse()
    {
        // Arrange
        var cache = new SceneCache();

        // Act & Assert
        Assert.False(cache.TryGet<TestScene>(null!, out _));
        Assert.False(cache.TryGet<TestScene>("", out _));
        Assert.False(cache.TryGet<TestScene>("   ", out _));
    }

    [Fact]
    public void Cache_ExceedsMaxSize_EvictsLRU()
    {
        // Arrange
        var cache = new SceneCache(3);
        var scene1 = cache.GetOrCreate("scene1", () => new TestScene());
        cache.GetOrCreate("scene2", () => new TestScene());
        cache.GetOrCreate("scene3", () => new TestScene());

        // scene1 is now the least recently used

        // Act - Add a 4th scene, should evict scene1
        cache.GetOrCreate("scene4", () => new TestScene());

        // Assert
        Assert.Equal(3, cache.Count);
        Assert.False(cache.Contains("scene1"));
        Assert.True(cache.Contains("scene2"));
        Assert.True(cache.Contains("scene3"));
        Assert.True(cache.Contains("scene4"));
        Assert.True(scene1.OnUnloadCalled);
    }

    [Fact]
    public void Cache_LRUTracking_UpdatesOnAccess()
    {
        // Arrange
        var cache = new SceneCache(3);
        cache.GetOrCreate("scene1", () => new TestScene());
        cache.GetOrCreate("scene2", () => new TestScene());
        cache.GetOrCreate("scene3", () => new TestScene());

        // Act - Access scene1 to update its LRU timestamp
        cache.GetOrCreate("scene1", () => new TestScene());

        // Now scene2 is LRU, add scene4 to trigger eviction
        cache.GetOrCreate("scene4", () => new TestScene());

        // Assert - scene2 should be evicted, not scene1
        Assert.Equal(3, cache.Count);
        Assert.True(cache.Contains("scene1"));
        Assert.False(cache.Contains("scene2"));
        Assert.True(cache.Contains("scene3"));
        Assert.True(cache.Contains("scene4"));
    }

    [Fact]
    public void Preload_ExceedsMaxSize_EvictsLRU()
    {
        // Arrange
        var cache = new SceneCache(2);
        var scene1 = cache.GetOrCreate("scene1", () => new TestScene());
        cache.GetOrCreate("scene2", () => new TestScene());

        // Act - Preload a 3rd scene
        cache.Preload("scene3", () => new TestScene());

        // Assert
        Assert.Equal(2, cache.Count);
        Assert.False(cache.Contains("scene1"));
        Assert.True(cache.Contains("scene2"));
        Assert.True(cache.Contains("scene3"));
        Assert.True(scene1.OnUnloadCalled);
    }

    [Fact]
    public async Task Cache_ThreadSafety_ConcurrentAccess()
    {
        // Arrange
        var cache = new SceneCache(100);
        var tasks = new List<Task>();

        // Act - Create 100 scenes concurrently
        for (var i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                cache.GetOrCreate($"scene{index}", () => new TestScene());
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(100, cache.Count);
        for (var i = 0; i < 100; i++)
        {
            Assert.True(cache.Contains($"scene{i}"));
        }
    }

    [Fact]
    public void Cache_UsagePattern_SimulatesGameSceneFlow()
    {
        // Arrange - Simulate a game with limited cache
        var cache = new SceneCache(5);

        // Act - Simulate game flow: MainMenu → Level1 → Level2 → Level1 (revisit) → GameOver
        cache.GetOrCreate("MainMenu", () => new TestScene());
        var level1 = cache.GetOrCreate("Level1", () => new TestScene());
        cache.GetOrCreate("Level2", () => new TestScene());
        
        // Revisit Level1 (should be cached)
        var level1Again = cache.GetOrCreate("Level1", () => new TestScene());
        Assert.Same(level1, level1Again);

        // Preload next level while playing
        cache.Preload("Level3", () => new TestScene());

        cache.GetOrCreate("GameOver", () => new TestScene());

        // Assert
        Assert.Equal(5, cache.Count);
        Assert.True(cache.Contains("MainMenu"));
        Assert.True(cache.Contains("Level1"));
        Assert.True(cache.Contains("Level2"));
        Assert.True(cache.Contains("Level3"));
        Assert.True(cache.Contains("GameOver"));
    }

    #region Async Preloading Tests

    [Fact]
    public async Task PreloadAsync_WithValidScene_LoadsSceneInBackground()
    {
        // Arrange
        var cache = new SceneCache(5);

        // Act
        await cache.PreloadAsync("TestScene", () => new TestScene());

        // Assert
        Assert.True(cache.Contains("TestScene"));
        Assert.Equal(1, cache.Count);
        Assert.False(cache.IsPreloading("TestScene"));
    }

    [Fact]
    public async Task PreloadAsync_WithAlreadyCachedScene_DoesNothing()
    {
        // Arrange
        var cache = new SceneCache(5);
        var original = cache.GetOrCreate("TestScene", () => new TestScene());

        // Act
        await cache.PreloadAsync("TestScene", () => new TestScene());

        // Assert
        var retrieved = cache.GetOrCreate("TestScene", () => new TestScene());
        Assert.Same(original, retrieved); // Should be same instance
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public async Task PreloadAsync_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        var cache = new SceneCache(5);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            cache.PreloadAsync<TestScene>(null!, () => new TestScene()));
    }

    [Fact]
    public async Task PreloadAsync_WithNullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new SceneCache(5);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            cache.PreloadAsync<TestScene>("TestScene", null!));
    }

    [Fact]
    public async Task PreloadAsync_WithCancellationToken_CancelsOperation()
    {
        // Arrange
        var cache = new SceneCache(5);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            cache.PreloadAsync("TestScene", () => new TestScene(), cts.Token));

        Assert.False(cache.Contains("TestScene"));
        Assert.False(cache.IsPreloading("TestScene"));
    }

    [Fact]
    public async Task IsPreloading_DuringPreload_ReturnsTrue()
    {
        // Arrange
        var cache = new SceneCache(5);
        var tcs = new TaskCompletionSource<bool>();
        var slowFactory = () =>
        {
            tcs.Task.Wait(); // Wait until we signal
            return new TestScene();
        };

        // Act
        var preloadTask = cache.PreloadAsync("SlowScene", slowFactory);
        
        // Give preload task time to start
        await Task.Delay(10);
        var isPreloading = cache.IsPreloading("SlowScene");
        
        // Signal factory to complete
        tcs.SetResult(true);
        await preloadTask;
        
        var isPreloadingAfter = cache.IsPreloading("SlowScene");

        // Assert
        Assert.True(isPreloading);
        Assert.False(isPreloadingAfter);
        Assert.True(cache.Contains("SlowScene"));
    }

    [Fact]
    public void IsPreloading_WithNonExistentScene_ReturnsFalse()
    {
        // Arrange
        var cache = new SceneCache(5);

        // Act
        var isPreloading = cache.IsPreloading("NonExistent");

        // Assert
        Assert.False(isPreloading);
    }

    [Fact]
    public async Task PreloadMultipleAsync_WithMultipleScenes_LoadsAllInParallel()
    {
        // Arrange
        var cache = new SceneCache(10);
        var requests = new[]
        {
            ("Scene1", (Func<IScene>)(() => new TestScene())),
            ("Scene2", (Func<IScene>)(() => new TestScene())),
            ("Scene3", (Func<IScene>)(() => new TestScene())),
            ("Scene4", (Func<IScene>)(() => new TestScene())),
            ("Scene5", (Func<IScene>)(() => new TestScene()))
        };

        // Act
        await cache.PreloadMultipleAsync(requests);

        // Assert
        Assert.Equal(5, cache.Count);
        Assert.True(cache.Contains("Scene1"));
        Assert.True(cache.Contains("Scene2"));
        Assert.True(cache.Contains("Scene3"));
        Assert.True(cache.Contains("Scene4"));
        Assert.True(cache.Contains("Scene5"));

        // Parallel execution should be faster than sequential
        // (though this is timing-dependent, so we just check all loaded)
        Assert.False(cache.IsPreloading("Scene1"));
        Assert.False(cache.IsPreloading("Scene5"));
    }

    [Fact]
    public async Task PreloadMultipleAsync_WithNullRequests_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new SceneCache(5);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            cache.PreloadMultipleAsync(null!));
    }

    [Fact]
    public async Task PreloadMultipleAsync_WithCancellation_CancelsAll()
    {
        // Arrange
        var cache = new SceneCache(10);
        using var cts = new CancellationTokenSource();
        var requests = new[]
        {
            ("Scene1", (Func<IScene>)(() => new TestScene())),
            ("Scene2", (Func<IScene>)(() => new TestScene())),
            ("Scene3", (Func<IScene>)(() => new TestScene()))
        };

        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            cache.PreloadMultipleAsync(requests, cts.Token));

        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public async Task ScenePreloaded_OnSuccessfulPreload_RaisesEvent()
    {
        // Arrange
        var cache = new SceneCache(5);
        string? preloadedKey = null;
        bool? preloadSuccess = null;

        cache.ScenePreloaded += (sender, e) =>
        {
            preloadedKey = e.SceneKey;
            preloadSuccess = e.Success;
        };

        // Act
        await cache.PreloadAsync("TestScene", () => new TestScene());

        // Assert
        Assert.Equal("TestScene", preloadedKey);
        Assert.True(preloadSuccess);
    }

    [Fact]
    public async Task ScenePreloaded_OnFailedPreload_RaisesEventWithException()
    {
        // Arrange
        var cache = new SceneCache(5);
        string? preloadedKey = null;
        bool? preloadSuccess = null;
        Exception? capturedException = null;

        cache.ScenePreloaded += (sender, e) =>
        {
            preloadedKey = e.SceneKey;
            preloadSuccess = e.Success;
            capturedException = e.Exception;
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await cache.PreloadAsync<TestScene>("FailingScene", () => throw new InvalidOperationException("Test error"));
        });

        Assert.Equal("FailingScene", preloadedKey);
        Assert.False(preloadSuccess);
        Assert.NotNull(capturedException);
        Assert.IsType<InvalidOperationException>(capturedException);
    }

    [Fact]
    public async Task PreloadAsync_ConcurrentPreloads_HandlesCorrectly()
    {
        // Arrange
        var cache = new SceneCache(20);
        var tasks = new List<Task>();

        // Act - Start multiple preloads concurrently
        for (int i = 0; i < 10; i++)
        {
            var sceneKey = $"Scene{i}";
            tasks.Add(cache.PreloadAsync(sceneKey, () => new TestScene()));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, cache.Count);
        for (int i = 0; i < 10; i++)
        {
            Assert.True(cache.Contains($"Scene{i}"));
            Assert.False(cache.IsPreloading($"Scene{i}"));
        }
    }

    [Fact]
    public async Task PreloadAsync_ThenGetOrCreate_ReturnsCachedInstance()
    {
        // Arrange
        var cache = new SceneCache(5);

        // Act - Preload first
        await cache.PreloadAsync("TestScene", () => new TestScene());
        
        // Get the preloaded scene
        var scene1 = cache.GetOrCreate("TestScene", () => new TestScene());
        var scene2 = cache.GetOrCreate("TestScene", () => new TestScene());

        // Assert
        Assert.Same(scene1, scene2);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public async Task PreloadAsync_DuplicatePreload_IgnoresSecondRequest()
    {
        // Arrange
        var cache = new SceneCache(5);
        var createCount = 0;
        var tcs = new TaskCompletionSource<bool>();
        
        var slowFactory = () =>
        {
            Interlocked.Increment(ref createCount);
            tcs.Task.Wait(100); // Wait briefly
            return new TestScene();
        };

        // Act - Start two preloads for same scene simultaneously
        var task1 = cache.PreloadAsync("TestScene", slowFactory);
        await Task.Delay(10); // Small delay to ensure first starts
        var task2 = cache.PreloadAsync("TestScene", slowFactory);
        
        // Signal completion
        tcs.SetResult(true);
        
        await Task.WhenAll(task1, task2);

        // Assert - Should only create scene once (second request ignored as already cached/preloading)
        Assert.Equal(1, cache.Count);
    }

    #endregion
}
