using System.IO.Compression;

namespace EternalfestDesktop.Infrastructure.LocalServer;

/// <summary>The size of a Flash movie's stage, in pixels, or of something keeping its proportions.</summary>
public sealed record StageSize(int Width, int Height)
{
    private const int TwipsPerPixel = 20;

    /// <summary>Reads the stage from the SWF header: a rectangle in twips, after the signature, version and length.</summary>
    /// <exception cref="InvalidDataException">Not an uncompressed or zlib-compressed SWF.</exception>
    public static StageSize Of(Stream swf)
    {
        Span<byte> header = stackalloc byte[8];
        swf.ReadExactly(header);
        switch (header[..3])
        {
            case [(byte)'F', (byte)'W', (byte)'S']:
                return OfRectangle(swf);
            case [(byte)'C', (byte)'W', (byte)'S']:
                using (var body = new ZLibStream(swf, CompressionMode.Decompress, leaveOpen: true))
                    return OfRectangle(body);
            default:
                throw new InvalidDataException("Not a SWF, or an LZMA-compressed one.");
        }
    }

    private static StageSize OfRectangle(Stream body)
    {
        var rectangle = new BitReader(body);
        var bitsPerField = rectangle.Read(5);
        var left = rectangle.Read(bitsPerField);
        var right = rectangle.Read(bitsPerField);
        var top = rectangle.Read(bitsPerField);
        var bottom = rectangle.Read(bitsPerField);
        return new StageSize(Width: (right - left) / TwipsPerPixel, Height: (bottom - top) / TwipsPerPixel);
    }

    public StageSize ScaledToHeight(int height) =>
        new(Width: (int)Math.Round((double)Width * height / Height, MidpointRounding.AwayFromZero), Height: height);

    /// <summary>Reads unsigned big-endian bit fields, like the SWF header's.</summary>
    private sealed class BitReader(Stream stream)
    {
        private int _byte;
        private int _bitsLeft;

        public int Read(int bits)
        {
            var value = 0;
            for (var bit = 0; bit < bits; bit++)
            {
                if (_bitsLeft == 0)
                {
                    _byte = stream.ReadByte();
                    if (_byte < 0)
                        throw new InvalidDataException("The SWF header is truncated.");
                    _bitsLeft = 8;
                }
                _bitsLeft--;
                value = (value << 1) | ((_byte >> _bitsLeft) & 1);
            }
            return value;
        }
    }
}
