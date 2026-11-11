using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace MicroEngine.Cli;

/// <summary>
/// A game's composition entry point: the public static <c>Configure(MicroEngine.Hosting.HostBuilder)</c> method that
/// its desktop <c>Program.cs</c> hands to the launcher, found in the game's built assembly by reading its metadata (the
/// assembly is never loaded or run). The browser, Android and iOS hosts call that same method, so whatever the game
/// composes there reaches every build.
/// </summary>
public static class EntryPoint
{
    /// <summary>The full name of the parameter type an entry point takes.</summary>
    public const string BuilderType = "MicroEngine.Hosting.HostBuilder";

    /// <summary>
    /// The entry points of the assembly at <paramref name="assemblyPath"/>, as C# names of their methods
    /// (<c>global::Namespace.Type.Configure</c>), in metadata order.
    /// </summary>
    public static IReadOnlyList<string> Find(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        var provider = new NameProvider();
        var found = new List<string>();
        foreach (var handle in metadata.TypeDefinitions)
        {
            var type = metadata.GetTypeDefinition(handle);
            if (!IsVisible(metadata, type)) continue;
            foreach (var methodHandle in type.GetMethods())
            {
                var method = metadata.GetMethodDefinition(methodHandle);
                var attributes = method.Attributes;
                if ((attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public || !attributes.HasFlag(MethodAttributes.Static)) continue;
                if (metadata.GetString(method.Name) != "Configure") continue;
                var signature = method.DecodeSignature(provider, null);
                if (signature.ReturnType == "System.Void" && signature.ParameterTypes is [BuilderType])
                    found.Add($"global::{CSharpName(metadata, type)}.Configure");
            }
        }
        return found;
    }

    /// <summary>
    /// The body of the generated host's <c>GameEntry.Configure(host)</c>: a call to the game's entry point.
    /// </summary>
    /// <exception cref="CliException">The game has no entry point, or more than one.</exception>
    public static string Call(GameProject game, string target)
    {
        string assembly = Path.Combine(game.TargetDir, game.Id + ".dll");
        var found = File.Exists(assembly) ? Find(assembly) : [];
        if (found.Count == 1) return $"{found[0]}(host)";
        if (found.Count > 1)
            throw new CliException($"{game.Id} has {found.Count} Configure(HostBuilder) entry points ({string.Join(", ", found.Select(f => f.Replace("global::", "")))}); keep one, the method its Program.cs passes to DesktopLauncher.Run.");
        throw new CliException($"{target} packages compose the game with its Configure entry point, and {game.Id} has none. Add one to the game class, and pass it to DesktopLauncher.Run in Program.cs:\n  public static void Configure(HostBuilder host) => host.UseGame(() => new MainGame());\n  return DesktopLauncher.Run(MainGame.Configure, args);");
    }

    private static bool IsVisible(MetadataReader metadata, TypeDefinition type)
    {
        var visibility = type.Attributes & TypeAttributes.VisibilityMask;
        if (visibility == TypeAttributes.Public) return true;
        if (visibility != TypeAttributes.NestedPublic) return false;
        return IsVisible(metadata, metadata.GetTypeDefinition(type.GetDeclaringType()));
    }

    private static string CSharpName(MetadataReader metadata, TypeDefinition type)
    {
        string name = metadata.GetString(type.Name);
        if (type.GetDeclaringType() is { IsNil: false } outer) return $"{CSharpName(metadata, metadata.GetTypeDefinition(outer))}.{name}";
        string ns = metadata.GetString(type.Namespace);
        return ns.Length > 0 ? $"{ns}.{name}" : name;
    }

    /// <summary>Type names for signature decoding: enough to recognise void and the builder type.</summary>
    private sealed class NameProvider : ISignatureTypeProvider<string, object?>
    {
        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode == PrimitiveTypeCode.Void ? "System.Void" : typeCode.ToString();
        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            var type = reader.GetTypeDefinition(handle);
            return $"{reader.GetString(type.Namespace)}.{reader.GetString(type.Name)}";
        }
        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            var type = reader.GetTypeReference(handle);
            return $"{reader.GetString(type.Namespace)}.{reader.GetString(type.Name)}";
        }
        public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => "?";
        public string GetSZArrayType(string elementType) => elementType + "[]";
        public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[,]";
        public string GetByReferenceType(string elementType) => elementType + "&";
        public string GetPointerType(string elementType) => elementType + "*";
        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) => $"{genericType}<{string.Join(",", typeArguments)}>";
        public string GetGenericTypeParameter(object? genericContext, int index) => $"!{index}";
        public string GetGenericMethodParameter(object? genericContext, int index) => $"!!{index}";
        public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr";
        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
        public string GetPinnedType(string elementType) => elementType;
    }
}
