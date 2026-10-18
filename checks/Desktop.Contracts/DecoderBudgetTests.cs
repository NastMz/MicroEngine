using Microsoft.VisualStudio.TestTools.UnitTesting;
using MicroEngine.Desktop;
namespace Desktop.Contracts;
[TestClass] public class DecoderBudgetTests
{
    [TestMethod] public void DefaultAndExplicitBudgetsAreStyleIndependent()
    {
        var defaults = new DesktopWindowOptions(640, 360, "old"); Assert.AreEqual(256, defaults.MaxImageDimension); Assert.AreEqual(32, defaults.MaxImageResources);
        var large = new DesktopWindowOptions(768, 480, "real art", 2048, 32); Assert.AreEqual(2048, large.MaxImageDimension);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new DesktopWindowOptions(1, 1, "x", 0, 32));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new DesktopWindowOptions(1, 1, "x", 8193, 32));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new DesktopWindowOptions(1, 1, "x", 512, 0));
    }
    [TestMethod] public void DecoderRejectsInvalidHeaderBeforeNativeLoading()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => DesktopImageDecoder.Decode(null!));
        Assert.ThrowsExactly<InvalidDataException>(() => DesktopImageDecoder.Decode([]));
        Assert.ThrowsExactly<InvalidDataException>(() => DesktopImageDecoder.Decode(new byte[64]));
        byte[] header = new byte[33]; byte[] signature = [137, 80, 78, 71, 13, 10, 26, 10]; signature.CopyTo(header, 0);
        header[11] = 13; "IHDR"u8.CopyTo(header.AsSpan(12));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(16), 9000);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(20), 1);
        Assert.ThrowsExactly<InvalidDataException>(() => DesktopImageDecoder.Decode(header));
    }
}
