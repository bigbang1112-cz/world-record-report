﻿using BigBang1112.Exceptions;
using BigBang1112.WorldRecordReportLib.Attributes;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Exceptions;
using BigBang1112.WorldRecordReportLib.Models.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.DataEncryption;
using Microsoft.EntityFrameworkCore.DataEncryption.Providers;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Text;

using ThisShouldNotHappenException = BigBang1112.WorldRecordReportLib.Exceptions.ThisShouldNotHappenException;

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
    public DbSet<RecordCountModel> RecordCounts2 { get; set; } = default!;
    public DbSet<NicknameChangeModel> NicknameChanges { get; set; } = default!;
    public DbSet<CampaignModel> Campaigns { get; set; } = default!;

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

        modelBuilder.Entity<GameModel>().HasEnumData<GameModel, Game, GameAttribute>(att => new GameModel
        {
            Name = att.Name,
            DisplayName = att.DisplayName
        });

        modelBuilder.Entity<EnvModel>().HasEnumData<EnvModel, Env, EnvAttribute>(att => new EnvModel
        {
            Name = att.Name,
            Name2 = att.Name2,
            DisplayName = att.DisplayName,
            Color = new byte[] { att.ColorR, att.ColorG, att.ColorB }
        });

        modelBuilder.Entity<TmxSiteModel>().HasEnumData<TmxSiteModel, TmxSite, TmxSiteAttribute>(att => new TmxSiteModel
        {
            ShortName = att.ShortName,
            Url = att.Url
        });

        modelBuilder.Entity<MapModeModel>().HasEnumData<MapModeModel, MapMode, MapModeAttribute>(att => new MapModeModel
        {
            Name = att.Name
        });

        modelBuilder.Entity<RecordSetDetailedChangeModel>()
            .Property(e => e.Type)
            .HasConversion<int>();
    }
}
