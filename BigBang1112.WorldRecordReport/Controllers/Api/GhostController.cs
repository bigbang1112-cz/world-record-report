using Microsoft.Net.Http.Headers;
using System.Text.RegularExpressions;
using BigBang1112.Services;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.WorldRecordReportLib.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TmEssentials;
using BigBang1112.Data;

namespace BigBang1112.WorldRecordReport.Controllers.Api;

[Route("api/v1/ghost")]
[ApiController]
public class GhostController : ControllerBase
{
    private static readonly Regex specialCharsRegex = new(@"^[a-zA-Z0-9\-\._]+$", RegexOptions.Compiled);

    private readonly IGhostService _ghostService;
    private readonly IMapRepo _mapRepo;
    private readonly ILoginRepo _loginRepo;

    public GhostController(IGhostService ghostService, IMapRepo mapRepo, ILoginRepo loginRepo)
    {
        _ghostService = ghostService;
        _mapRepo = mapRepo;
        _loginRepo = loginRepo;
    }

    [HttpGet("download/{mapUid}/{timeInMilliseconds}/{login}")]
    public async Task<IActionResult> DownloadAsync(string mapUid, int timeInMilliseconds, string login, CancellationToken cancellationToken = default)
    {
        if (!specialCharsRegex.IsMatch(mapUid))
        {
            return BadRequest(new { message = "mapUid cannot contain special characters" });
        }

        if (!specialCharsRegex.IsMatch(login))
        {
            return BadRequest(new { message = "login cannot contain special characters" });
        }

        var stream = _ghostService.GetGhostStream(mapUid, timeInMilliseconds, login);

        if (stream is null)
        {
            return NotFound();
        }

        var map = await _mapRepo.GetByUidAsync(mapUid, cancellationToken);

        if (map is null)
        {
            return File(stream, MimeConsts.ApplicationGbx, _ghostService.GetGhostFileName(mapUid, timeInMilliseconds, login));
        }

        var loginModel = await _loginRepo.GetByNameAsync(map.Game, login, cancellationToken);

        var nickname = loginModel?.GetDeformattedNickname() ?? login;

        var timeStr = new TimeInt32(timeInMilliseconds)
            .ToString(useHundredths: map.Game.IsTMUF())
            .Replace(':', '\'')
            .Replace(".", "''");

        var fileName = $"{map.DeformattedName}_{nickname}({timeStr}).Ghost.Gbx";

        foreach (var c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }

        var lastModified = _ghostService.GetGhostLastModified(mapUid, timeInMilliseconds, login);

        if (lastModified is null)
        {
            return File(stream, MimeConsts.ApplicationGbx, fileName);
        }

        // create valid etag
        var etag = new EntityTagHeaderValue($"\"{mapUid}-{timeInMilliseconds}-{login}\"");

        return File(stream, MimeConsts.ApplicationGbx, fileName, lastModified, etag);
    }
}
