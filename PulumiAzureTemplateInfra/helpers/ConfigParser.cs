using System.Collections.Generic;
using System.Text.Json;

namespace PulumiAzureTemplateInfra.helpers
{
    /// <summary>
    /// Provides utilities for parsing and converting JSON configuration data into strongly-typed C# objects.
    /// This class is designed to work with Pulumi Azure infrastructure configurations, converting
    /// JsonElement objects from System.Text.Json into Dictionary structures for easier programmatic access.
    /// So you can add nested objects and arrays in the YAML without worrying about the underlying JSON structure.
    /// </summary>
    public class ConfigParser
    {
        /// <summary>
        /// Recursively converts a JsonElement object into a Dictionary&lt;string, object&gt; structure.
        /// This method preserves the original data types and handles nested objects and arrays.
        /// </summary>
        /// <param name="element">The JsonElement to convert. Must represent a JSON object.</param>
        /// <returns>
        /// A Dictionary&lt;string, object&gt; where keys are property names and values are converted to appropriate .NET types:
        /// - JSON objects become nested Dictionary&lt;string, object&gt;
        /// - JSON arrays become object[]
        /// - JSON strings become string
        /// - JSON numbers become int, long, or double (in that priority order)
        /// - JSON booleans become bool
        /// - JSON null becomes null
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if the JsonElement does not represent a JSON object.</exception>
        public static Dictionary<string, object> ConvertJsonElementToDictionary(JsonElement element)
        {
            var dictionary = new Dictionary<string, object>();

            foreach (var property in element.EnumerateObject())
            {
                string propertyName = property.Name;
                JsonElement propertyValue = property.Value;

                switch (propertyValue.ValueKind)
                {
                    case JsonValueKind.Object:
                        dictionary[propertyName] = ConvertJsonElementToDictionary(propertyValue);
                        break;

                    case JsonValueKind.Array:
                        dictionary[propertyName] = ConvertJsonElementToArray(propertyValue);
                        break;

                    case JsonValueKind.String:
                        dictionary[propertyName] = propertyValue.GetString() ?? string.Empty;
                        break;

                    case JsonValueKind.Number:
                        if (propertyValue.TryGetInt32(out int intValue))
                            dictionary[propertyName] = intValue;
                        else if (propertyValue.TryGetInt64(out long longValue))
                            dictionary[propertyName] = longValue;
                        else if (propertyValue.TryGetDouble(out double doubleValue))
                            dictionary[propertyName] = doubleValue;
                        break;

                    case JsonValueKind.True:
                        dictionary[propertyName] = true;
                        break;

                    case JsonValueKind.False:
                        dictionary[propertyName] = false;
                        break;

                    case JsonValueKind.Null:
                        dictionary[propertyName] = null;
                        break;
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Converts a JsonElement representing a JSON array into an object array.
        /// Each array element is recursively converted to its appropriate .NET type.
        /// </summary>
        /// <param name="arrayElement">The JsonElement to convert. Must represent a JSON array.</param>
        /// <returns>
        /// An object[] where each element is converted to the appropriate .NET type:
        /// - JSON objects become Dictionary&lt;string, object&gt;
        /// - JSON arrays become nested object[]
        /// - JSON primitives become their corresponding .NET types
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if the JsonElement does not represent a JSON array.</exception>
        private static object[] ConvertJsonElementToArray(JsonElement arrayElement)
        {
            var list = new List<object>();

            foreach (var item in arrayElement.EnumerateArray())
            {
                switch (item.ValueKind)
                {
                    case JsonValueKind.Object:
                        list.Add(ConvertJsonElementToDictionary(item));
                        break;
                    case JsonValueKind.Array:
                        list.Add(ConvertJsonElementToArray(item));
                        break;
                    case JsonValueKind.String:
                        list.Add(item.GetString() ?? string.Empty);
                        break;
                    case JsonValueKind.Number:
                        if (item.TryGetInt32(out int intValue))
                            list.Add(intValue);
                        else if (item.TryGetDouble(out double doubleValue))
                            list.Add(doubleValue);
                        break;
                    case JsonValueKind.True:
                        list.Add(true);
                        break;
                    case JsonValueKind.False:
                        list.Add(false);
                        break;
                }
            }

            return list.ToArray();
        }

        /// <summary>
        /// Safely retrieves a value from a dictionary with type checking and default value support.
        /// This method prevents runtime exceptions from invalid casts or missing keys.
        /// </summary>
        /// <typeparam name="T">The expected type of the value to retrieve.</typeparam>
        /// <param name="dict">The dictionary to search in.</param>
        /// <param name="key">The key to look for in the dictionary.</param>
        /// <param name="defaultValue">The value to return if the key is not found or the value cannot be cast to type T. Defaults to the default value of type T.</param>
        /// <returns>
        /// The value associated with the key if it exists and can be cast to type T; otherwise, the defaultValue.
        /// </returns>
        /// <example>
        /// <code>
        /// var config = new Dictionary&lt;string, object&gt; { { "timeout", 30 }, { "enabled", true } };
        /// int timeout = GetValue&lt;int&gt;(config, "timeout", 60); // Returns 30
        /// bool debug = GetValue&lt;bool&gt;(config, "debug", false); // Returns false (key not found)
        /// string name = GetValue&lt;string&gt;(config, "timeout", "default"); // Returns "default" (wrong type)
        /// </code>
        /// </example>
        public static T GetValue<T>(Dictionary<string, object> dict, string key, T defaultValue = default!)
        {
            if (dict.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        /// <summary>
        /// Safely retrieves a nested dictionary from a parent dictionary.
        /// This is a convenience method for accessing nested configuration objects.
        /// </summary>
        /// <param name="dict">The parent dictionary to search in.</param>
        /// <param name="key">The key that should contain a nested dictionary.</param>
        /// <returns>
        /// The nested Dictionary&lt;string, object&gt; if the key exists and contains a dictionary;
        /// otherwise, an empty Dictionary&lt;string, object&gt;.
        /// </returns>
        /// <example>
        /// <code>
        /// var config = new Dictionary&lt;string, object&gt; 
        /// { 
        ///     { "database", new Dictionary&lt;string, object&gt; { { "host", "localhost" } } }
        /// };
        /// var dbConfig = GetNestedDict(config, "database"); // Returns the database dictionary
        /// var cacheConfig = GetNestedDict(config, "cache"); // Returns empty dictionary
        /// </code>
        /// </example>
        public static Dictionary<string, object> GetNestedDict(Dictionary<string, object> dict, string key)
        {
            return GetValue<Dictionary<string, object>>(dict, key) ?? new Dictionary<string, object>();
        }
    }
}