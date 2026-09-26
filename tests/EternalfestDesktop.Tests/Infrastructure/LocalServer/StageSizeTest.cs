using EternalfestDesktop.Infrastructure.LocalServer;

namespace EternalfestDesktop.Tests.Infrastructure.LocalServer;

public sealed class StageSizeTest
{
    [Fact]
    public void Reads_the_bundled_loaders_stage_and_leaves_its_stream_open()
    {
        using var loader = BundledFlashFiles.NextToApp().OpenLoader();

        Assert.Equal(new StageSize(Width: 420, Height: 520), StageSize.Of(loader));
        Assert.True(loader.CanRead);
    }

    [Fact]
    public void Reads_an_uncompressed_stage_that_does_not_start_at_the_origin()
    {
        using var swf = new MemoryStream(UncompressedSwf(leftInTwips: 200, rightInTwips: 8600, topInTwips: 400, bottomInTwips: 10800));

        Assert.Equal(new StageSize(Width: 420, Height: 520), StageSize.Of(swf));
    }

    [Theory]
    [InlineData("ZWS")]
    [InlineData("PNG")]
    public void Refuses_what_it_cannot_read(string signature)
    {
        using var swf = new MemoryStream([.. System.Text.Encoding.ASCII.GetBytes(signature), 8, 0, 0, 0, 0, 0x78, 0, 0x05]);

        Assert.Throws<InvalidDataException>(() => StageSize.Of(swf));
    }

    [Fact]
    public void Refuses_a_truncated_header()
    {
        using var swf = new MemoryStream(UncompressedSwf(leftInTwips: 0, rightInTwips: 8400, topInTwips: 0, bottomInTwips: 10400)[..10]);

        Assert.Throws<InvalidDataException>(() => StageSize.Of(swf));
    }

    [Theory]
    [InlineData(968, 782)]
    [InlineData(520, 420)]
    [InlineData(1000, 808)]
    public void Keeps_its_proportions_when_scaled_to_a_height(int height, int width)
    {
        Assert.Equal(new StageSize(Width: width, Height: height), new StageSize(Width: 420, Height: 520).ScaledToHeight(height));
    }

    /// <summary>The 8-byte header, then the stage rectangle: 5 bits for the field size, then 4 fields of that size.</summary>
    private static byte[] UncompressedSwf(int leftInTwips, int rightInTwips, int topInTwips, int bottomInTwips)
    {
        const int BitsPerField = 16;
        var bits = Convert.ToString(BitsPerField, 2).PadLeft(5, '0')
            + string.Concat(new[] { leftInTwips, rightInTwips, topInTwips, bottomInTwips }.Select(field => Convert.ToString(field, 2).PadLeft(BitsPerField, '0')));
        bits = bits.PadRight((bits.Length + 7) / 8 * 8, '0');
        var rectangle = Enumerable.Range(0, bits.Length / 8).Select(index => Convert.ToByte(bits.Substring(index * 8, 8), 2));
        return [(byte)'F', (byte)'W', (byte)'S', 8, 0, 0, 0, 0, .. rectangle, 0, 24, 1, 0];
    }
}
