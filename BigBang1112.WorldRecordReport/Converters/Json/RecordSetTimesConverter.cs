using System.Text.Json;
using System.Text.Json.Serialization;

namespace BigBang1112.WorldRecordReport.Converters.Json
{
    public class RecordSetTimesConverter : JsonConverter<(int time, int count)>
    {
        public override (int time, int count) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Read();
            var time = reader.GetInt32();
            reader.Read();
            var count = reader.GetInt32();
            reader.Read();
            return (time, count);
        }

        public override void Write(Utf8JsonWriter writer, (int time, int count) value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.time);
            writer.WriteNumberValue(value.count);
            writer.WriteEndArray();
        }
    }
}
