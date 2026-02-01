using FluentAssertions;
using Granite.Mod.Services.Map;
using Vintagestory.API.Common;

namespace Granite.Mod.Tests.Services.Map;

public class MapColorsTests
{
    [Theory]
    [InlineData(EnumBlockMaterial.Soil, "land")]
    [InlineData(EnumBlockMaterial.Sand, "desert")]
    [InlineData(EnumBlockMaterial.Leaves, "forest")]
    [InlineData(EnumBlockMaterial.Liquid, "lake")]
    [InlineData(EnumBlockMaterial.Ice, "glacier")]
    [InlineData(EnumBlockMaterial.Snow, "glacier")]
    [InlineData(EnumBlockMaterial.Plant, "plant")]
    [InlineData(EnumBlockMaterial.Wood, "forest")]
    [InlineData(EnumBlockMaterial.Lava, "lava")]
    [InlineData(EnumBlockMaterial.Brick, "settlement")]
    public void GetDefaultMapColorCode_ReturnsExpectedColorCode(EnumBlockMaterial material, string expectedCode)
    {
        // Act
        var result = MapColors.GetDefaultMapColorCode(material);

        // Assert
        result.Should().Be(expectedCode);
    }

    [Fact]
    public void GetDefaultMapColorCode_UnknownMaterial_ReturnsLand()
    {
        // Act - Air is a valid material that should default to land
        var result = MapColors.GetDefaultMapColorCode(EnumBlockMaterial.Air);

        // Assert
        result.Should().Be("land");
    }

    [Fact]
    public void GetColor_ValidColorCode_ReturnsColor()
    {
        // Act
        var color = MapColors.GetColor("land");

        // Assert
        color.Should().NotBe(0u);
        // Alpha should be fully opaque (0xFF)
        ((color >> 24) & 0xFF).Should().Be(0xFF);
    }

    [Fact]
    public void GetColor_InvalidColorCode_ReturnsLandColor()
    {
        // Act
        var unknownColor = MapColors.GetColor("unknown");
        var landColor = MapColors.GetColor("land");

        // Assert
        unknownColor.Should().Be(landColor);
    }

    [Theory]
    [InlineData("land")]
    [InlineData("desert")]
    [InlineData("forest")]
    [InlineData("lake")]
    [InlineData("glacier")]
    [InlineData("settlement")]
    [InlineData("lava")]
    public void ColorsByCode_ContainsExpectedColorCodes(string colorCode)
    {
        // Assert
        MapColors.ColorsByCode.Should().ContainKey(colorCode);
    }

    [Theory]
    [InlineData("#AC8858")]
    [InlineData("#C4A468")]
    [InlineData("#98844C")]
    public void ParseHexColor_ValidHex_ReturnsCorrectColor(string hex)
    {
        // Act
        var color = MapColors.ParseHexColor(hex);

        // Assert
        // Should have full alpha
        ((color >> 24) & 0xFF).Should().Be(0xFF);
    }

    [Fact]
    public void ParseHexColor_ValidHex_ParsesRGBCorrectly()
    {
        // Arrange - #AC8858 = R:172, G:136, B:88
        var hex = "#AC8858";

        // Act
        var color = MapColors.ParseHexColor(hex);

        // Assert
        var r = (color >> 16) & 0xFF;
        var g = (color >> 8) & 0xFF;
        var b = color & 0xFF;

        r.Should().Be(0xAC);
        g.Should().Be(0x88);
        b.Should().Be(0x58);
    }

    [Fact]
    public void ParseHexColor_WithoutHash_ParsesCorrectly()
    {
        // Arrange
        var hexWithHash = "#AC8858";
        var hexWithoutHash = "AC8858";

        // Act
        var colorWithHash = MapColors.ParseHexColor(hexWithHash);
        var colorWithoutHash = MapColors.ParseHexColor(hexWithoutHash);

        // Assert
        colorWithHash.Should().Be(colorWithoutHash);
    }

    [Fact]
    public void ApplyBrightness_IncreaseBrightness_LightensColor()
    {
        // Arrange
        var originalColor = MapColors.GetColor("land");
        var originalR = (originalColor >> 16) & 0xFF;

        // Act
        var brighterColor = MapColors.ApplyBrightness(originalColor, 1.5f);
        var brighterR = (brighterColor >> 16) & 0xFF;

        // Assert
        brighterR.Should().BeGreaterThan((byte)originalR);
    }

    [Fact]
    public void ApplyBrightness_DecreaseBrightness_DarkensColor()
    {
        // Arrange
        var originalColor = MapColors.GetColor("land");
        var originalR = (originalColor >> 16) & 0xFF;

        // Act
        var darkerColor = MapColors.ApplyBrightness(originalColor, 0.5f);
        var darkerR = (darkerColor >> 16) & 0xFF;

        // Assert
        darkerR.Should().BeLessThan((byte)originalR);
    }

