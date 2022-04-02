using BigBang1112.WorldRecordReportLib.Enums;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class EnvRepo : EnumRepo<EnvModel, Env>, IEnvRepo
{
    public EnvRepo(WrContext context) : base(context)
    {

    }
}
