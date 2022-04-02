using BigBang1112.Repos;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models.Db;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class GameRepo : EnumRepo<GameModel, Game>, IGameRepo
{
    public GameRepo(WrContext context) : base(context)
    {

    }
}
