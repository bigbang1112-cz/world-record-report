using System.Collections.Immutable;
using BigBang1112.Services;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Services;
using ManiaAPI.TMX;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using TmxSite = BigBang1112.WorldRecordReportLib.Enums.TmxSite;
using ApiSite = ManiaAPI.TMX.TmxSite;

namespace BigBang1112.WorldRecordReportLib.Tests.Unit.Services;

public class RefreshTmxServiceTests
{
    [Theory]
    [InlineData(TmxSite.United, ApiSite.TMUF, Game.TMUF, ManiaAPI.TMX.Environment.Desert, Env.Desert, TrackType.Race, MapMode.Race)]
    [InlineData(TmxSite.TMNF, ApiSite.TMNF, Game.TMUF, ManiaAPI.TMX.Environment.Stadium, Env.Stadium, TrackType.Race, MapMode.Race)]
    [InlineData(TmxSite.Nations, ApiSite.Nations, Game.TMN, ManiaAPI.TMX.Environment.Stadium, Env.Stadium, TrackType.Race, MapMode.Race)]
    [InlineData(TmxSite.United, ApiSite.TMUF, Game.TMUF, ManiaAPI.TMX.Environment.Coast, Env.Coast, TrackType.Stunts, MapMode.Stunts)]
    public async Task FullClassicScan_AddsMissingTrackOnce_EvenWithoutReplays(TmxSite site, ApiSite apiSite, Game game,
        ManiaAPI.TMX.Environment environment, Env expectedEnvironment, TrackType type, MapMode expectedMode)
    {
        var fixture = new Fixture();
        var track = fixture.Track with { Environment = environment, PrimaryType = type };
        fixture.SetTracks(track);
        var author = new LoginModel { Name = "map-author", Nickname = "Existing nickname" };
        fixture.UnitOfWork.Setup(x => x.Logins.GetByNameAsync(game, "map-author", default)).ReturnsAsync(author);
        fixture.UnitOfWork.Setup(x => x.Games.GetAsync(game, default)).ReturnsAsync(new GameModel { Id = (int)game });
        fixture.UnitOfWork.Setup(x => x.Envs.GetAsync(expectedEnvironment, default)).ReturnsAsync(new EnvModel { Id = (int)expectedEnvironment });
        fixture.UnitOfWork.Setup(x => x.MapModes.GetAsync(expectedMode, default)).ReturnsAsync(new MapModeModel { Id = (int)expectedMode });
        fixture.UnitOfWork.Setup(x => x.TmxLogins.GetOrAddAsync(42, site, default))
            .ReturnsAsync(new TmxLoginModel { UserId = 42, Site = new TmxSiteModel { Id = (int)site } });
        await fixture.Service.UpdateWorldRecordsAsync(site, LeaderboardType.Classic, "Classic", fullScan: true);
        await fixture.Service.UpdateWorldRecordsAsync(site, LeaderboardType.Classic, "Classic", fullScan: true);

        var savedMap = Assert.Single(fixture.Maps);
        Assert.Equal(track.UId, savedMap.MapUid);
        Assert.Equal(checked((int)track.TrackId), savedMap.MxId);
        Assert.Equal("Classic track", savedMap.DeformattedName);
        Assert.Equal((int)game, savedMap.Game.Id);
        Assert.Equal((int)expectedEnvironment, savedMap.Environment.Id);
        Assert.Equal((int)expectedMode, savedMap.Mode!.Id);
        Assert.Same(author, savedMap.Author);
        Assert.Equal("Existing nickname", savedMap.Author.Nickname);
        Assert.Equal("Uploader", savedMap.TmxAuthor!.Nickname);
        fixture.UnitOfWork.Verify(x => x.Maps.AddAsync(It.IsAny<MapModel>(), default), Times.Once);
        fixture.UnitOfWork.Verify(x => x.SaveAsync(default), Times.Once);
        fixture.Tmx.Verify(x => x.GetTrackAuthorLoginAsync(apiSite, track.TrackId, default), Times.Once);
        fixture.Tmx.Verify(x => x.GetReplaysAsync(apiSite, track.TrackId, default), Times.Exactly(2));
    }

    [Fact]
    public async Task FailedClassicImport_IsRetriedDuringNextRegularScan()
    {
        var fixture = new Fixture();
        fixture.SetTracks(fixture.Track);
        fixture.Tmx.SetupSequence(x => x.GetTrackAuthorLoginAsync(ApiSite.TMUF, fixture.Track.TrackId, default))
            .ThrowsAsync(new HttpRequestException("Temporary download failure"))
            .ReturnsAsync("map-author");

        await fixture.Service.UpdateWorldRecordsAsync(TmxSite.United, LeaderboardType.Classic, "Classic", fullScan: true);
        Assert.Empty(fixture.Maps);
        fixture.SetTracks();

        await fixture.Service.UpdateWorldRecordsAsync(TmxSite.United, LeaderboardType.Classic, "Classic");

        var map = Assert.Single(fixture.Maps);
        Assert.Equal(fixture.Track.UId, map.MapUid);
        Assert.Equal("map-author", map.Author.Name);
        fixture.UnitOfWork.Verify(x => x.SaveAsync(default), Times.Once);
    }

