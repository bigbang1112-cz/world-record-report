using System.Net;
using System.Text.Json;
using BigBang1112.WorldRecordReportLib.Jobs;
using BigBang1112.WorldRecordReportLib.Services.Wrappers;
using BigBang1112.WorldRecordReportLib.Tests.Mocks;
using ManiaAPI.Base.Converters;
using ManiaAPI.TrackmaniaIO;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BigBang1112.WorldRecordReportLib.Tests.Unit.Jobs;

public class AcquireNewOfficialCampaignsJobTests
{
    // test AcquireNewOfficialCampaignsAsync

    // test AcquireNewOfficialCampaignsAsync_WhenThereAreNoNewCampaigns

    // test AcquireNewOfficialCampaignsAsync_WhenThereAreNewCampaigns
    [Fact]
    public async Task AcquireNewOfficialCampaignsAsync()
    {
        // Arrange
        var mockUnitOfWork = new MockWrUnitOfWork();

        var mockTmIo = new Mock<ITrackmaniaIoApiService>();
        mockTmIo.Setup(x => x.GetCampaignsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new CampaignCollection(new CampaignItem[]
        {
            new OfficialCampaignItem(id: 22874, "Spring 2022", DateTimeOffset.FromUnixTimeSeconds(1648825200), 25),
            new OfficialCampaignItem(id: 18729, "Winter 2022", DateTimeOffset.FromUnixTimeSeconds(1641052800), 25),
            new OfficialCampaignItem(id: 16056, "Fall 2021", DateTimeOffset.FromUnixTimeSeconds(1633100400), 25)
        }, 0));

        mockTmIo.Setup(x => x.GetOfficialCampaignAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((int id, CancellationToken cancellationToken) => new Campaign
        {
            Id = id,
            Name = id switch
            {
                22874 => "Spring 2022",
                18729 => "Winter 2022",
                16056 => "Fall 2021",
                _ => throw new ArgumentOutOfRangeException(nameof(id))
            },
            LeaderboardUid = id switch
            {
                22874 => "fab938e9-05e3-4c09-856f-62f10579a243",
                18729 => "cd7facc2-3cee-45cf-b7c6-816da8e99db4",
                16056 => "45569279-a101-446d-b5d6-649471deadcf",
                _ => throw new ArgumentOutOfRangeException(nameof(id))
            },
            Playlist = id switch
            {
                22874 => CreatePlaylistSpring2022(),
                _ => Array.Empty<Map>()
            }
        });

        var http = new MockHttpClient((request, cancellationToken) =>
        {
            if (request.RequestUri is null)
            {
                throw new ArgumentNullException(nameof(request.RequestUri));
            }

            if (request.RequestUri.AbsoluteUri.StartsWith("https://prod.trackmania.core.nadeo.online/storageObjects/"))
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                };
            }

            throw new Exception("Unknown request");
        });

        var logger = Mock.Of<ILogger<AcquireNewOfficialCampaignsJob>>();

        var job = new AcquireNewOfficialCampaignsJob(mockUnitOfWork, mockTmIo.Object, http, logger);

        // Act
        await job.AcquireNewOfficialCampaignsAsync(0);

        // Assert
    }

    private static Map[] CreatePlaylistSpring2022()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new TimeInt32Converter());
        return JsonSerializer.Deserialize<Map[]>(File.ReadAllText("Data/PlaylistSpring2022.json"), options) ?? throw new Exception();
    }

    // test AcquireNewOfficialCampaignsAsync_WhenThereAreNewCampaignsAndThereIsAnError

}
