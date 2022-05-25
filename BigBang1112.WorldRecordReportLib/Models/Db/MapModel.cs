using BigBang1112.Models.Db;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

[Index(nameof(MapUid))]
public class MapModel : DbModel
{
    [Required]
    [MaxLength(32)] // 27 chars
    public string MapUid { get; set; } = default!;

    // Should specify the major game versions (TMUF, TM2, TMTURBO or TM2020)
    [Required]
    public virtual GameModel Game { get; set; } = default!;

    [Required]
    public virtual EnvModel Environment { get; set; } = default!;

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = default!;

    [StringLength(255)]
    public string DeformattedName { get; set; } = default!;

    public virtual LoginModel Author { get; set; } = default!;

    public virtual TitlePackModel? TitlePack { get; set; }

    public int? MxId { get; set; }

    public Guid? ThumbnailGuid { get; set; } // for TM2020

    public virtual MapGroupModel? Group { get; set; }

    [MinLength(32)]
    [MaxLength(32)]
    public byte[]? Checksum { get; set; }

    [Required]
    [Column(TypeName = "datetime")]
    public DateTime AddedOn { get; set; }

    [Required]
    [Column(TypeName = "datetime")]
    public DateTime UpdatedOn { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastActivityOn { get; set; }

    public virtual TmxLoginModel? TmxAuthor { get; set; }

    public virtual MapModeModel? Mode { get; set; }

    // Can specify the game where the map was originally made, or where it can be played. Should be different than Game or NULL.
    public virtual GameModel? IntendedGame { get; set; }

    public virtual CampaignModel? Campaign { get; set; }

    [StringLength(255)]
    public string? MapType { get; set; }
    
    [StringLength(255)]
    public string? MapStyle { get; set; }
    
    public Guid? DownloadGuid { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? FileLastModifiedOn { get; set; }

    public Guid? MapId { get; set; }

    public ScoreContextValue<DateTimeOffset>? LastRefreshedOn { get; set; }

    public virtual ICollection<WorldRecordModel> WorldRecords { get; set; } = default!;
    public virtual ICollection<RecordSetChangeModel> RecordSetChanges { get; set; } = default!;
    public virtual ICollection<RecordSetDetailedChangeModel> RecordSetDetailedChanges { get; set; } = default!;
    public virtual ICollection<RecordCountModel> RecordCounts { get; set; } = default!;

    public bool IsStuntsMode()
    {
        return Mode?.Name == NameConsts.MapModeStunts;
    }

    public string GetTitleUidOrEnvironment()
    {
        return TitlePack?.GetTitleUid() ?? Environment.DisplayName ?? Environment.Name;
    }

    public string GetHumanizedDeformattedName()
    {
        // Wouldn't work well for platform maps and maps with fake nadeo login

        if (TitlePack is null)
        {
            return DeformattedName;
        }

        if (Author.Name != NameConsts.LoginNadeoName)
        {
            return DeformattedName;
        }

        return $"{Environment.Name} {DeformattedName}";
    }

    public string? GetThumbnailUrl()
    {
        if (Game.IsTM2020() && ThumbnailGuid.HasValue)
        {
            return $"https://prod.trackmania.core.nadeo.online/storageObjects/{ThumbnailGuid.Value}.jpg";
        }

        if (MxId is null)
        {
            return null;
        }

        if (Game.IsTMUF())
        {
            if (TmxAuthor is null)
            {
                return null;
            }

            return (TmxSite)TmxAuthor.Site.Id switch
            {
                TmxSite.United or TmxSite.TMNF => $"{TmxAuthor.Site.Url}trackshow/{MxId}/image/0",
                _ => null,
            };
        }

        if (Game.IsTM2())
        {
            return $"https://tm.mania-exchange.com/tracks/thumbnail/{MxId}";
        }

        return null;
    }

    public string? GetTmxUrl()
    {
        if (MxId is null)
        {
            return null;
        }

        if (Game.IsTMUF())
        {
            if (TmxAuthor is null)
            {
                return null;
            }

            return (TmxSite)TmxAuthor.Site.Id switch
            {
                TmxSite.United or TmxSite.TMNF => $"{TmxAuthor.Site.Url}trackshow/{MxId}",
                _ => null,
            };
        }

        if (Game.IsTM2())
        {
            return $"https://tm.mania.exchange/maps/{MxId}";
        }

        if (Game.IsTM2020())
        {
            return $"https://trackmania.exchange/s/tr/{MxId}";
        }

        return null;
    }

    public string? GetTrackmaniaIoUrl()
    {
        if (!Game.IsTM2020() || Campaign?.LeaderboardUid is null)
        {
            return null;
        }

        return $"https://trackmania.io/#/leaderboard/{MapUid}";
    }

    public string? GetInfoUrl()
    {
        return GetTrackmaniaIoUrl() ?? GetTmxUrl();
    }

    public string GetMdLink()
    {
        var infoUrl = GetInfoUrl();

        if (infoUrl is null)
        {
            return DeformattedName;
        }

        return $"[{DeformattedName}]({infoUrl})";
    }

    public string GetMdLinkHumanized()
    {
        var infoUrl = GetInfoUrl();

        if (infoUrl is null)
        {
            return GetHumanizedDeformattedName();
        }

        return $"[{GetHumanizedDeformattedName()}]({infoUrl})";
    }

    public string GetAuthorNickname()
    {
        return TmxAuthor?.Nickname ?? Author?.Nickname ?? "[unknown author]";
    }

    public string GetAuthorNicknameDeformatted()
    {
        return TmxAuthor?.Nickname ?? Author?.GetDeformattedNickname() ?? "[unknown author]";
    }

    public string GetAuthorNicknameMdLink()
    {
        return TmxAuthor?.GetMdLink() ?? Author?.GetMdLink() ?? "[unknown author]";
    }

    public string? GetAuthorInfoUrl()
    {
        return TmxAuthor?.GetInfoUrl() ?? Author?.GetInfoUrl();
    }
}
