using BigBang1112.WorldRecordReportLib.Models.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.DataEncryption;
using Microsoft.EntityFrameworkCore.DataEncryption.Providers;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace BigBang1112.WorldRecordReportLib.Data;

public class WrContext : DbContext
{
    private readonly IEncryptionProvider encryption;

    public DbSet<AssociatedAccountModel> AssociatedAccounts { get; set; } = default!;
    public DbSet<WorldRecordModel> WorldRecords { get; set; } = default!;
    public DbSet<MapModel> Maps { get; set; } = default!;
    public DbSet<GameModel> Games { get; set; } = default!;
    public DbSet<LoginModel> Logins { get; set; } = default!;
    public DbSet<TmxLoginModel> TmxLogins { get; set; } = default!;
    public DbSet<IgnoredLoginModel> IgnoredLogins { get; set; } = default!;
    public DbSet<IgnoredLoginFromMapModel> IgnoredLoginsFromMaps { get; set; } = default!;
    public DbSet<IgnoredLoginFromRemovedRecordReportModel> IgnoredLoginsFromRemovedRecordReport { get; internal set; } = default!;
    public DbSet<RefreshModel> Refreshes { get; set; } = default!;
    public DbSet<RefreshLoopModel> RefreshLoops { get; set; } = default!;
    public DbSet<ReportModel> Reports { get; set; } = default!;
    public DbSet<TitlePackModel> TitlePacks { get; set; } = default!;
    public DbSet<EnvModel> Environments { get; set; } = default!;
    public DbSet<AltReplayModel> AltReplays { get; set; } = default!;
    public DbSet<MapGroupModel> MapGroups { get; set; } = default!;
    public DbSet<DiscordWebhookModel> DiscordWebhooks { get; set; } = default!;
    public DbSet<DiscordWebhookMessageModel> DiscordWebhookMessages { get; set; } = default!;
    public DbSet<TmxSiteModel> TmxSites { get; set; } = default!;
    public DbSet<TmxInitModel> TmxInits { get; set; } = default!;
    public DbSet<MapModeModel> MapModes { get; set; } = default!;
    public DbSet<RecordChangeModel> RecordChanges { get; set; } = default!;
    public DbSet<RecordSetChangeModel> RecordSetChanges { get; set; } = default!;
    public DbSet<RecordSetDetailedChangeModel> RecordSetDetailedChanges { get; set; } = default!;
    public DbSet<RecordCountModel> RecordCounts { get; set; } = default!;
    public DbSet<NicknameChangeModel> NicknameChanges { get; set; } = default!;

    public WrContext(DbContextOptions<WrContext> options, IConfiguration config) : base(options)
    {
        var key = Encoding.ASCII.GetBytes(config["WrDbEncryptionKey"]);
        var iv = Encoding.ASCII.GetBytes(config["WrDbEncryptionIV"]);

        encryption = new AesProvider(key, iv);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLazyLoadingProxies();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseEncryption(encryption);

        modelBuilder.Entity<RefreshLoopModel>()
            .HasMany(x => x.Refreshes)
            .WithOne(x => x.RefreshLoop)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DiscordWebhookMessageModel>()
            .HasOne(x => x.Report)
            .WithMany(x => x.DiscordWebhookMessages)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<GameModel>().HasData(
            new GameModel { Id = 1, Name = NameConsts.GameTM2Name, DisplayName = NameConsts.GameTM2DisplayName },
            new GameModel { Id = 2, Name = NameConsts.GameTMUFName, DisplayName = NameConsts.GameTMUFDisplayName },
            new GameModel { Id = 3, Name = NameConsts.GameTMNFName, DisplayName = NameConsts.GameTMNFDisplayName },
            new GameModel { Id = 4, Name = NameConsts.GameTMUName, DisplayName = NameConsts.GameTMUDisplayName },
            new GameModel { Id = 5, Name = NameConsts.GameTMSName, DisplayName = NameConsts.GameTMSDisplayName },
            new GameModel { Id = 6, Name = NameConsts.GameTMNName, DisplayName = NameConsts.GameTMNDisplayName },
            new GameModel { Id = 7, Name = NameConsts.GameTMOName, DisplayName = NameConsts.GameTMODisplayName }
            );

        modelBuilder.Entity<EnvModel>().HasData(
            new EnvModel { Id = 1, Name = "Desert", Name2 = "Speed", Color = OfficialColors.EnvDesert },
            new EnvModel { Id = 2, Name = "Snow", Name2 = "Alpine", Color = OfficialColors.EnvSnow },
            new EnvModel { Id = 3, Name = "Rally", Color = OfficialColors.EnvRally },
            new EnvModel { Id = 4, Name = "Island", Color = OfficialColors.EnvIsland },
            new EnvModel { Id = 5, Name = "Bay", Color = OfficialColors.EnvBay },
            new EnvModel { Id = 6, Name = "Coast", Color = OfficialColors.EnvCoast },
            new EnvModel { Id = 7, Name = "Stadium", Color = OfficialColors.EnvStadium },
            new EnvModel { Id = 8, Name = "Canyon", Color = OfficialColors.EnvCanyon },
            new EnvModel { Id = 9, Name = "Valley", Color = OfficialColors.EnvValley },
            new EnvModel { Id = 10, Name = "Lagoon", Color = OfficialColors.EnvLagoon },
            new EnvModel { Id = 11, Name = "Stadium2020", DisplayName = "Stadium 2020", Color = OfficialColors.EnvStadium2020 });

        modelBuilder.Entity<TmxSiteModel>().HasData(
            new TmxSiteModel { Id = 1, ShortName = NameConsts.TMXSiteNations, Url = "http://nations.tm-exchange.com/" },
            new TmxSiteModel { Id = 2, ShortName = NameConsts.TMXSiteUnited, Url = "https://united.tm-exchange.com/" },
            new TmxSiteModel { Id = 3, ShortName = NameConsts.TMXSiteTMNF, Url = "https://tmnforever.tm-exchange.com/" },
            new TmxSiteModel { Id = 4, ShortName = NameConsts.TMXSiteTM2, Url = "https://tm.mania-exchange.com/" },
            new TmxSiteModel { Id = 5, ShortName = NameConsts.TMXSiteTrackmania, Url = "https://trackmania.exchange/" });

        modelBuilder.Entity<MapModeModel>().HasData(
            new MapModeModel { Id = 1, Name = NameConsts.MapModeRace },
            new MapModeModel { Id = 2, Name = NameConsts.MapModeStunts }
            );

        modelBuilder.Entity<RecordSetDetailedChangeModel>()
            .Property(e => e.Type)
            .HasConversion<int>();
    }
}
