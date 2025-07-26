using System.Text.Json.Serialization;

namespace server.Models.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnnotationType
{
    BOUNDING_BOX,
    POLYGON,
    POLYLINE,
    POINT,
    TEXT,
    LINE
}
