using BigBang1112.Exceptions;
using BigBang1112.WorldRecordReportLib.Attributes;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Exceptions;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Models.ReportScopes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SoftFluent.EntityFrameworkCore.DataEncryption;
using SoftFluent.EntityFrameworkCore.DataEncryption.Providers;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace BigBang1112.WorldRecordReportLib.Data;

public class WrContext : DbContext
{
    private readonly IEncryptionProvider encryption;

    public virtual DbSet<AssociatedAccountModel> AssociatedAccounts { get; set; } = default!;
    public virtual DbSet<WorldRecordModel> WorldRecords { get; set; } = default!;
    public virtual DbSet<MapModel> Maps { get; set; } = default!;
    public virtual DbSet<GameModel> Games { get; set; } = default!;
    public virtual DbSet<LoginModel> Logins { get; set; } = default!;
    public virtual DbSet<TmxLoginModel> TmxLogins { get; set; } = default!;
    public virtual DbSet<IgnoredLoginModel> IgnoredLogins { get; set; } = default!;
    public virtual DbSet<IgnoredLoginFromMapModel> IgnoredLoginsFromMaps { get; set; } = default!;
    public virtual DbSet<IgnoredLoginFromRemovedRecordReportModel> IgnoredLoginsFromRemovedRecordReport { get; internal set; } = default!;
    public virtual DbSet<RefreshModel> Refreshes { get; set; } = default!;
    public virtual DbSet<RefreshLoopModel> RefreshLoops { get; set; } = default!;
    public virtual DbSet<ReportModel> Reports { get; set; } = default!;
    public virtual DbSet<TitlePackModel> TitlePacks { get; set; } = default!;
    public virtual DbSet<EnvModel> Environments { get; set; } = default!;
    public virtual DbSet<AltReplayModel> AltReplays { get; set; } = default!;
    public virtual DbSet<MapGroupModel> MapGroups { get; set; } = default!;
    public virtual DbSet<DiscordWebhookModel> DiscordWebhooks { get; set; } = default!;
    public virtual DbSet<DiscordWebhookMessageModel> DiscordWebhookMessages { get; set; } = default!;
    public virtual DbSet<TmxSiteModel> TmxSites { get; set; } = default!;
    public virtual DbSet<TmxInitModel> TmxInits { get; set; } = default!;
    public virtual DbSet<MapModeModel> MapModes { get; set; } = default!;
    public virtual DbSet<RecordChangeModel> RecordChanges { get; set; } = default!;
    public virtual DbSet<RecordSetChangeModel> RecordSetChanges { get; set; } = default!;
    public virtual DbSet<RecordSetDetailedChangeModel> RecordSetDetailedChanges { get; set; } = default!;
    public virtual DbSet<RecordCountModel> RecordCounts2 { get; set; } = default!;
    public virtual DbSet<NicknameChangeModel> NicknameChanges { get; set; } = default!;
    public virtual DbSet<CampaignModel> Campaigns { get; set; } = default!;

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

        modelBuilder.Entity<GameModel>().HasEnumData<GameModel, Game, GameAttribute>(WrEnumData.GameAttributeToModel);
        modelBuilder.Entity<EnvModel>().HasEnumData<EnvModel, Env, EnvAttribute>(WrEnumData.EnvAttributeToModel);
        modelBuilder.Entity<TmxSiteModel>().HasEnumData<TmxSiteModel, TmxSite, TmxSiteAttribute>(WrEnumData.TmxSiteAttributeToModel);
        modelBuilder.Entity<MapModeModel>().HasEnumData<MapModeModel, MapMode, MapModeAttribute>(WrEnumData.MapModeAttributeToModel);

        modelBuilder.Entity<RefreshLoopModel>()
            .HasMany(x => x.Refreshes)
            .WithOne(x => x.RefreshLoop)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DiscordWebhookMessageModel>()
            .HasOne(x => x.Report)
            .WithMany(x => x.DiscordWebhookMessages)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<RecordSetDetailedChangeModel>()
            .Property(e => e.Type)
            .HasConversion<int>();

        modelBuilder.Entity<DiscordWebhookModel>()
            .Property(e => e.Scope)
            .HasConversion(
                x => JsonSerializer.Serialize(x, ReportScopeSet.JsonSerializerOptions),
                x => JsonSerializer.Deserialize<ReportScopeSet>(x, ReportScopeSet.JsonSerializerOptions));

        modelBuilder.Entity<MapModel>()
            .Property(e => e.LastRefreshedOn)
            .HasColumnType("text")
            .HasConversion(
                x => JsonSerializer.Serialize(x, ReportScopeSet.JsonSerializerOptions),
                x => JsonSerializer.Deserialize<ScoreContextValue<DateTimeOffset>>(x, ReportScopeSet.JsonSerializerOptions));
    }
}
