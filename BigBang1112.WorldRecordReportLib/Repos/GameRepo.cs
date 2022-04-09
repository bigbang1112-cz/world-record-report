using BigBang1112.WorldRecordReportLib.Enums;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class GameRepo : EnumRepo<GameModel, Game>, IGameRepo
{
    public GameRepo(WrContext context) : base(context)
    {

    }
}
