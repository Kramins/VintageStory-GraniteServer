namespace GraniteServer.Map;

/// <summary>
/// Block color mapping based on WebCartographer's medieval style rendering.
/// Maps block materials and color codes to ARGB color values.
/// Shared between mod (for colorCode lookup) and server (for rendering).
/// </summary>
public static class MapColors
{
    /// <summary>
    /// Default block material to map color code mapping.
    /// Ported from Vintagestory.GameContent.ChunkMapLayer.defaultMapColorCodes.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> DefaultMapColorCodes =
        new Dictionary<string, string>
        {
            { "Soil", "land" },
            { "Sand", "desert" },
            { "Ore", "land" },
            { "Gravel", "desert" },
            { "Stone", "land" },
            { "Leaves", "forest" },
            { "Plant", "plant" },
            { "Wood", "forest" },
            { "Snow", "glacier" },
            { "Liquid", "lake" },
            { "Ice", "glacier" },
            { "Lava", "lava" },
        };

    /// <summary>
    /// Color palette for medieval-style map rendering (hex strings).
    /// Ported from Vintagestory.GameContent.ChunkMapLayer.hexColorsByCode.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> HexColorsByCode =
        new Dictionary<string, string>
        {
            { "ink", "#483018" },
            { "settlement", "#856844" },
            { "wateredge", "#483018" },
            { "land", "#AC8858" },
            { "desert", "#C4A468" },
            { "forest", "#98844C" },
            { "road", "#805030" },
            { "plant", "#808650" },
            { "lake", "#CCC890" },
            { "ocean", "#CCC890" },
            { "glacier", "#E0E0C0" },
            { "lava", "#FF4400" },
            { "devastation", "#755c3c" },
        };

    /// <summary>
    /// Color palette as ARGB uint values.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, uint> ColorsByCode;

    static MapColors()
    {
        var colors = new Dictionary<string, uint>();
        foreach (var (key, hexValue) in HexColorsByCode)
        {
            colors[key] = ParseHexColor(hexValue);
        }
        ColorsByCode = colors;
    }

    /// <summary>
    /// Gets the color for a specific color code string.
    /// </summary>
    /// <param name="colorCode">The color code</param>
    /// <returns>ARGB color value</returns>
    public static uint GetColor(string colorCode)
    {
        return ColorsByCode.TryGetValue(colorCode, out var color) ? color : ColorsByCode["land"];
    }

    /// <summary>
    /// Gets color for a color code index.
    /// </summary>
    public static uint GetColorByIndex(int index)
    {
        var codes = ColorsByCode.Keys.ToArray();
        if (index >= 0 && index < codes.Length)
        {
            return ColorsByCode[codes[index]];
        }
        return ColorsByCode["land"];
    }

    /// <summary>
    /// Gets the index for a color code.
    /// </summary>
    public static int GetColorIndex(string colorCode)
    {
        var codes = ColorsByCode.Keys.ToArray();
        return Array.IndexOf(codes, colorCode);
    }

    /// <summary>
    /// Parses a hex color string to ARGB uint.
    /// </summary>
    /// <param name="hex">Hex color string (e.g., "#AC8858")</param>
    /// <returns>ARGB uint value with full alpha</returns>
    public static uint ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            var r = Convert.ToByte(hex.Substring(0, 2), 16);
            var g = Convert.ToByte(hex.Substring(2, 2), 16);
            var b = Convert.ToByte(hex.Substring(4, 2), 16);
            return 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;
        }
        return 0xFFAC8858; // Default to land color
    }

    /// <summary>
    /// Applies a brightness multiplier to a color.
    /// </summary>
    /// <param name="color">Base color</param>
    /// <param name="multiplier">Brightness multiplier (1.0 = no change)</param>
    /// <returns>Modified color</returns>
    public static uint ApplyBrightness(uint color, float multiplier)
    {
        var a = (byte)((color >> 24) & 0xFF);
        var r = (byte)Math.Clamp((int)(((color >> 16) & 0xFF) * multiplier), 0, 255);
        var g = (byte)Math.Clamp((int)(((color >> 8) & 0xFF) * multiplier), 0, 255);
        var b = (byte)Math.Clamp((int)((color & 0xFF) * multiplier), 0, 255);
        return (uint)((a << 24) | (r << 16) | (g << 8) | b);
    }

    /// <summary>
    /// Multiplies RGB channels of a color by a factor, clamping to 0-255.
    /// Ported from Vintagestory.API.MathTools.ColorUtil.ColorMultiply3Clamped.
    /// Alpha channel is preserved unchanged.
    /// </summary>
    public static int ColorMultiply3Clamped(int color, float multiplier)
    {
        int a = (int)(color & 0xFF000000u);
        int r = Math.Clamp((int)(((color >> 16) & 0xFF) * multiplier), 0, 255);
        int g = Math.Clamp((int)(((color >> 8) & 0xFF) * multiplier), 0, 255);
        int b = Math.Clamp((int)((color & 0xFF) * multiplier), 0, 255);
        return a | (r << 16) | (g << 8) | b;
    }

    /// <summary>
    /// Resolves a block ID to its color code using the block-color mapping
    /// and block-material fallback.
    /// </summary>
    /// <param name="blockId">The block ID</param>
    /// <param name="blockIdToColorCode">Block ID to mapColorCode mapping (from collectibles sync)</param>
    /// <param name="blockIdToMaterial">Block ID to BlockMaterial string mapping (from collectibles sync)</param>
    /// <returns>The resolved color code string</returns>
    public static string ResolveColorCode(
        int blockId,
        IReadOnlyDictionary<int, string>? blockIdToColorCode,
        IReadOnlyDictionary<int, string>? blockIdToMaterial)
    {
        // Try explicit mapColorCode first
        if (blockIdToColorCode != null
            && blockIdToColorCode.TryGetValue(blockId, out var colorCode)
            && !string.IsNullOrEmpty(colorCode)
            && ColorsByCode.ContainsKey(colorCode))
        {
            return colorCode;
        }

        // Fall back to material-based mapping
        if (blockIdToMaterial != null
            && blockIdToMaterial.TryGetValue(blockId, out var material)
            && !string.IsNullOrEmpty(material)
            && DefaultMapColorCodes.TryGetValue(material, out var materialColorCode))
        {
            return materialColorCode;
        }

        return "land";
    }

    /// <summary>
    /// Determines whether a block is a lake/water block based on its material.
    /// Ported from ChunkMapLayer.isLake().
    /// </summary>
    public static bool IsLake(string blockMaterial, string? blockPath = null)
    {
        if (blockMaterial == "Liquid")
            return true;

        if (blockMaterial == "Ice")
            return blockPath != "glacierice";

        return false;
    }
}
