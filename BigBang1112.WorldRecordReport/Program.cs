using BigBang1112;
using BigBang1112.TMWR;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Extensions;
using BigBang1112.WorldRecordReportLib.Jobs;
using BigBang1112.WorldRecordReportLib.Services;
using Quartz;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.DiscordBot.Data;
using System.Globalization;
using BigBang1112.WorldRecordReportLib;
using BigBang1112.WorldRecordReportLib.Services.Wrappers;
using ManiaAPI.NadeoAPI;
using ManiaAPI.TrackmaniaAPI;
using BigBang1112.WorldRecordReport.DiscordBot;

var cultureInfo = (CultureInfo)CultureInfo.InvariantCulture.Clone();
cultureInfo.NumberFormat.NumberGroupSeparator = " ";

CultureInfo.CurrentCulture = cultureInfo;

var assembly = typeof(Program).Assembly;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseEssentials();

var config = builder.Configuration;

var options = new EssentialsOptions
{
    Title = "World Record Report",
    Assembly = assembly,
    Config = config,
    Mapper = new WrMapper()
};

// Add services to the container.
builder.Services.AddEssentials(options);

builder.Services.AddDbContext2<WrContext>(options.Config, "WrDb");
builder.Services.AddDbContext2<DiscordBotContext>(options.Config, "DiscordBotDb");

builder.Services.AddScoped<IWrUnitOfWork, WrUnitOfWork>();

builder.Services.AddScoped<IWrRepo, WrRepo>();
builder.Services.AddScoped<IDiscordBotRepo, DiscordBotRepo>();
builder.Services.AddScoped<ICampaignRepo, CampaignRepo>();
builder.Services.AddScoped<IGameRepo, GameRepo>();
builder.Services.AddScoped<IMapRepo, MapRepo>();
builder.Services.AddScoped<ILoginRepo, LoginRepo>();

builder.Services.AddScoped<IRecordSetService, RecordSetService>();
builder.Services.AddScoped<IDiscordWebhookService, DiscordWebhookService>();
builder.Services.AddScoped<ILeaderboardsManialinkService, LeaderboardsManialinkService>();
builder.Services.AddScoped<ITM2ReportService, TM2ReportService>();
//builder.Services.AddScoped<TM2020ReportService>();

builder.Services.AddScoped<WrAuthService>();
builder.Services.AddScoped<ITmxService, TmxService>();
builder.Services.AddScoped<TmxReportService>();
builder.Services.AddScoped<ITmxRecordSetService, TmxRecordSetService>();

builder.Services.AddScoped<IGhostService, GhostService>();
builder.Services.AddScoped<ReportService>();

builder.Services.AddSingleton<NadeoServices>();
builder.Services.AddSingleton<NadeoLiveServices>();
builder.Services.AddSingleton<TrackmaniaAPI>();
builder.Services.AddSingleton<INadeoApiService, NadeoApiService>();
builder.Services.AddHostedService(x => x.GetRequiredService<INadeoApiService>());
builder.Services.AddSingleton<ITrackmaniaApiService, TrackmaniaApiService>();
builder.Services.AddHostedService(x => x.GetRequiredService<ITrackmaniaApiService>());
builder.Services.AddSingleton<ITrackmaniaIoApiService, TrackmaniaIoApiService>();

builder.Services.AddSingleton<RefreshScheduleService>();
builder.Services.AddSingleton<RecordStorageService>();

builder.Services.AddSingleton<TmwrDiscordBotService>();
builder.Services.AddHostedService(x => x.GetRequiredService<TmwrDiscordBotService>());
builder.Services.AddSingleton<WrrDiscordBotService>();
builder.Services.AddHostedService(x => x.GetRequiredService<WrrDiscordBotService>());

builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    q.AddIntervalTrigger<RefreshTM2020OfficialJob>(config);
    q.AddIntervalTrigger<RefreshTM2OfficialJob>(config);
    q.AddIntervalTrigger<RefreshTmxOfficialJob>(config);
    q.AddIntervalTrigger<CleanupTmxRemovedWorldRecordsJob>(config);
    q.AddDailyTrigger<AcquireNewOfficialCampaignsJob>(TimeOfDay.HourAndMinuteOfDay(17, 2), config, timezone: TimeZoneInfo.Local);
});

// ASP.NET Core hosting
builder.Services.AddQuartzServer(options =>
{
    // when shutting down we want jobs to complete gracefully
    options.WaitForJobsToComplete = true;
});

var app = builder.Build();

app.UseEssentials(options);

app.Run();
