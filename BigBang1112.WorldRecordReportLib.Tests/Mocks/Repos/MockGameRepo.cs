using System.Linq.Expressions;
using BigBang1112.Repos.Mocks;
using BigBang1112.WorldRecordReportLib.Attributes;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.WorldRecordReportLib.Tests.Mocks.Repos;

public class MockGameRepo : MockEnumRepo<GameModel, Game>, IGameRepo
{
    public MockGameRepo()
    {
        Entities = EnumData.Create<GameModel, Game, GameAttribute>(WrEnumData.GameAttributeToModel).ToList();
    }
}
