namespace BigBang1112.WorldRecordReportLib.Exceptions;

public class RefreshLoopNotFoundException : Exception
{
    public RefreshLoopNotFoundException(Guid refreshLoopGuid)
        : base($"Refresh loop {refreshLoopGuid} was expected but not found.")
    {
        
    }
}
