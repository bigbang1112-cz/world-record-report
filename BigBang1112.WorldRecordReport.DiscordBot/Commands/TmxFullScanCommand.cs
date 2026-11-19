using BigBang1112.DiscordBot.Attributes;
using BigBang1112.DiscordBot.Models;
using BigBang1112.WorldRecordReportLib.Jobs;
using BigBang1112.WorldRecordReportLib.Services;
using Discord.WebSocket;
using Quartz;

namespace BigBang1112.WorldRecordReport.DiscordBot.Commands;

[DiscordBotCommand("tmxfullscan", "Runs a full refresh of the TMX leaderboards.")]
public class TmxFullScanCommand : DiscordBotCommand
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly TmxFullScanRequest _fullScanRequest;

    public TmxFullScanCommand(WrrDiscordBotService discordBotService,
                              ISchedulerFactory schedulerFactory,
                              TmxFullScanRequest fullScanRequest) : base(discordBotService)
    {
        _schedulerFactory = schedulerFactory;
        _fullScanRequest = fullScanRequest;
    }

    public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var jobKey = new JobKey(nameof(RefreshTmxOfficialJob));

        if (!await scheduler.CheckExists(jobKey))
        {
            return new DiscordBotMessage("The TMX refresh job is disabled.", ephemeral: true);
        }

        _fullScanRequest.Request();
        await scheduler.TriggerJob(jobKey);

        return new DiscordBotMessage("Full TMX scan queued. It will start when the TMX refresh job is free.", ephemeral: true);
    }
}