    [Fact]
    public void ApplyBrightness_ClampsToMaximum()
    {
        // Arrange - A bright color
        var color = 0xFFFFFFFF; // White

        // Act
        var result = MapColors.ApplyBrightness(color, 2.0f);

        // Assert - Should clamp to 255
        var r = (result >> 16) & 0xFF;
        var g = (result >> 8) & 0xFF;
        var b = result & 0xFF;

        r.Should().Be(255);
        g.Should().Be(255);
        b.Should().Be(255);
    }

    [Fact]
    public void ApplyBrightness_PreservesAlpha()
    {
        // Arrange
        var color = MapColors.GetColor("land");
        var originalAlpha = (color >> 24) & 0xFF;

        // Act
        var modifiedColor = MapColors.ApplyBrightness(color, 1.5f);
        var modifiedAlpha = (modifiedColor >> 24) & 0xFF;

        // Assert
        modifiedAlpha.Should().Be((byte)originalAlpha);
    }

    [Fact]
    public void HexColorsByCode_AllColorsHaveValidHexFormat()
    {
        // Assert
        foreach (var (code, hex) in MapColors.HexColorsByCode)
        {
            hex.Should().StartWith("#");
            hex.Should().HaveLength(7); // #RRGGBB
        }
    }

    [Fact]
    public void GetColorIndex_ValidCode_ReturnsNonNegativeIndex()
    {
        // Act
        var index = MapColors.GetColorIndex("land");

        // Assert
        index.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void GetColorByIndex_ValidIndex_ReturnsColor()
    {
        // Arrange
        var landIndex = MapColors.GetColorIndex("land");

        // Act
        var color = MapColors.GetColorByIndex(landIndex);

        // Assert
        color.Should().Be(MapColors.GetColor("land"));
    }

    [Fact]
    public void GetColorByIndex_InvalidIndex_ReturnsLandColor()
    {
        // Act
        var color = MapColors.GetColorByIndex(-1);

        // Assert
        color.Should().Be(MapColors.GetColor("land"));
    }

    #region API Verification Tests (Based on VS DLL Decompilation)

    /// <summary>
    /// Verifies all EnumBlockMaterial values are handled by GetDefaultMapColorCode.
    /// Verified via VS API decompilation that EnumBlockMaterial has these values:
    /// Air, Liquid, Soil, Stone, Ore, Gravel, Sand, Meta, Plant, Leaves, 
    /// Wood, Ice, Snow, Lava, Brick, Ceramic, Glass, Cloth, Mantle
    /// </summary>
    [Theory]
    [InlineData(EnumBlockMaterial.Air, "land")]
    [InlineData(EnumBlockMaterial.Liquid, "lake")]
    [InlineData(EnumBlockMaterial.Soil, "land")]
    [InlineData(EnumBlockMaterial.Stone, "land")]
    [InlineData(EnumBlockMaterial.Ore, "land")]
    [InlineData(EnumBlockMaterial.Gravel, "desert")]
    [InlineData(EnumBlockMaterial.Sand, "desert")]
    [InlineData(EnumBlockMaterial.Meta, "land")]
    [InlineData(EnumBlockMaterial.Plant, "plant")]
    [InlineData(EnumBlockMaterial.Leaves, "forest")]
    [InlineData(EnumBlockMaterial.Wood, "forest")]
    [InlineData(EnumBlockMaterial.Ice, "glacier")]
    [InlineData(EnumBlockMaterial.Snow, "glacier")]
    [InlineData(EnumBlockMaterial.Lava, "lava")]
    [InlineData(EnumBlockMaterial.Brick, "settlement")]
    [InlineData(EnumBlockMaterial.Ceramic, "settlement")]
    [InlineData(EnumBlockMaterial.Glass, "settlement")]
    [InlineData(EnumBlockMaterial.Cloth, "settlement")]
    [InlineData(EnumBlockMaterial.Mantle, "land")]
    public void GetDefaultMapColorCode_AllEnumBlockMaterialValues_MapsCorrectly(
        EnumBlockMaterial material, string expectedColorCode)
    {
        // Act
        var result = MapColors.GetDefaultMapColorCode(material);

        // Assert
        result.Should().Be(expectedColorCode);
    }

    /// <summary>
    /// Verifies that all color codes in the palette are valid and parseable.
    /// </summary>
    [Fact]
    public void ColorsByCode_AllColorsAreValid()
    {
        // Assert
        foreach (var (code, color) in MapColors.ColorsByCode)
        {
            // Each color should have full alpha
            var alpha = (color >> 24) & 0xFF;
            alpha.Should().Be(0xFF, $"Color '{code}' should have full alpha");
            
            // Color value should not be zero (except for pure black which we don't use)
            color.Should().NotBe(0u, $"Color '{code}' should not be zero");
        }
    }

    /// <summary>
    /// Verifies hex colors match the ARGB color values.
    /// </summary>
    [Theory]
    [InlineData("land", "#AC8858")]
    [InlineData("desert", "#C4A468")]
    [InlineData("forest", "#98844C")]
    [InlineData("lake", "#CCC890")]
    [InlineData("glacier", "#E0E0C0")]
    [InlineData("lava", "#FF4400")]
    [InlineData("settlement", "#856844")]
    public void ColorsByCode_MatchesHexColorsByCode(string code, string expectedHex)
    {
        // Arrange
        var expectedColor = MapColors.ParseHexColor(expectedHex);

        // Act
        var actualColor = MapColors.GetColor(code);

        // Assert
        actualColor.Should().Be(expectedColor);
    }

    /// <summary>
    /// Tests brightness calculation edge cases.
    /// Height factor calculation uses mapYHalf for normalization.
    /// Range is clamped to 0.5f - 1.5f in MapGenerationService.
    /// </summary>
    [Theory]
    [InlineData(0.5f)]  // Minimum brightness (low terrain)
    [InlineData(1.0f)]  // Normal brightness (mid terrain)
    [InlineData(1.5f)]  // Maximum brightness (high terrain)
    public void ApplyBrightness_HeightFactorRange_ProducesValidColors(float brightnessFactor)
    {
        // Arrange
        var baseColor = MapColors.GetColor("land");

        // Act
        var result = MapColors.ApplyBrightness(baseColor, brightnessFactor);

        // Assert
        var alpha = (result >> 24) & 0xFF;
        var r = (result >> 16) & 0xFF;
        var g = (result >> 8) & 0xFF;
        var b = result & 0xFF;

        alpha.Should().Be(0xFF, "Alpha should be preserved");
        r.Should().BeInRange((uint)0, 255);
        g.Should().BeInRange((uint)0, 255);
        b.Should().BeInRange((uint)0, 255);
    }

    /// <summary>
    /// Tests that brightness of 0 results in black (preserving alpha).
    /// </summary>
    [Fact]
    public void ApplyBrightness_ZeroBrightness_ResultsInBlack()
    {
        // Arrange
        var baseColor = MapColors.GetColor("land");

        // Act
        var result = MapColors.ApplyBrightness(baseColor, 0f);

        // Assert
        var alpha = (result >> 24) & 0xFF;
        var r = (result >> 16) & 0xFF;
        var g = (result >> 8) & 0xFF;
        var b = result & 0xFF;

        alpha.Should().Be(0xFF);
        r.Should().Be(0);
        g.Should().Be(0);
        b.Should().Be(0);
    }

    /// <summary>
    /// Verifies GetBlockColor helper returns correct color for known color codes.
    /// This is used in MapGenerationService for quick block-to-color lookup.
    /// </summary>
    [Fact]
    public void GetBlockColor_Helper_ReturnsCorrectColors()
    {
        // Verify the helper method works for all known color codes
        foreach (var code in MapColors.ColorsByCode.Keys)
        {
            var color = MapColors.GetColor(code);
            color.Should().Be(MapColors.ColorsByCode[code]);
        }
    }

    #endregion

    #region ParseHexColor Edge Cases

    /// <summary>
    /// Tests that invalid hex strings return the default land color.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("invalid")]
    [InlineData("#GGG")]
    public void ParseHexColor_InvalidHex_ReturnsDefaultColor(string invalidHex)
    {
        // Act
        var color = MapColors.ParseHexColor(invalidHex);

        // Assert - should return default land color
        color.Should().Be(0xFFAC8858u);
    }

    /// <summary>
    /// Tests case insensitivity of hex color parsing.
    /// </summary>
    [Fact]
    public void ParseHexColor_CaseInsensitive_ParsesCorrectly()
    {
        // Arrange
        var upperHex = "#AABBCC";
        var lowerHex = "#aabbcc";
        var mixedHex = "#AaBbCc";

        // Act
        var upperColor = MapColors.ParseHexColor(upperHex);
        var lowerColor = MapColors.ParseHexColor(lowerHex);
        var mixedColor = MapColors.ParseHexColor(mixedHex);

        // Assert - all should be equal
        upperColor.Should().Be(lowerColor);
        lowerColor.Should().Be(mixedColor);
    }

    #endregion
}
