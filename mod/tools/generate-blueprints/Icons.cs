using System.IO.Compression;
using System.Text;

namespace SEE.Tools.GenerateBlueprints;

/// <summary>
/// Generates the 24 icons (128x128 PNG) plus one <c>.png.meta.json</c> per icon.
///
/// P9 ingest hook: when the art card has rendered the final sprite at
/// <c>&lt;mod root&gt;/art/icons/&lt;IconName&gt;.png</c>, that file is the source of truth and its bytes
/// are copied into <c>data/Icons/</c> verbatim (so <c>--check</c> keeps passing once the generator has
/// been re-run). When the rendered sprite is absent, the programmatic placeholder below is drawn.
///
/// The placeholder art is intentionally simple and text-free (contract 4.11): a faction-tinted badge,
/// a boiler silhouette whose size grows with the footprint, and one to four tier pips. The meta shape
/// and file names are the shipped contract shape, so no blueprint has to change when P9 art lands.
/// </summary>
internal static class Icons
{
    public const int PixelSize = 128;
    public const int ExpectedIconCount = 24;

    private static readonly uint[] CrcTable = BuildCrcTable();

    /// <summary>
    /// Renders the icon for one blueprint: the P9-rendered sprite at <paramref name="ingestPath"/>
    /// when it exists, otherwise the programmatic placeholder.
    /// </summary>
    /// <param name="variant">The (engine, faction) expansion the icon belongs to.</param>
    /// <param name="sizeIndex">0=Small, 1=Medium, 2=Large (placeholder geometry).</param>
    /// <param name="tierIndex">0=T1 .. 3=T4 (placeholder trim colour and pips).</param>
    /// <param name="ironTeeth">Faction tint of the placeholder.</param>
    /// <param name="ingestPath">Absolute path of the optional P9-rendered sprite; may be null or absent.</param>
    public static byte[] Render(
        FactionVariant variant, int sizeIndex, int tierIndex, bool ironTeeth, string? ingestPath = null)
    {
        if (!string.IsNullOrEmpty(ingestPath) && File.Exists(ingestPath))
        {
            return File.ReadAllBytes(ingestPath);
        }

        var pixels = new byte[PixelSize * PixelSize * 4];
        var background = ironTeeth ? new Rgba(42, 46, 52) : new Rgba(58, 44, 32);
        var body = ironTeeth ? new Rgba(118, 126, 136) : new Rgba(126, 88, 52);
        var bodyLight = ironTeeth ? new Rgba(150, 158, 168) : new Rgba(158, 114, 70);
        var trim = TierColor(tierIndex);

        FillRoundedRect(pixels, 6, 6, 116, 116, 16, background);
        FillRoundedRect(pixels, 6, 6, 116, 116, 16, trim, borderOnly: true, borderWidth: 3);

        var bodyWidth = 54 + sizeIndex * 16;
        var bodyHeight = 46 + sizeIndex * 10;
        var left = (PixelSize - bodyWidth) / 2;
        var right = left + bodyWidth;
        var bottom = 92;
        var top = bottom - bodyHeight;

        // Chimney behind the body top.
        FillRect(pixels, 60, top - 20, 10, 22, bodyLight);
        FillRect(pixels, 57, top - 24, 16, 6, trim);

        // Boiler body, dome and base plate.
        FillRoundedRect(pixels, left, top, bodyWidth, bodyHeight, 12, body);
        FillCircle(pixels, PixelSize / 2, top, bodyWidth / 2 - 4, bodyLight);
        FillRect(pixels, left - 4, bottom - 8, bodyWidth + 8, 8, bodyLight);
        FillRect(pixels, left + 6, bottom, 8, 8, bodyLight);
        FillRect(pixels, right - 14, bottom, 8, 8, bodyLight);

        // Water window (liquid inventory hint) and a piston rod.
        var windowWidth = bodyWidth - 20;
        FillRoundedRect(pixels, left + 10, bottom - 30, windowWidth, 16, 4, new Rgba(58, 118, 168));
        FillRect(pixels, left + 12, bottom - 28, windowWidth - 4, 4, new Rgba(96, 168, 214));
        FillRect(pixels, right + 2, top + 10, 6, bodyHeight - 22, bodyLight);
        FillRect(pixels, right + 8, top + 12, 5, 5, trim);

        // Tier pips (1-4), bottom-left.
        for (var index = 0; index <= tierIndex; index++)
        {
            FillRect(pixels, 14 + index * 12, 110, 8, 8, trim);
        }

        return EncodePng(pixels);
    }

    /// <summary>The meta file content used by the released sister mods: only <c>isSprite</c>.</summary>
    public static byte[] MetaJson() => Encoding.UTF8.GetBytes("{\n  \"isSprite\": true\n}\n");

    private static Rgba TierColor(int tierIndex) => tierIndex switch
    {
        0 => new Rgba(176, 126, 78),
        1 => new Rgba(186, 186, 190),
        2 => new Rgba(214, 178, 64),
        _ => new Rgba(116, 196, 236),
    };

