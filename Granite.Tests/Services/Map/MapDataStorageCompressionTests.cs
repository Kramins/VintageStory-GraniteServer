using FluentAssertions;
using Granite.Server.Services.Map;
using Xunit;

namespace Granite.Tests.Services.Map;

/// <summary>
/// Tests for byte conversion methods in MapDataStorageService.
/// </summary>
public class MapDataStorageByteConversionTests
{
    [Fact]
    public void BlockIdsToBytes_AndBytesToBlockIds_RoundTrips()
    {
        // Arrange
        var original = new int[1024];
        var random = new Random(42);
        for (var i = 0; i < 1024; i++)
        {
            original[i] = random.Next(0, 10000);
        }

        // Act
        var bytes = MapDataStorageService.BlockIdsToBytes(original);
        var restored = MapDataStorageService.BytesToBlockIds(bytes);

        // Assert
        restored.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void BlockIdsToBytes_ProducesCorrectSize()
    {
        // Arrange - 1024 ints = 4096 bytes
        var original = new int[1024];

        // Act
        var bytes = MapDataStorageService.BlockIdsToBytes(original);

        // Assert
        bytes.Length.Should().Be(4096);
    }

    [Fact]
    public void BlockIdsToBytes_EmptyArray_Works()
    {
        // Arrange
        var original = new int[0];

        // Act
        var bytes = MapDataStorageService.BlockIdsToBytes(original);
        var restored = MapDataStorageService.BytesToBlockIds(bytes);

        // Assert
        restored.Should().BeEmpty();
    }

    [Fact]
    public void BlockIdsToBytes_PreservesExtremeValues()
    {
        // Arrange
        var original = new int[] { 0, int.MaxValue, int.MinValue, -1, 1 };

        // Act
        var bytes = MapDataStorageService.BlockIdsToBytes(original);
        var restored = MapDataStorageService.BytesToBlockIds(bytes);

        // Assert
        restored.Should().BeEquivalentTo(original);
    }
}
