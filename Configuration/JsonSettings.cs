using System.Text.Json.Serialization;
using System.Text.Json;

namespace BelotWebApp.Configuration
{
    public static class JsonSettings
    {
        public static readonly JsonSerializerOptions Compact = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            PropertyNamingPolicy = null // Pascal Case
        };
    }

}
