using FluentAssertions;
using GraniteServer.Map;
using Xunit;

namespace Granite.Tests.Services.Map;

/// <summary>
/// Tests for the shared MapColors class used for map rendering.
/// </summary>
public class MapColorsTests
{
    [Theory]
    [InlineData("land")]
    [InlineData("desert")]
    [InlineData("forest")]
    [InlineData("lake")]
    [InlineData("glacier")]
    [InlineData("settlement")]
    [InlineData("lava")]
    [InlineData("plant")]
    [InlineData("ocean")]
    [InlineData("ink")]
    [InlineData("wateredge")]
    [InlineData("road")]
    [InlineData("devastation")]
    public void ColorsByCode_ContainsAllKnownCodes(string colorCode)
    {
        MapColors.ColorsByCode.Should().ContainKey(colorCode);
    }

    [Fact]
    public void GetColor_ValidCode_ReturnsColorWithFullAlpha()
    {
        var color = MapColors.GetColor("land");

        var alpha = (color >> 24) & 0xFF;
        alpha.Should().Be(0xFF);
    }

    [Fact]
    public void GetColor_InvalidCode_ReturnsLandColor()
    {
        var unknownColor = MapColors.GetColor("unknown");
        var landColor = MapColors.GetColor("land");

        unknownColor.Should().Be(landColor);
    }

    [Theory]
    [InlineData("#AC8858")]
    [InlineData("#C4A468")]
    [InlineData("#98844C")]
    public void ParseHexColor_ValidHex_ReturnsCorrectColor(string hex)
    {
        var color = MapColors.ParseHexColor(hex);

        // Should have full alpha
        var alpha = (color >> 24) & 0xFF;
        alpha.Should().Be(0xFF);
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
        var hexWithHash = "#AC8858";
        var hexWithoutHash = "AC8858";

        var colorWithHash = MapColors.ParseHexColor(hexWithHash);
        var colorWithoutHash = MapColors.ParseHexColor(hexWithoutHash);

        colorWithHash.Should().Be(colorWithoutHash);
    }

    [Fact]
    public void ApplyBrightness_IncreaseBrightness_LightensColor()
    {
        var originalColor = MapColors.GetColor("land");
        var originalR = (originalColor >> 16) & 0xFF;

        var brighterColor = MapColors.ApplyBrightness(originalColor, 1.5f);
        var brighterR = (brighterColor >> 16) & 0xFF;

        brighterR.Should().BeGreaterThan((uint)originalR);
    }

    [Fact]
    public void ApplyBrightness_DecreaseBrightness_DarkensColor()
    {
        var originalColor = MapColors.GetColor("land");
        var originalR = (originalColor >> 16) & 0xFF;

        var darkerColor = MapColors.ApplyBrightness(originalColor, 0.5f);
        var darkerR = (darkerColor >> 16) & 0xFF;

        darkerR.Should().BeLessThan((uint)originalR);
    }

    [Fact]
    public void ApplyBrightness_ClampsToMaximum()
    {
        var color = 0xFFFFFFFF; // White

        var result = MapColors.ApplyBrightness(color, 2.0f);

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
        var color = MapColors.GetColor("land");
        var originalAlpha = (color >> 24) & 0xFF;

        var modifiedColor = MapColors.ApplyBrightness(color, 1.5f);
        var modifiedAlpha = (modifiedColor >> 24) & 0xFF;

        modifiedAlpha.Should().Be((uint)originalAlpha);
    }

    [Fact]
    public void HexColorsByCode_AllColorsHaveValidHexFormat()
    {
        foreach (var (code, hex) in MapColors.HexColorsByCode)
        {
            hex.Should().StartWith("#", $"Color '{code}' should start with #");
            hex.Should().HaveLength(7, $"Color '{code}' should be #RRGGBB format");
        }
    }

    [Fact]
    public void GetColorIndex_ValidCode_ReturnsNonNegativeIndex()
    {
        var index = MapColors.GetColorIndex("land");
        index.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void GetColorByIndex_ValidIndex_ReturnsColor()
    {
        var landIndex = MapColors.GetColorIndex("land");
        var color = MapColors.GetColorByIndex(landIndex);

        color.Should().Be(MapColors.GetColor("land"));
    }

    [Fact]
    public void GetColorByIndex_InvalidIndex_ReturnsLandColor()
    {
        var color = MapColors.GetColorByIndex(-1);
        color.Should().Be(MapColors.GetColor("land"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("invalid")]
    [InlineData("#GGG")]
    public void ParseHexColor_InvalidHex_ReturnsDefaultColor(string invalidHex)
    {
        var color = MapColors.ParseHexColor(invalidHex);

        // Should return default land color
        color.Should().Be(0xFFAC8858u);
    }

    [Fact]
    public void ParseHexColor_CaseInsensitive_ParsesCorrectly()
    {
        var upperHex = "#AABBCC";
        var lowerHex = "#aabbcc";
        var mixedHex = "#AaBbCc";

        var upperColor = MapColors.ParseHexColor(upperHex);
        var lowerColor = MapColors.ParseHexColor(lowerHex);
        var mixedColor = MapColors.ParseHexColor(mixedHex);

        upperColor.Should().Be(lowerColor);
        lowerColor.Should().Be(mixedColor);
    }

    [Theory]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    [InlineData(1.5f)]
    public void ApplyBrightness_HeightFactorRange_ProducesValidColors(float brightnessFactor)
    {
        var baseColor = MapColors.GetColor("land");

        var result = MapColors.ApplyBrightness(baseColor, brightnessFactor);

        var alpha = (result >> 24) & 0xFF;
        var r = (result >> 16) & 0xFF;
        var g = (result >> 8) & 0xFF;
        var b = result & 0xFF;

        alpha.Should().Be(0xFF, "Alpha should be preserved");
        r.Should().BeInRange((uint)0, 255);
        g.Should().BeInRange((uint)0, 255);
        b.Should().BeInRange((uint)0, 255);
    }

    [Fact]
    public void ApplyBrightness_ZeroBrightness_ResultsInBlack()
    {
        var baseColor = MapColors.GetColor("land");

        var result = MapColors.ApplyBrightness(baseColor, 0f);

        var alpha = (result >> 24) & 0xFF;
        var r = (result >> 16) & 0xFF;
        var g = (result >> 8) & 0xFF;
        var b = result & 0xFF;

        alpha.Should().Be(0xFF);
        r.Should().Be(0);
        g.Should().Be(0);
        b.Should().Be(0);
    }

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
        var expectedColor = MapColors.ParseHexColor(expectedHex);
        var actualColor = MapColors.GetColor(code);

        actualColor.Should().Be(expectedColor);
    }
}
