using System.Reflection;
using MicroEngine.Core;

namespace MicroEngine.Tiled.Tests;

/// <summary>ADR 0052: the parser stays replaceable, and games without maps do not carry it.</summary>
[TestClass]
public sealed class TiledArchitectureTests
{
    private static readonly Assembly Tiled = typeof(TiledMap).Assembly;

    private static IEnumerable<Type> Mentioned(Type type)
    {
        if (type.HasElementType) foreach (var t in Mentioned(type.GetElementType()!)) yield return t;
        else yield return type;
        if (type.IsGenericType)
            foreach (var argument in type.GetGenericArguments())
                foreach (var t in Mentioned(argument)) yield return t;
    }

    [TestMethod]
    public void No_public_member_exposes_a_type_of_the_parser()
    {
        const BindingFlags Visible = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var leaks = new List<string>();
        foreach (var type in Tiled.GetExportedTypes())
        {
            var used = new List<Type>();
            if (type.BaseType is { } baseType) used.Add(baseType);
            used.AddRange(type.GetInterfaces());
            foreach (var member in type.GetMembers(Visible))
                switch (member)
                {
                    case PropertyInfo p: used.Add(p.PropertyType); break;
                    case FieldInfo f: used.Add(f.FieldType); break;
                    case MethodBase m:
                        used.AddRange(m.GetParameters().Select(x => x.ParameterType));
                        if (m is MethodInfo mi) used.Add(mi.ReturnType);
                        break;
                }
            leaks.AddRange(used.SelectMany(Mentioned).Where(t => t.Assembly.GetName().Name == "DotTiled").Select(t => $"{type.Name} uses {t.FullName}"));
        }
        Assert.IsEmpty(leaks, string.Join("; ", leaks));
    }

    [TestMethod]
    public void The_package_references_only_the_engine_and_the_parser_and_the_engine_never_the_parser()
    {
        var references = Tiled.GetReferencedAssemblies().Select(a => a.Name!).Where(n => !n.StartsWith("System", StringComparison.Ordinal) && n != "netstandard").ToList();
        Assert.IsTrue(references.All(n => n == "DotTiled" || n.StartsWith("MicroEngine", StringComparison.Ordinal)), string.Join(", ", references));
        CollectionAssert.Contains(references, "DotTiled");
        Assert.IsFalse(typeof(Game).Assembly.GetReferencedAssemblies().Any(a => a.Name == "DotTiled"), "games without maps do not carry the parser");
    }
}
