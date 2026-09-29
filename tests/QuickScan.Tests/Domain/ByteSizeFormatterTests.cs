using QuickScan.Domain.Common;
using Xunit;

namespace QuickScan.Tests.Domain;

public sealed class ByteSizeFormatterTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(-5, "0 B")]
    [InlineData(500, "500 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1.00 KB")]
    [InlineData(1536, "1.50 KB")]
    [InlineData(1048575, "1.00 MB")] // 1023.999 KB rounds to 1.00 MB
    [InlineData(1048576, "1.00 MB")]
    [InlineData(1073741823, "1.00 GB")] // 1023.999 MB rounds to 1.00 GB
    [InlineData(1073741824, "1.00 GB")]
    [InlineData(1099511627776, "1.00 TB")]
    public void Format_GivenByteCount_ReturnsExpectedUnitString(long bytes, string expected)
    {
        // Arrange & Act
        var result = ByteSizeFormatter.Format(bytes);

        // Assert
        Assert.Equal(expected, result);
    }
}