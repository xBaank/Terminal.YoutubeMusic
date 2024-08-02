using System.Text.Json;
using System.Text.Json.Serialization;
using YoutubeExplode.Videos;

namespace Console.LocalPlaylists;

internal class VideoIdConverter : JsonConverter<VideoId>
{
    public override VideoId Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        // Assuming the JSON value is a string that represents the GUID
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            return value is null ? throw new JsonException("VideoId is null.") : new VideoId(value);
        }

        throw new JsonException("Invalid format for VideoId.");
    }

    public override void Write(Utf8JsonWriter writer, VideoId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value.ToString());
    }
}
