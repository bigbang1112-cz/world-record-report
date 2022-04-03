using AutoBogus;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Xunit;

namespace BigBang1112.WorldRecordReportLib.Tests.Integration.Repos;

public class CampaignRepoTests
{
    [Fact]
    public async void GetByLeaderboardUidAsync_Returns_Campaign()
    {
        // Arrange
        var campaigns = AutoFaker.Generate<CampaignModel>(5);
        var expectedCampaign = campaigns[1];

        using var context = Fakes.CreateWrContext(context =>
        {
            foreach (var campaign in campaigns)
            {
                context.Campaigns.Add(campaign);
            }
        });

        var repo = new CampaignRepo(context);

        // Act
        var actualCampaign = await repo.GetByLeaderboardUidAsync(expectedCampaign.LeaderboardUid!);

        // Assert
        Assert.NotNull(actualCampaign);

        foreach (var property in typeof(CampaignModel).GetProperties())
        {
            Assert.Equal(property.GetValue(expectedCampaign), property.GetValue(actualCampaign));
        }
    }
}
