using System.Numerics;
using System.Text;
using MicroEngine.Conformance.Graphics3D;
using MicroEngine.Content;
using MicroEngine.Graphics.Devices;
using MicroEngine.Graphics3D;
using MicroEngine.Platform;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace MicroEngine.Graphics3D.Tests;

[TestClass]
public sealed class GltfReaderTests
{
    private static readonly ImageData Checker = new(2, 2, [255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 255, 128]);

    [TestMethod]
    public void Gltf_ReadsPositionsNormalsUvsIndicesAndTheBaseColour()
    {
        var mesh = GltfReader.Read(TestMeshes.CubeGltf(2, new Vector4(0.5f, 0.25f, 1, 1), Checker), "cube.gltf");
        var expected = TestMeshes.CubeGeometry(2);
        Assert.AreEqual("cube", mesh.Name);
        Assert.HasCount(1, mesh.Primitives);
        var p = mesh.Primitives[0];
        CollectionAssert.AreEqual(expected.Vertices, p.Vertices, "every vertex exact (positions, normals, uvs)");
        CollectionAssert.AreEqual(expected.Indices, p.Indices);
        Assert.AreEqual(new Vector4(0.5f, 0.25f, 1, 1), p.BaseColor);
        Assert.IsNotNull(p.BaseColorTexture);
        Assert.AreEqual((2, 2), (p.BaseColorTexture.Width, p.BaseColorTexture.Height));
        CollectionAssert.AreEqual(Checker.Rgba, p.BaseColorTexture.Rgba, "the PNG decodes to the texels it was written from, straight alpha kept");
        Assert.AreEqual((new Vector3(-1), new Vector3(1)), mesh.Bounds());
        Assert.AreEqual(24, mesh.VertexCount);
        Assert.AreEqual(36, mesh.IndexCount);
    }

    [TestMethod]
    public void Glb_ReadsTheSameMeshAsTheJsonForm()
    {
        var gltf = TestMeshes.CubeGltf(1, Vector4.One);
        var a = GltfReader.Read(gltf, "cube.gltf").Primitives[0];
        var b = GltfReader.Read(TestMeshes.ToGlb(gltf), "cube.glb").Primitives[0];
        CollectionAssert.AreEqual(a.Vertices, b.Vertices);
        CollectionAssert.AreEqual(a.Indices, b.Indices);
    }

    [TestMethod]
    public void NodeTransforms_AreAppliedToPositionsAndNormals()
    {
        // translation (5, 0, 0) and a quarter turn about Y: the +Z face (normal +Z) ends up facing +X.
        var q = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
        string extras = $"\"translation\": [5, 0, 0], \"rotation\": [{q.X.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}, {q.Y.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}, 0, {q.W.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}]";
        var mesh = GltfReader.Read(TestMeshes.CubeGltf(2, Vector4.One, nodeExtras: extras), "moved.gltf");
        var (min, max) = mesh.Bounds();
        Assert.IsTrue(Vector3.Distance(min, new Vector3(4, -1, -1)) < 1e-5f && Vector3.Distance(max, new Vector3(6, 1, 1)) < 1e-5f, $"{min} {max}");
        var front = mesh.Primitives[0].Vertices[0]; // first vertex of the +Z face
        Assert.IsTrue(Vector3.Distance(front.Normal, Vector3.UnitX) < 1e-5f, $"normal {front.Normal}");
    }

    [TestMethod]
    public void NodeMatrix_IsColumnMajorAsGltfSays()
    {
        // glTF matrix [1,0,0,0, 0,1,0,0, 0,0,1,0, 3,4,5,1] is a translation by (3, 4, 5).
        var mesh = GltfReader.Read(TestMeshes.CubeGltf(2, Vector4.One, nodeExtras: "\"matrix\": [1,0,0,0, 0,1,0,0, 0,0,1,0, 3,4,5,1]"), "m.gltf");
        Assert.AreEqual((new Vector3(2, 3, 4), new Vector3(4, 5, 6)), mesh.Bounds());
    }

