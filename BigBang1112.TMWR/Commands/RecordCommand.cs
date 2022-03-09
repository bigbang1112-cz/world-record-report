using BigBang1112.Attributes.DiscordBot;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("record", "Shows information about a certain record.")]
public class RecordCommand : MapRelatedCommand
{
    public RecordCommand(IWrRepo repo) : base(repo)
    {

    }

    public override IEnumerable<SlashCommandOptionBuilder> YieldOptions()
    {
        foreach (var option in base.YieldOptions())
        {
            yield return option;
        }

        yield return CreateMapUidOption();

        yield return new SlashCommandOptionBuilder
        {
            Name = "rank",
            Description = "Rank of the record.",
            Type = ApplicationCommandOptionType.Integer,
            IsRequired = true,
            MinValue = 1,
            MaxValue = 10
        };
    }
}
