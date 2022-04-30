using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class RecordSetDetailedChangeRepo : Repo<RecordSetDetailedChangeModel>, IRecordSetDetailedChangeRepo
{
    public RecordSetDetailedChangeRepo(DbContext context) : base(context)
    {
        
    }
}
