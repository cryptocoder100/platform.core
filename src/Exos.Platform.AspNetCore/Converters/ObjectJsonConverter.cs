#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
namespace Exos.Platform.AspNetCore.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class to Convert JSON String to object.
    /// </summary>
    public class ObjectJsonConverter : JsonConverter<object>
    {
        /// <summary>
        /// overriding base method to read json string from UTF8jsonreader.
        /// </summary>
        /// <param name="reader">reader.</param>
        /// <param name="typeToConvert">type to convert.</param>
        /// <param name="options">Json Options.</param>
        /// <returns>returns object.</returns>
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.True)
            {
                return true;
            }

            if (reader.TokenType == JsonTokenType.False)
            {
                return false;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt64(out long l))
                {
                    return l;
                }

                return reader.GetDouble();
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                if (reader.TryGetDateTime(out DateTime datetime))
                {
                    return datetime;
                }

                return reader.GetString();
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using JsonDocument documentV = JsonDocument.ParseValue(ref reader);
                return ReadObject(documentV.RootElement);
            }

            // Use JsonElement as fallback.
            // Newtonsoft uses JArray or JObject.
            JsonDocument document = JsonDocument.ParseValue(ref reader);
            return document.RootElement.Clone();
        }

        /// <summary>
        /// Write.
        /// </summary>
        /// <param name="writer">UTF8 writer.</param>
        /// <param name="value">value.</param>
        /// <param name="options">json options.</param>
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            // writer.WriteStringValue(value.ToString());
        }

        /// <summary>
        /// Read Object.
        /// </summary>
        /// <param name="jsonElement">Element.</param>
        /// <returns>object return.</returns>
        private object ReadObject(JsonElement jsonElement)
        {
            IDictionary<string, object> expandoObject = new ExpandoObject();
            foreach (var obj in jsonElement.EnumerateObject())
            {
                var k = obj.Name;
                var value = ReadValue(obj.Value);
                expandoObject[k] = value;
            }

            return expandoObject;
        }

        /// <summary>
        /// Read Value.
        /// </summary>
        /// <param name="jsonElement">Json Element.</param>
        /// <returns>object.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Exception.</exception>
        private object? ReadValue(JsonElement jsonElement)
        {
            object? result = null;
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Object:
                    result = ReadObject(jsonElement);
                    break;
                case JsonValueKind.Array:
                    result = ReadList(jsonElement);
                    break;
                case JsonValueKind.String:
                    // TODO: Missing Datetime&Bytes Convert
                    result = jsonElement.GetString();
                    break;
                case JsonValueKind.Number:
                    // TODO: more num type
                    result = 0;
                    if (jsonElement.TryGetInt64(out long l))
                    {
                        result = l;
                    }

                    break;
                case JsonValueKind.True:
                    result = true;
                    break;
                case JsonValueKind.False:
                    result = false;
                    break;
                case JsonValueKind.Undefined:
                case JsonValueKind.Null:
                    result = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("ArgumentOutOfRangeException", nameof(jsonElement.ValueKind));
            }

            return result;
        }

        /// <summary>
        /// Read List.
        /// </summary>
        /// <param name="jsonElement">Element.</param>
        /// <returns>Nullable object.</returns>
        private object? ReadList(JsonElement jsonElement)
        {
            var list = new List<object?>();
            foreach (var item in jsonElement.EnumerateArray())
            {
                list.Add(ReadValue(item));
            }

            return list.Count == 0 ? null : list;
        }
    }
}
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.