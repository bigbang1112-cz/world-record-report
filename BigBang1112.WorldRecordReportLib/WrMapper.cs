using BigBang1112.WorldRecordReportLib.Models;
using Mapster;
using ManiaAPI.TMX;

namespace BigBang1112.WorldRecordReportLib;

public class WrMapper : Mapper
{
    public override void Configure()
    {
        base.Configure();
        
        TypeAdapterConfig<LbManialinkMapRecord, LeaderboardRecord>
             .ForType()
             .Map(dest => dest.Time, src => TimeSpan.FromMilliseconds(src.Time))
             .Map(dest => dest.Timestamp, src => DateTimeOffset.FromUnixTimeSeconds(src.Timestamp))
             .Map(dest => dest.IsFromManialink, src => true);

        TypeAdapterConfig<ReplayItem, TmxReplay>
            .ForType()
            .Map(dest => dest.Rank, src => src.Position.HasValue ? src.Position + 1 : null)
            .Map(dest => dest.IsCompPatch, src => src.Validated)
            .Map(dest => dest.UserId, src => src.User.UserId)
            .Map(dest => dest.UserName, src => src.User.Name)
            .Map(dest => dest.ReplayAt, src => src.ReplayAt.UtcDateTime)
            .Map(dest => dest.ReplayTime, src => src.ReplayTime.TotalMilliseconds);

        TypeAdapterConfig<ManiaAPI.NadeoAPI.Record, TM2020RecordFundamental>
            .ForType()
            .Map(dest => dest.Rank, src => src.Position);

        TypeAdapterConfig<MapGroupModel, MapGroupRefreshData>
            .ForType()
            .Map(dest => dest.TitleId, src => src.TitlePack != null ? src.TitlePack.GetTitleUid() : null);
    }
}
