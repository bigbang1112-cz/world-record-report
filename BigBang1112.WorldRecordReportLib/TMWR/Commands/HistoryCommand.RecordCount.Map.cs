﻿using BigBang1112.DiscordBot.Models;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Models.Db;
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

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class HistoryCommand
{
    public partial class RecordCount
    {
        [DiscordBotSubCommand("map", "Gets the history of the record count increase on a map.")]
        [UnfinishedDiscordBotCommand]
        public class Map : MapRelatedWithUidCommand
        {
            private readonly IWrUnitOfWork _wrUnitOfWork;

            public Map(TmwrDiscordBotService tmwrDiscordBotService, IWrUnitOfWork wrUnitOfWork) : base(tmwrDiscordBotService, wrUnitOfWork)
            {
                _wrUnitOfWork = wrUnitOfWork;
            }

            protected override async Task<FileAttachment?> CreateAttachmentAsync(MapModel map, Deferer deferer)
            {
                var counts = await _wrUnitOfWork.RecordCounts.GetAllByMapAsync(map);

                await deferer.DeferAsync();

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

            protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
            {
                var date = await _wrUnitOfWork.RecordCounts.GetStartingDateOfTrackingAsync(map);

                builder.Title = $"{map.GetHumanizedDeformattedName()}";
                builder.Url = map.GetInfoUrl();

                if (date.HasValue)
                {
                    builder.Description = $"This is a record count history tracked since {date.Value.ToTimestampTag(TimestampTagStyles.ShortDate)}.";
                }
                else
                {
                    builder.Description = $"There is no tracked record count history.";
                }
            }
        }
    }
}
