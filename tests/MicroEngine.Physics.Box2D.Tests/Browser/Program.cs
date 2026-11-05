using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using MicroEngine.Physics.Box2D;

[assembly: SupportedOSPlatform("browser")]

// Main only brings the runtime up; wwwroot/main.js runs the tests one call at a time, so the page stays responsive
// and a crash still leaves every earlier result on it.
Console.WriteLine("box2d browser tests: ready");

/// <summary>
/// A minimal MSTest runner: every non-abstract [TestClass] of this assembly, a new instance per [TestMethod], its
/// [TestInitialize] methods (base class first) before it and its [TestCleanup] methods after it. An inconclusive test
/// counts as skipped; anything else thrown fails it.
/// </summary>
public static partial class Runner
{
    private static readonly List<(Type Type, MethodInfo Method)> s_tests = Discover();

    private static List<(Type, MethodInfo)> Discover() =>
        typeof(Runner).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.GetCustomAttribute<TestClassAttribute>() is not null)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() is not null)
                .OrderBy(m => m.Name, StringComparer.Ordinal)
                .Select(m => (t, m)))
            .ToList();

    [JSExport]
    public static string Info() =>
        $"{RuntimeInformation.FrameworkDescription} {RuntimeInformation.ProcessArchitecture}; Box2D {Box2DPhysicsWorld2D.Box2DVersion}; " +
        $"native library {(Box2DPhysicsWorld2D.IsSupported ? "loaded" : "MISSING")}; {s_tests.Count} tests";

    [JSExport]
    public static int Count() => s_tests.Count;

    [JSExport]
    public static string Run(int index)
    {
        var (type, method) = s_tests[index];
        string name = $"{type.Name}.{method.Name}";
        object instance = Activator.CreateInstance(type)!;
        var chain = new List<Type>();
        for (var t = type; t is not null; t = t.BaseType) chain.Insert(0, t);
        try
        {
            foreach (var t in chain) Invoke<TestInitializeAttribute>(t, instance);
            method.Invoke(instance, null);
            return $"PASS {name}";
        }
        catch (TargetInvocationException e) when (e.InnerException is AssertInconclusiveException)
        {
            return $"SKIP {name}: {e.InnerException.Message}";
        }
        catch (TargetInvocationException e)
        {
            return $"FAIL {name}: {e.InnerException?.GetType().Name}: {e.InnerException?.Message}";
        }
        finally
        {
            for (int i = chain.Count - 1; i >= 0; i--)
            {
                try { Invoke<TestCleanupAttribute>(chain[i], instance); }
                catch (TargetInvocationException) { }
            }
        }
    }

    private static void Invoke<T>(Type declaring, object instance) where T : Attribute
    {
        foreach (var m in declaring.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            if (m.GetCustomAttribute<T>() is not null) m.Invoke(instance, null);
    }
}
