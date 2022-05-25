using BigBang1112.WorldRecordReportLib.Repos;
using Xunit;

namespace BigBang1112.WorldRecordReportLib.Tests.Integration.Repos;

public class MapModeRepoTests
{
    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Arrange
        using var context = Fakes.CreateWrContext();

        // Act
        var repo = new MapModeRepo(context);

        // Assert
        Assert.NotNull(repo);
    }
}
