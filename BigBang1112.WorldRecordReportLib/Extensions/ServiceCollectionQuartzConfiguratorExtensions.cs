using Microsoft.Extensions.Configuration;
using Quartz;
using System.Globalization;

namespace BigBang1112.WorldRecordReportLib.Extensions;

public static class ServiceCollectionQuartzConfiguratorExtensions
{
    private const string ContainerKey = "Schedule";

    public static void AddIntervalTrigger<T>(this IServiceCollectionQuartzConfigurator quartz,
        IConfiguration config, TimeSpan offset = default) where T : IJob
    {
        var jobName = typeof(T).Name;

        var enabled = config.GetValue<bool>($"{ContainerKey}:{jobName}:Enabled");

        if (!enabled)
        {
            return;
        }

        var timeStr = config[$"{ContainerKey}:{jobName}:Interval"];

        if (string.IsNullOrEmpty(timeStr))
        {
            throw new Exception($"No schedule found for job in configuration at '{jobName}'");
        }

        var time = ParseTime(jobName, timeStr);

        //if(!int.TryParse(timeStr, out int time))
        //    throw new Exception($"Schedule time for '{jobName}' is in invalid format.");

        var jobKey = new JobKey(jobName);
        quartz.AddJob<T>(opts => opts.WithIdentity(jobKey));

        quartz.AddTrigger(opts => opts
            .ForJob(jobKey)
            .WithIdentity(jobName + "-trigger")
            .WithSimpleSchedule(x => x
                .WithInterval(time)
                .RepeatForever())
            .StartAt(DateTimeOffset.Now + offset + TimeSpan.FromSeconds(10)));

        //quartz.AddTriggerListener<RefreshTriggerListener>();
    }

    public static void AddDailyTrigger<T>(this IServiceCollectionQuartzConfigurator quartz, TimeOfDay timeOfDay,
        IConfiguration config, TimeSpan offset = default) where T : IJob
    {
        var jobName = typeof(T).Name;

        var jobKey = new JobKey(jobName);
        quartz.AddJob<T>(opts => opts.WithIdentity(jobKey));

        quartz.AddTrigger(opts => opts
            .ForJob(jobKey)
            .WithIdentity(jobName + "-trigger-first")
            .WithSimpleSchedule(builder => builder.WithRepeatCount(0))
            .StartAt(DateTimeOffset.Now + offset + TimeSpan.FromSeconds(10)));
        
        quartz.AddTrigger(opts => opts
            .ForJob(jobKey)
            .WithIdentity(jobName + "-trigger")
            .WithDailyTimeIntervalSchedule(builder => builder.WithIntervalInHours(24)
                .OnEveryDay()
                .StartingDailyAt(timeOfDay)
                .InTimeZone(TimeZoneInfo.Local)
            )
            .StartAt(DateTimeOffset.Now + offset + TimeSpan.FromSeconds(10)));
    }

    private static TimeSpan ParseTime(string jobName, string timeStr)
    {
        if (TimeSpan.TryParseExact(timeStr, "s's'", CultureInfo.InvariantCulture, out TimeSpan time)
         || TimeSpan.TryParseExact(timeStr, "m'm'", CultureInfo.InvariantCulture, out time)
         || TimeSpan.TryParseExact(timeStr, "h'h'", CultureInfo.InvariantCulture, out time)
         || TimeSpan.TryParseExact(timeStr, "d'd'", CultureInfo.InvariantCulture, out time))
        {
            return time;
        }

        throw new Exception($"Unsupported time format at '{jobName}'");
    }
}
