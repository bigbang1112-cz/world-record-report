using AutoBogus;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Bogus;
using Xunit;

namespace BigBang1112.WorldRecordReportLib.Tests.Integration.Repos;

public class MapRepoTests
{
    [Fact]
    public async void GetByUidAsync_ReturnsMap()
    {
        // Arrange
        var maps = new Faker<MapModel>()
            .RuleFor(x => x.MapUid, f => f.Internet.Password(28))
            .RuleFor(x => x.Name, f => f.Name.JobArea())
            .RuleFor(x => x.DeformattedName, f => f.Name.JobArea())
            .Generate(5);
        var expectedMap = maps[1];

        using var context = Fakes.CreateWrContext(context =>
        {
            foreach (var map in maps)
            {
                context.Maps.Add(map);
            }
        });

        var repo = new MapRepo(context);

        // Act
        var actualMap = await repo.GetByUidAsync(expectedMap.MapUid!);

        // Assert
        Assert.NotNull(actualMap);

        foreach (var property in typeof(MapModel).GetProperties())
        {
            Assert.Equal(property.GetValue(expectedMap), property.GetValue(actualMap));
        }
    }
}
