using BigBang1112.Models.States;
using BigBang1112.WorldRecordReportLib.Models;
using Microsoft.AspNetCore.Http;
using OneOf;

namespace BigBang1112.WorldRecordReportLib.Services;

public interface ILeaderboardsManialinkService
{
    Task<string> AuthorizeAsync(string redirectUri, string code, string state);
    Task<string> GetManialinkAsync(HttpContext httpContext, CancellationToken cancellationToken);
    Task<LbManialinkMember> GetMemberAsync(string login, CancellationToken cancellationToken);
    Task<IEnumerable<LbManialinkMember>> GetMembersAsync(CancellationToken cancellationToken);
    Task<IEnumerable<LbManialinkReport>> GetReportsAsync(string titleUid, CancellationToken cancellationToken);
    void Head(IHeaderDictionary headers);
    Task<OneOf<LbManialinkWorldRecordsResponse, AccountUnauthorized, AccountForbidden>> PostWorldRecordsAsync(HttpContext httpContext, LbManialinkMap lbManialinkMap);
}