    private static void FillRect(byte[] pixels, int x, int y, int width, int height, Rgba color)
    {
        for (var row = y; row < y + height; row++)
        {
            for (var column = x; column < x + width; column++)
            {
                SetPixel(pixels, column, row, color);
            }
        }
    }

    private static void FillRoundedRect(
        byte[] pixels, int x, int y, int width, int height, int radius, Rgba color,
        bool borderOnly = false, int borderWidth = 1)
    {
        var right = x + width - 1;
        var bottom = y + height - 1;
        for (var row = y; row <= bottom; row++)
        {
            for (var column = x; column <= right; column++)
            {
                var dx = column < x + radius ? x + radius - column : column > right - radius ? column - (right - radius) : 0;
                var dy = row < y + radius ? y + radius - row : row > bottom - radius ? row - (bottom - radius) : 0;
                if (dx * dx + dy * dy > radius * radius)
                {
                    continue;
                }

                if (borderOnly)
                {
                    var innerX = x + borderWidth;
                    var innerY = y + borderWidth;
                    var innerRight = right - borderWidth;
                    var innerBottom = bottom - borderWidth;
                    var innerRadius = Math.Max(1, radius - borderWidth);
                    var innerDx = column < innerX + innerRadius ? innerX + innerRadius - column
                        : column > innerRight - innerRadius ? column - (innerRight - innerRadius) : 0;
                    var innerDy = row < innerY + innerRadius ? innerY + innerRadius - row
                        : row > innerBottom - innerRadius ? row - (innerBottom - innerRadius) : 0;
                    if (column >= innerX && column <= innerRight && row >= innerY && row <= innerBottom
                        && innerDx * innerDx + innerDy * innerDy <= innerRadius * innerRadius)
                    {
                        continue;
                    }
                }

                SetPixel(pixels, column, row, color);
            }
        }
    }

    private static void FillCircle(byte[] pixels, int centerX, int centerY, int radius, Rgba color)
    {
        for (var row = centerY - radius; row <= centerY + radius; row++)
        {
            for (var column = centerX - radius; column <= centerX + radius; column++)
            {
                var dx = column - centerX;
                var dy = row - centerY;
                if (dx * dx + dy * dy <= radius * radius)
                {
                    SetPixel(pixels, column, row, color);
                }
            }
        }
    }

    private static void SetPixel(byte[] pixels, int x, int y, Rgba color)
    {
        if (x < 0 || y < 0 || x >= PixelSize || y >= PixelSize)
        {
            return;
        }

        var offset = (y * PixelSize + x) * 4;
        pixels[offset] = color.R;
        pixels[offset + 1] = color.G;
        pixels[offset + 2] = color.B;
        pixels[offset + 3] = 255;
    }

    private static byte[] EncodePng(byte[] rgba)
    {
        var stride = 1 + PixelSize * 4;
        var raw = new byte[PixelSize * stride];
        for (var row = 0; row < PixelSize; row++)
        {
            raw[row * stride] = 0;
            Array.Copy(rgba, row * PixelSize * 4, raw, row * stride + 1, PixelSize * 4);
        }

        byte[] compressed;
        using (var output = new MemoryStream())
        {
            using (var deflate = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
            {
                deflate.Write(raw, 0, raw.Length);
            }

            compressed = output.ToArray();
        }

        using var png = new MemoryStream();
        png.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, 0, 8);
        var header = new byte[13];
        WriteInt(header, 0, PixelSize);
        WriteInt(header, 4, PixelSize);
        header[8] = 8;
        header[9] = 6;
        header[10] = 0;
        header[11] = 0;
        header[12] = 0;
        WriteChunk(png, "IHDR", header);
        WriteChunk(png, "IDAT", compressed);
        WriteChunk(png, "IEND", Array.Empty<byte>());
        return png.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var typeBytes = Encoding.ASCII.GetBytes(type);
        var length = new byte[4];
        WriteInt(length, 0, data.Length);
        stream.Write(length, 0, 4);
        stream.Write(typeBytes, 0, 4);
        stream.Write(data, 0, data.Length);
        var crc = Crc(typeBytes, data);
        var crcBytes = new byte[4];
        WriteInt(crcBytes, 0, unchecked((int)crc));
        stream.Write(crcBytes, 0, 4);
    }

    private static uint Crc(byte[] typeBytes, byte[] data)
    {
        var crc = uint.MaxValue;
        foreach (var value in typeBytes)
        {
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }

        foreach (var value in data)
        {
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ uint.MaxValue;
    }

    private static void WriteInt(byte[] target, int offset, int value)
    {
        target[offset] = (byte)(value >> 24);
        target[offset + 1] = (byte)(value >> 16);
        target[offset + 2] = (byte)(value >> 8);
        target[offset + 3] = (byte)value;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint index = 0; index < table.Length; index++)
        {
            var value = index;
            for (var bit = 0; bit < 8; bit++)
            {
                value = (value & 1) != 0 ? 0xEDB88320u ^ (value >> 1) : value >> 1;
            }

            table[index] = value;
        }

        return table;
    }

    private readonly record struct Rgba(byte R, byte G, byte B);
}
