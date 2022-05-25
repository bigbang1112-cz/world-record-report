using BigBang1112.WorldRecordReportLib.Models.Db;
using Bogus;

namespace BigBang1112.WorldRecordReportLib.Tests.Fakers;

public class LoginModelFaker : Faker<LoginModel>
{
    public LoginModelFaker()
    {
        RuleFor(x => x.Id, f => f.Random.Int(1, 100));
        RuleFor(x => x.Name, f => f.Internet.UserName());
        RuleFor(x => x.Nickname, f => f.Name.FullName());
        RuleFor(x => x.JoinedOn, f => f.Date.Recent());
        RuleFor(x => x.LastSeenOn, f => f.Date.Recent());
        RuleFor(x => x.Game, new GameModelFaker().Generate());
    }
}
