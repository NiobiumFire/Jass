using BelotWebApp.BelotClasses.Declarations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BelotWebApp.Configuration
{
    public class DeclarationConverter : JsonConverter<Declaration>
    {
        public override Declaration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp))
            {
                // Missing "type" property
                return null;
            }

            DeclarationType type;

            if (typeProp.ValueKind == JsonValueKind.String)
            {
                if (!Enum.TryParse(typeProp.GetString(), ignoreCase: true, out type))
                {
                    return null;
                }
            }
            else if (typeProp.ValueKind == JsonValueKind.Number)
            {
                if (!typeProp.TryGetInt32(out int typeInt) || !Enum.IsDefined(typeof(DeclarationType), typeInt))
                {
                    return null;
                }

                type = (DeclarationType)typeInt;
            }
            else
            {
                return null; // Unexpected type
            }


            string json = root.GetRawText();

            try
            {
                return type switch
                {
                    DeclarationType.Belot => JsonSerializer.Deserialize<Belot>(json, options),
                    DeclarationType.Run => JsonSerializer.Deserialize<Run>(json, options),
                    DeclarationType.Carre => JsonSerializer.Deserialize<Carre>(json, options),
                    _ => null
                };
            }
            catch (Exception)
            {
                // Deserialization failed due to mismatched shape or invalid data
                return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, Declaration value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case Belot belot:
                    JsonSerializer.Serialize(writer, belot, options);
                    break;
                case Run run:
                    JsonSerializer.Serialize(writer, run, options);
                    break;
                case Carre carre:
                    JsonSerializer.Serialize(writer, carre, options);
                    break;
                default:
                    JsonSerializer.Serialize(writer, value, options);
                    break;
            }
        }
    }

}