using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BigBang1112.WorldRecordReport.Controllers;

[Route("manialink/leaderboards")]
[ApiExplorerSettings(IgnoreApi = true)]
public class LeaderboardsManialinkController : ControllerBase
{
    private readonly ILeaderboardsManialinkService _lbManialinkService;

    private string RedirectUri => $"https://{Request.Host.Host}/manialink/leaderboards/auth";

    private static readonly JsonSerializerOptions jsonSerializerOptions = new();

    public LeaderboardsManialinkController(ILeaderboardsManialinkService lbManialinkService)
    {
        _lbManialinkService = lbManialinkService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var content = await _lbManialinkService.GetManialinkAsync(HttpContext, cancellationToken);
        return Content(content, "text/plain");
    }

    [HttpHead]
    public IActionResult Head()
    {
        _lbManialinkService.Head(HttpContext.Response.Headers);
        return Ok();
    }

    [HttpPost("worldrecords")]
    public async Task<IActionResult> PostWorldRecordsAsync([FromBody] LbManialinkMap lbManialinkMap)
    {
        var response = await _lbManialinkService.PostWorldRecordsAsync(HttpContext, lbManialinkMap);

        return response.Match<IActionResult>(content =>
        {
            return new JsonResult(content, jsonSerializerOptions);
        },
        unauthorized => StatusCode(StatusCodes.Status401Unauthorized, unauthorized),
        forbidden => StatusCode(StatusCodes.Status403Forbidden, forbidden));
    }

    [HttpGet("auth")]
    public async Task<IActionResult> AuthorizeAsync(string code, string state)
    {
        try
        {
            var content = await _lbManialinkService.AuthorizeAsync(RedirectUri, code, state);
            return Content(content, "application/xml");
        }
        catch (HttpRequestException e)
        {
            switch (e.StatusCode)
            {
                case System.Net.HttpStatusCode.BadRequest:
                    return BadRequest();
                default:
                    throw;
            }
        }
    }

    [HttpGet("member")]
    public async Task<IActionResult> GetMemberAsync(string login, CancellationToken cancellationToken)
    {
        var member = await _lbManialinkService.GetMemberAsync(login, cancellationToken);
        var json = JsonSerializer.Serialize(member);
        return Content(json, "application/json");
    }

    [HttpGet("members")]
    public async Task<ActionResult<IEnumerable<LbManialinkMember>>> GetMembersAsync(CancellationToken cancellationToken)
    {
        return new JsonResult(await _lbManialinkService.GetMembersAsync(cancellationToken), jsonSerializerOptions);
    }

    [HttpGet("reports")]
    public async Task<ActionResult<IEnumerable<LbManialinkReport>>> GetReportsAsync(string title, CancellationToken cancellationToken)
    {
        return new JsonResult(await _lbManialinkService.GetReportsAsync(title, cancellationToken), jsonSerializerOptions);
    }
}