    [TestMethod]
    public void MissingNormals_AreComputedFacingOutwards()
    {
        var mesh = GltfReader.Read(TestMeshes.CubeGltf(2, Vector4.One, withNormals: false), "flat.gltf");
        foreach (var v in mesh.Primitives[0].Vertices)
            Assert.IsGreaterThan(0f, Vector3.Dot(v.Normal, v.Position), $"normal {v.Normal} at {v.Position} points outwards");
    }

    [TestMethod]
    public void UnsupportedFeatures_AreRefusedNamingThem()
    {
        string Json(string replace, string with) => Encoding.UTF8.GetString(TestMeshes.CubeGltf(1, Vector4.One)).Replace(replace, with);
        var cases = new (string Json, string Named)[]
        {
            (Json("\"asset\": { \"version\": \"2.0\" },", "\"asset\": { \"version\": \"2.0\" }, \"extensionsRequired\": [\"KHR_draco_mesh_compression\"],"), "KHR_draco_mesh_compression"),
            (Json("\"indices\": 3,", "\"indices\": 3, \"mode\": 1,"), "mode 1"),
            (Json("\"asset\": { \"version\": \"2.0\" }", "\"asset\": { \"version\": \"1.0\" }"), "1.0"),
            (Json("\"type\": \"SCALAR\" }", "\"type\": \"SCALAR\", \"sparse\": {} }"), "sparse"),
        };
        foreach (var (json, named) in cases)
        {
            var e = Assert.ThrowsExactly<InvalidDataException>(() => GltfReader.Read(Encoding.UTF8.GetBytes(json), "bad.gltf"));
            Assert.Contains(named, e.Message);
            Assert.Contains("bad.gltf", e.Message);
        }
        Assert.ThrowsExactly<InvalidDataException>(() => GltfReader.Read("not json"u8, "garbage.gltf"));
    }

    [TestMethod]
    public void MeshAssetKind_LoadsGltfAndGlbThroughTheStore_WithExternalBuffers()
    {
        var gltf = TestMeshes.CubeGltf(2, Vector4.One);
        var glb = TestMeshes.ToGlb(gltf);
        // An external buffer next to the .gltf, referred to by a relative URI.
        var json = Encoding.UTF8.GetString(gltf);
        const string marker = "data:application/octet-stream;base64,";
        int start = json.IndexOf(marker, StringComparison.Ordinal);
        int end = json.IndexOf('"', start);
        var bin = Convert.FromBase64String(json[(start + marker.Length)..end]);
        var external = json[..start] + "cube%20data.bin" + json[end..];
        var files = new Dictionary<string, byte[]> { ["models/cube.glb"] = glb, ["models/cube.gltf"] = Encoding.UTF8.GetBytes(external), ["models/cube data.bin"] = bin };
        using var store = new AssetStore(new MemorySource(files), new AssetLoaderRegistry().AddMeshes());
        Assert.AreEqual(MeshAssets.Kind, store.Loaders.KindOf("models/cube.glb"));
        var fromGlb = store.Get(store.Root.Load<MeshData>(MeshAssets.Kind, "models/cube.glb"));
        var fromGltf = store.Get(store.Root.Load<MeshData>(MeshAssets.Kind, "models/cube.gltf"));
        CollectionAssert.AreEqual(fromGlb.Primitives[0].Vertices, fromGltf.Primitives[0].Vertices);
        Assert.AreEqual(36, fromGltf.IndexCount);
    }

    [TestMethod]
    public void Renderer3D_OnATier0Device_IsTheTypedErrorNamingShaders()
    {
        using var device = new ReferenceGraphicsDevice();
        var e = Assert.ThrowsExactly<GraphicsCapabilityException>(() => new Renderer3D(device));
        Assert.AreEqual(GraphicsFeatures.Shaders, e.Missing);
        Assert.AreEqual("reference", e.Device);
    }

    private sealed class MemorySource(Dictionary<string, byte[]> files) : IContentSource
    {
        public string Location => "memory";
        public bool Exists(string path) => files.ContainsKey(path);
        public Stream Open(string path) => files.TryGetValue(path, out var b) ? new MemoryStream(b) : throw new FileNotFoundException(path);
        public IReadOnlyList<string> Files(string folder) => [.. files.Keys.Where(k => k.StartsWith(folder + "/", StringComparison.Ordinal)).Order(StringComparer.Ordinal)];
    }
}
