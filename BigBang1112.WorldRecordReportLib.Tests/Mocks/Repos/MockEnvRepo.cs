using BigBang1112.Repos.Mocks;
using BigBang1112.WorldRecordReportLib.Attributes;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.WorldRecordReportLib.Tests.Mocks.Repos;

public class MockEnvRepo : MockEnumRepo<EnvModel, Env>, IEnvRepo
{
    public MockEnvRepo()
    {
        Entities = EnumData.Create<EnvModel, Env, EnvAttribute>(WrEnumData.EnvAttributeToModel).ToList();
    }
}
