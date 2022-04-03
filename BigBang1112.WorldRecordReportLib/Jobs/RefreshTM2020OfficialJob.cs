using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using BigBang1112.WorldRecordReportLib.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace BigBang1112.WorldRecordReportLib.Jobs;

public class RefreshTM2020OfficialJob : IJob
{
    private readonly IConfiguration _config;
    private readonly RefreshScheduleService _refreshSchedule;
    private readonly ILogger<RefreshTM2020OfficialJob> _logger;

    public RefreshTM2020OfficialJob(IConfiguration config, RefreshScheduleService refreshSchedule, ILogger<RefreshTM2020OfficialJob> logger)
    {
        _config = config;
        _refreshSchedule = refreshSchedule;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        
    }
}