    [Fact]
    public async Task FullClassicScan_ImportsMissingTrackOnLaterPage()
    {
        var fixture = new Fixture();
        var existing = new MapModel
        {
            MxId = 1, MapUid = "existing-map", LastActivityOn = DateTime.UtcNow,
            TmxAuthor = new TmxLoginModel { Site = new TmxSiteModel { Id = (int)TmxSite.United } }
        };
        fixture.Maps.Add(existing);
        fixture.Tmx.SetupSequence(x => x.SearchAsync(ApiSite.TMUF, It.IsAny<TMX.SearchTracksParameters>(), default))
            .ReturnsAsync(new ItemCollection<TrackItem>
            {
                HasMoreItems = true,
                Results = ImmutableList.Create(fixture.Track with { TrackId = 1, ActivityAt = DateTimeOffset.MinValue })
            })
            .ReturnsAsync(new ItemCollection<TrackItem> { Results = ImmutableList.Create(fixture.Track) });

        await fixture.Service.UpdateWorldRecordsAsync(TmxSite.United, LeaderboardType.Classic, "Classic", fullScan: true);

        Assert.Equal(2, fixture.Maps.Count);
        Assert.Contains(fixture.Maps, map => map.MapUid == fixture.Track.UId);
        fixture.Tmx.Verify(x => x.SearchAsync(ApiSite.TMUF,
            It.Is<TMX.SearchTracksParameters>(p => p.After == 1), default), Times.Once);
    }

    [Theory]
    [InlineData(false, LeaderboardType.Classic)]
    [InlineData(true, LeaderboardType.Nadeo)]
    [InlineData(true, LeaderboardType.Star)]
    public async Task OtherScans_DoNotImportMissingTracks(bool fullScan, LeaderboardType leaderboard)
    {
        var fixture = new Fixture();
        fixture.SetTracks(fixture.Track);

        await fixture.Service.UpdateWorldRecordsAsync(TmxSite.United, leaderboard, "Test", fullScan);

        fixture.UnitOfWork.Verify(x => x.Maps.AddAsync(It.IsAny<MapModel>(), default), Times.Never);
        fixture.Tmx.Verify(x => x.GetTrackAuthorLoginAsync(It.IsAny<ApiSite>(), It.IsAny<long>(), default), Times.Never);
    }

    private sealed class Fixture
    {
        public Mock<IWrUnitOfWork> UnitOfWork { get; } = new() { DefaultValue = DefaultValue.Mock };
        public Mock<ITmxService> Tmx { get; } = new();
        public List<MapModel> Maps { get; } = [];
        public TrackItem Track { get; } = new()
        {
            TrackId = 123,
            UId = "classic-map-uid",
            TrackName = "$f00Classic track",
            Uploader = new User(42, "Uploader"),
            Environment = ManiaAPI.TMX.Environment.Stadium,
            PrimaryType = TrackType.Race
        };
        public RefreshTmxService Service { get; }

        public Fixture()
        {
            UnitOfWork.Setup(x => x.Maps.GetByMxIdAsync(It.IsAny<long>(), It.IsAny<TmxSite>(), default))
                .ReturnsAsync((long id, TmxSite site, CancellationToken _) =>
                    Maps.SingleOrDefault(map => map.MxId == id && map.TmxAuthor!.Site.Id == (int)site));
            UnitOfWork.Setup(x => x.Maps.AddAsync(It.IsAny<MapModel>(), default))
                .Callback<MapModel, CancellationToken>((map, _) => Maps.Add(map)).Returns(Task.CompletedTask);
            UnitOfWork.Setup(x => x.Games.GetAsync(It.IsAny<Game>(), default))
                .ReturnsAsync((Game game, CancellationToken _) => new GameModel { Id = (int)game });
            UnitOfWork.Setup(x => x.Envs.GetAsync(It.IsAny<Env>(), default))
                .ReturnsAsync((Env environment, CancellationToken _) => new EnvModel { Id = (int)environment });
            UnitOfWork.Setup(x => x.MapModes.GetAsync(It.IsAny<MapMode>(), default))
                .ReturnsAsync((MapMode mode, CancellationToken _) => new MapModeModel { Id = (int)mode });
            UnitOfWork.Setup(x => x.TmxLogins.GetOrAddAsync(It.IsAny<int>(), It.IsAny<TmxSite>(), default))
                .ReturnsAsync((int id, TmxSite site, CancellationToken _) => new TmxLoginModel
                {
                    UserId = id, Site = new TmxSiteModel { Id = (int)site }
                });
            UnitOfWork.Setup(x => x.Logins.GetByNameAsync(It.IsAny<Game>(), It.IsAny<string>(), default))
                .ReturnsAsync((LoginModel?)null);
            UnitOfWork.Setup(x => x.Logins.GetOrAddAsync(It.IsAny<Game>(), It.IsAny<string>(), It.IsAny<string>(), default))
                .ReturnsAsync((Game game, string name, string nickname, CancellationToken _) =>
                    new LoginModel { Name = name, Nickname = nickname });
            Tmx.Setup(x => x.GetTrackAuthorLoginAsync(It.IsAny<ApiSite>(), It.IsAny<long>(), default)).ReturnsAsync("map-author");
            Tmx.Setup(x => x.GetReplaysAsync(It.IsAny<ApiSite>(), It.IsAny<long>(), default))
                .ReturnsAsync(new ItemCollection<ReplayItem> { Results = ImmutableList<ReplayItem>.Empty });
            Service = new RefreshTmxService(Tmx.Object, UnitOfWork.Object,
                new RecordStorageService(Mock.Of<IFileHostService>()), null!, new TmxFailedTrackQueue(),
                new TmxFullScanRequest(), NullLogger<RefreshTmxService>.Instance);
        }

        public void SetTracks(params TrackItem[] tracks) => Tmx
            .Setup(x => x.SearchAsync(It.IsAny<ApiSite>(), It.IsAny<TMX.SearchTracksParameters>(), default))
            .ReturnsAsync(new ItemCollection<TrackItem> { Results = tracks.ToImmutableList() });
    }
}
