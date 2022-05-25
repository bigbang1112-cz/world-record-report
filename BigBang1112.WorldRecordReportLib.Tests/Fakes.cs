using BigBang1112.WorldRecordReportLib.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BigBang1112.WorldRecordReportLib.Tests;

public static class Fakes
{
    public static WrContext CreateWrContext(Action<WrContext>? populateData = null)
    {
        var options = new DbContextOptionsBuilder<WrContext>()
            .UseInMemoryDatabase(databaseName: "bigbang1112cz_wr")
            .Options;

        var dbSettings = new Dictionary<string, string>
        {
            {"WrDbEncryptionKey", "fhggX6V69G5mWoqDBb6z65sT8h7tfdbc"},
            {"WrDbEncryptionIV", "1bCtxFImMpjbJYsS"}
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(dbSettings)
            .Build();

        var context = new WrContext(options, config);

        populateData?.Invoke(context);

        context.SaveChanges();

        return context;
    }
}
