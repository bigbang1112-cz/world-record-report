﻿namespace BigBang1112.WorldRecordReport.Exceptions;

public class MapGroupNotFoundException : Exception
{
    public MapGroupNotFoundException()
        : base("Map group is missing and is required for this refresh loop.")
    {

    }
}
