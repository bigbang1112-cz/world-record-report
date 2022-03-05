using BigBang1112;
using BigBang1112.TMWR;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Extensions;
using BigBang1112.WorldRecordReport.Jobs;
using BigBang1112.WorldRecordReportLib.Services;
using Quartz;
using BigBang1112.WorldRecordReportLib.Repos;

var assembly = typeof(Program).Assembly;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

var options = new EssentialsOptions
{
    Title = "World Record Report",
    Assembly = assembly,
    Config = config
};

// Add services to the container.
builder.Services.AddEssentials(options);

builder.Services.AddDbContext2<WrContext>(options.Config, "WrDb");

builder.Services.AddScoped<IWrRepo, WrRepo>();
builder.Services.AddScoped<IRecordSetService, RecordSetService>();
builder.Services.AddScoped<IDiscordWebhookService, DiscordWebhookService>();
builder.Services.AddScoped<ILeaderboardsManialinkService, LeaderboardsManialinkService>();
builder.Services.AddScoped<ITM2ReportService, TM2ReportService>();

builder.Services.AddScoped<WrAuthService>();
builder.Services.AddScoped<ITmxService, TmxService>();
builder.Services.AddScoped<TmxReportService>();

builder.Services.AddHostedService<TmwrDiscordBotService>();

builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    q.AddJobAndTrigger<RefreshTM2OfficialJob>(config);
    q.AddJobAndTrigger<RefreshTmxOfficialJob>(config);
    q.AddJobAndTrigger<CleanupTmxRemovedWorldRecordsJob>(config);
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
