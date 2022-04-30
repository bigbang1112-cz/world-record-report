using BigBang1112.WorldRecordReportLib.Models;
using Microsoft.Extensions.FileProviders;

namespace BigBang1112.WorldRecordReportLib.Services;

public interface IRecordSetService
{
    IFileInfo GetFileInfo(string zone, string mapUid);
    void GetFilePaths(string zone, string mapUid, out string path, out string fileName, out string fullFileName, out string subDirFileName, out string fullSnapshotFileName);
    Task<LeaderboardTM2?> GetFromMapAsync(string zone, string mapUid);
    Task<string?> GetJsonFromMapAsync(string zone, string mapUid);
    Stream? GetStreamFromMap(string zone, string mapUid);
    Task UpdateRecordSetAsync(string zone, string mapUid, LeaderboardTM2 recordSet, Dictionary<string, string> nicknameDictionary);
}
