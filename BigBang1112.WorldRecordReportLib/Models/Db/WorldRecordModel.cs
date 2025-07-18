using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BigBang1112.WorldRecordReportLib.Enums;
using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class WorldRecordModel : DbModel
{
    [Required]
    public Guid Guid { get; set; }

    [Required]
    public virtual MapModel Map { get; set; } = default!;

    [Required]
    public int Time { get; set; }

    public virtual LoginModel? Player { get; set; }

    public virtual TmxLoginModel? TmxPlayer { get; set; }

    /// <summary>
    /// Date of the ghost file
    /// </summary>
    [Required]
    [Column(TypeName = "datetime")]
    public DateTime DrivenOn { get; set; }

    /// <summary>
    /// Date of the ghost file, or date of the occurance of this world record caused by login ignore.
    /// </summary>
    [Required]
    [Column(TypeName = "datetime")]
    public DateTime PublishedOn { get; set; }

    [StringLength(255)]
    public string? ReplayUrl { get; set; } // Should be optimized

    public virtual WorldRecordModel? PreviousWorldRecord { get; set; }
    public int? PreviousWorldRecordId { get; set; }
    public virtual AltReplayModel? AltReplay { get; set; }

    public IgnoredMode Ignored { get; set; }
    public bool ManialinkRecord { get; set; }
    public bool Unverified { get; set; }

    public int? ReplayId { get; set; }

    [NotMapped]
    public TimeInt32 TimeInt32
    {
        get => new(Time);
        set => Time = value.TotalMilliseconds;
    }

    public override string ToString()
    {
        var mapName = Map?.Name ?? "[not found]";

        var baseStr = $"{TimeInt32} by {GetPlayerNicknameDeformatted()} on {mapName}";

        if (PreviousWorldRecord is not null)
        {
            baseStr += $" (previous: {PreviousWorldRecord.TimeInt32} by {PreviousWorldRecord.GetPlayerNicknameDeformatted()})";
        }

        return baseStr;
    }

    public string GetPlayerLogin()
    {
        return Player?.Name ?? TmxPlayer?.UserId.ToString() ?? "[unknown player]";
    }

    public string GetPlayerNickname()
    {
        return Player?.Nickname ?? TmxPlayer?.Nickname ?? "[unknown player]";
    }

    public string GetPlayerNicknameDeformatted()
    {
        return Player?.GetDeformattedNickname() ?? TmxPlayer?.Nickname ?? "[unknown player]";
    }

    public string GetPlayerNicknameMdLink()
    {
        return Player?.GetMdLink() ?? TmxPlayer?.GetMdLink() ?? "[unknown player]";
    }

    public string? GetPlayerInfoUrl()
    {
        return Player?.GetInfoUrl() ?? TmxPlayer?.GetInfoUrl();
    }

    public string GetTimeFormattedToGame()
    {
        if (Map.IsStuntsMode())
        {
            return Time.ToString();
        }

        return TimeInt32.ToString(useHundredths: Map.Game.IsTMUF() || Map.Game.IsTMN());
    }

    public string GetViewUrl()
    {
        if (Map.Game.IsTM2())
        {
            return $"https://3d.gbx.tools/view/ghost?type=wrr&mapuid={Map.MapUid}&time={Time}&login={Player?.Name}&mx=TM2";
        }
        else
        {
            return $"https://3d.gbx.tools/view/replay?tmx={Map.TmxAuthor?.Site.GetSiteEnum()}&id={ReplayId}&mapid={Map.MxId}";
        }
    }
}
