using System.Text.Json;
using System.Text.Json.Serialization;
using BigBang1112.WorldRecordReportLib.Models;
using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Converters.Json;

public class UniqueRecordConverter : JsonConverter<UniqueRecord>
{
    public override UniqueRecord Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read();
        var time = reader.GetInt32();
        reader.Read();
        var count = reader.GetInt32();
        reader.Read();
        return new(time != -1 ? new TimeInt32(time) : null, count);
    }

    public override void Write(Utf8JsonWriter writer, UniqueRecord value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Time?.TotalMilliseconds ?? -1);
        writer.WriteNumberValue(value.Count);
        writer.WriteEndArray();
    }
}
