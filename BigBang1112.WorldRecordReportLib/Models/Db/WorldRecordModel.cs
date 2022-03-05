using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class WorldRecordModel
{
    public int Id { get; set; }

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

    public bool Ignored { get; set; }
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

        var (time, userName) = GetWrParams(this);

        var baseStr = $"{time} by {userName} on {mapName}";

        if (PreviousWorldRecord is not null)
        {
            var (prevTime, prevUserName) = GetWrParams(PreviousWorldRecord);
            baseStr += $" (previous: {prevTime} by {prevUserName})";
        }

        return baseStr;
    }

    private static (TimeInt32 time, string userName) GetWrParams(WorldRecordModel wrModel)
    {
        var time = wrModel.TimeInt32;
        var userName = wrModel.Player?.ToString() ?? wrModel.TmxPlayer?.Nickname ?? "[not found]";

        return (time, userName);
    }

    public static string GetPlayerLogin(WorldRecordModel wr)
    {
        return wr.Player?.Name ?? wr.TmxPlayer?.Nickname ?? "[unknown login]";
    }

    public static string GetPlayerNickname(WorldRecordModel wr)
    {
        return wr.Player?.Nickname ?? wr.TmxPlayer?.Nickname ?? "[unknown nickname]";
    }
}
