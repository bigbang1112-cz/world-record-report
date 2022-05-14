using Discord;

namespace BigBang1112.WorldRecordReportLib.TMWR.Extensions;

public static class EmbedBuilderExtensions
{
    public static EmbedBuilder WithBotFooter(this EmbedBuilder builder, string text)
    {
        return builder.WithFooter(text, UrlConsts.Favicon);
    }
}
