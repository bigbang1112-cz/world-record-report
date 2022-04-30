using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class RecordCountRepo : Repo<RecordCountModel>, IRecordCountRepo
{
    public RecordCountRepo(DbContext context) : base(context)
    {
        
    }
}
