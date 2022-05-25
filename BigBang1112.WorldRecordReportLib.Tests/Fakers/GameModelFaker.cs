using BigBang1112.WorldRecordReportLib.Models.Db;
using Bogus;

namespace BigBang1112.WorldRecordReportLib.Tests.Fakers;

public class GameModelFaker : Faker<GameModel>
{
    public GameModelFaker()
    {
        RuleFor(x => x.Id, f => f.Random.Int(1, 100));
        RuleFor(x => x.Name, f => f.Name.FirstName());
        RuleFor(x => x.DisplayName, f => f.Name.FullName());
    }
}
