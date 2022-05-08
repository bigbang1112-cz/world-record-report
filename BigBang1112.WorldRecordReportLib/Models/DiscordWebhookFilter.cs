using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Models.ReportScopes;
using ManiaAPI.TMX;

namespace BigBang1112.WorldRecordReportLib.Models;

public class DiscordWebhookFilter
{
    public IEnumerable<TM2>? ReportTM2 { get; set; }
    public IEnumerable<TMUF>? ReportTMUF { get; set; }
    public bool? ReportRemovedRecsFromTM2 { get; set; }

    public record TM2(string TitleId);
    public record TMUF(string Site, int? UserId = null, LeaderboardType? LeaderboardType = null);

    /// <param name="reportTM2">What should be reported from TM2 world records. Null means do not report entirely.</param>
    /// <param name="reportTMUF">What should be reported from TMUF world records. Null means do not report entirely</param>
    public DiscordWebhookFilter(IEnumerable<TM2>? reportTM2, IEnumerable<TMUF>? reportTMUF)
    {
        ReportTM2 = reportTM2;
        ReportTMUF = reportTMUF;
    }

    public DiscordWebhookFilter()
    {

    }

    public ReportScopeSet ToReportScopeSet()
    {
        var reportTM2 = default(ReportScopeTM2);
        var reportTMUF = default(ReportScopeTMUF);

        if (ReportTM2 is not null && ReportTM2.Any())
        {
            reportTM2 = new()
            {
                Nadeo = new()
                {
                    WR = new()
                    {
                        Param = ReportTM2.Select(x => x.TitleId).ToArray()
                    }
                }
            };
        }

        if (ReportTMUF is not null && ReportTMUF.Any())
        {
            reportTMUF = new()
            {
                TMX = new()
                {
                    Official = new()
                    {
                        WR = new()
                        {
                            Param = ReportTMUF.Select(x => x.Site switch
                            {
                                NameConsts.TMXSiteUnited => x.LeaderboardType switch
                                {
                                    LeaderboardType.Nadeo => "NadeoTMUF",
                                    LeaderboardType.Star => "StarTrack",
                                    _ => throw new Exception(),
                                },
                                NameConsts.TMXSiteTMNF => "NadeoTMNF",
                                _ => throw new Exception(),
                            }).ToArray()
                        }
                    }
                }
            };
        }

        return new ReportScopeSet()
        {
            TM2 = reportTM2,
            TMUF = reportTMUF,
        };
    }

    public static DiscordWebhookFilter CreateTM2Filter(string[] titleIds)
    {
        return new DiscordWebhookFilter
        {
            ReportTM2 = titleIds.Select(titleId => new TM2(titleId))
        };
    }

    public static DiscordWebhookFilter CreateTMUFFilter()
    {
        return new DiscordWebhookFilter
        {
            ReportTMUF = new TMUF[]
            {
                new(NameConsts.TMXSiteUnited, LeaderboardType: LeaderboardType.Nadeo),
                new(NameConsts.TMXSiteTMNF, LeaderboardType: LeaderboardType.Nadeo),
                new(NameConsts.TMXSiteUnited, LeaderboardType: LeaderboardType.Star), // StarTrack
                // Pls support for classic tracks
            }
        };
    }
}
