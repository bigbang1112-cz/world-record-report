using BigBang1112.DiscordBot.Attributes;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Geo;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.SKCharts;
using SkiaSharp;

namespace BigBang1112.TMWR.Commands;

public partial class HistoryCommand
{
    public partial class RecordCount
    {
        [DiscordBotSubCommand("map")]
        public class Map : MapRelatedWithUidCommand
        {
            private readonly IWrRepo _repo;

            public Map(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo) : base(tmwrDiscordBotService, repo)
            {
                _repo = repo;
            }

            protected override async Task<FileAttachment?> CreateAttachmentAsync(MapModel map)
            {
                var counts = await _repo.GetRecordCountsOnMapAsync(map);

                var ms = CreateChartFromValues(counts);

                return new FileAttachment(ms, "lol.png");
            }

            private static MemoryStream CreateChartFromValues(IEnumerable<RecordCountModel> counts)
            {
                IEnumerable<ObservablePoint> values;

                if (counts.Any())
                {
                    var smallestTick = counts.Min(x => x.Before.Ticks);
                    values = counts.Select(x => new ObservablePoint((x.Before.Ticks - smallestTick) / TimeSpan.TicksPerSecond / 86400.0, x.Count));
                }
                else
                {
                    values = Enumerable.Empty<ObservablePoint>();
                }

                var cartesianChart = new SKCartesianChart
                {
                    Width = 1600,
                    Height = 900,
                    Series = new ISeries[]
                    {
                        new LineSeries<ObservablePoint> {
                            Values = values,
                            Stroke = new SolidColorPaint(SKColors.Transparent),
                            Fill = new SolidColorPaint(SKColors.Transparent)
                        }
                    },
                    Background = new SKColor(41, 42, 45)
                };

                var image = cartesianChart.GetImage();
                var data = image.Encode(SKEncodedImageFormat.Png, quality: 80);
                var ms = new MemoryStream();
                data.SaveTo(ms);

                return ms;
            }

            protected override Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
            {
                builder.Title = $"Record count on {map.GetHumanizedDeformattedName()}";

                return Task.CompletedTask;
            }
        }
    }
}
