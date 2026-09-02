using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NebulaBot.API;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public sealed class Config
{
    private static Config? _instance;
    private static readonly object _lock = new object();
    // Use AppContext.BaseDirectory to avoid Directory.GetCurrentDirectory() variability
    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "config.yaml");

    /// Configuration options:

    [Description("Which Loglevel to use. (Fatal, Error, Warn, Verbose, Debug)")]
    public Discord.LogSeverity LogLevel { get; set; } = Discord.LogSeverity.Info;

    [Description("The token for the bot used.")]
    public string BotToken { get; set; } = "";

    [Description("Password for the DB")]
    public string dbPW { get; set; } = "";

    [Description("Channel where general user commands may be executed")]
    public long BotChannelID { get; set; } = 0;

    [Description("SL Server API Token (!api show)")]
    public string SLAPIToken { get; set; } = "";

    [Description("ID from the SL Server account (!id)")]
    public string SLAccountID { get; set; } = "";
    
    [Description("The SteamAPI key")]
    public string SteamAPIKey { get; set; } = "";
    /// Config logic
    /// DO NOT TOUCH BELOW THIS LINE!!

    public Config()
    {

    }

    public static Config Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock) // Ensures thread safety
                {
                    if (_instance == null)
                    {
                        _instance = LoadConfig(ConfigPath); // Load YAML when first accessed
                        Task.Delay(500);
                    }
                }
            }
            return _instance;
        }
    }

    private static Config LoadConfig(string filePath)
    {
        try
        {
            // Ensure file exists
            if (!File.Exists(filePath))
            {
                Log.Warn($"Config file not found: {filePath}, creating new one!");
                var firstConf = new Config();
                string yamlContent = BuildYamlWithComments(firstConf);
                File.WriteAllText(filePath, yamlContent, new UTF8Encoding(false));
                Log.Info($"Configuration file created: {Path.GetFullPath(filePath)}");
                return firstConf;
            }

            var yaml = File.ReadAllText(filePath, new UTF8Encoding(false));
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            try
            {
                var cfg = deserializer.Deserialize<Config>(yaml) ?? new Config();

                // If deserialized object has null string properties, set sensible defaults
                ApplyDefaultsIfMissing(cfg);

                // Preserve sensitive/important raw YAML values when parser produced defaults
                PreserveRawSensitiveValuesIfPresent(yaml, cfg, "SLAPIToken", "SLAccountID");

                // Rewrite file to normalize formatting and include descriptions/comments
                try
                {
                    string normalized = BuildYamlWithComments(cfg);

                    // Only overwrite the file when normalized content differs to avoid clobbering user edits
                    if (!string.Equals(normalized, yaml, StringComparison.Ordinal))
                    {
                        File.WriteAllText(filePath, normalized, new UTF8Encoding(false));
                    }
                }
                catch (Exception writeEx)
                {
                    Log.Warn($"Failed to rewrite config file with comments: {writeEx.Message}");
                }

                return cfg;
            }
            catch (YamlException ye)
            {
                // YAML is malformed -> try to recover by best-effort parsing into a dictionary
                Log.Error($"Config parse error: {ye.Message}. Attempting to recover corrupted config.");
                BackupCorruptedFile(filePath);

                var recovered = TryRecoverFromYaml(yaml);

                // If original raw YAML contains SL values, preserve them into recovered config
                PreserveRawSensitiveValuesIfPresent(yaml, recovered, "SLAPIToken", "SLAccountID");

                string sanitizedYaml = BuildYamlWithComments(recovered);
                File.WriteAllText(filePath, sanitizedYaml, new UTF8Encoding(false));
                Log.Info("Recovered and sanitized configuration file written.");

                return recovered;
            }
            catch (Exception exDeserialize)
            {
                // Other deserialization errors (invalid enum values, wrong types)
                Log.Error($"Config deserialize error: {exDeserialize.Message}. Attempting to sanitize values.");
                BackupCorruptedFile(filePath);

                var recovered = TryRecoverFromYaml(yaml);

                // Preserve raw SL values if present
                PreserveRawSensitiveValuesIfPresent(yaml, recovered, "SLAPIToken", "SLAccountID");

                string sanitizedYaml = BuildYamlWithComments(recovered);
                File.WriteAllText(filePath, sanitizedYaml, new UTF8Encoding(false));
                Log.Info("Sanitized configuration file written.");

                return recovered;
            }
        }
        catch (Exception ex)
        {
            // Provide a clearer error context
            throw new Exception($"Failed to load configuration from '{filePath}': {ex.Message}", ex);
        }
    }

    private static void BackupCorruptedFile(string filePath)
    {
        try
        {
            var dir = Path.GetDirectoryName(filePath) ?? Directory.GetCurrentDirectory();
            var backupName = Path.Combine(dir, $"config.backup.{DateTime.UtcNow:yyyyMMddHHmmss}.yaml");
            File.Copy(filePath, backupName, overwrite: true);
            Log.Warn($"Backed up corrupted config to {backupName}");
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to create backup of corrupted config: {ex.Message}");
        }
    }

    private static Config TryRecoverFromYaml(string yaml)
    {
        var result = new Config();
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        try
        {
            // Attempt to parse to a generic dictionary first
            var dict = deserializer.Deserialize<Dictionary<string, object?>>(yaml) ?? new Dictionary<string, object?>();

            var props = typeof(Config).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => ToCamelCase(p.Name), p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var kv in dict)
            {
                if (kv.Key == null) continue;
                if (!props.TryGetValue(kv.Key, out var prop)) continue;

                if (kv.Value == null)
                {
                    // leave default
                    continue;
                }

                try
                {
                    object? converted = ConvertYamlValueToProperty(kv.Value, prop.PropertyType);
                    if (converted != null)
                    {
                        prop.SetValue(result, converted);
                    }
                }
                catch (Exception convEx)
                {
                    Log.Warn($"Failed to convert config value '{kv.Key}': {convEx.Message}. Using default for that property.");
                }
            }
        }
        catch (Exception parseEx)
        {
            Log.Warn($"Failed recovery parse attempt: {parseEx.Message}. Returning defaults.");
        }

        return result;
    }

    private static object? ConvertYamlValueToProperty(object value, Type targetType)
    {
        if (targetType.IsEnum)
        {
            var s = value.ToString() ?? "";
            if (Enum.TryParse(targetType, s, ignoreCase: true, out var enumVal))
                return enumVal;
            // try numeric
            if (long.TryParse(s, out var num))
                return Enum.ToObject(targetType, num);
            throw new InvalidOperationException($"Cannot parse enum value '{s}' for {targetType.Name}");
        }

        if (targetType == typeof(string))
        {
            return value.ToString();
        }

        if (targetType == typeof(long))
        {
            if (value is long l) return l;
            if (value is int i) return (long)i;
            if (long.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedL))
                return parsedL;
        }

        if (targetType == typeof(int))
        {
            if (value is int i) return i;
            if (int.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
        }

        if (targetType == typeof(bool))
        {
            if (value is bool b) return b;
            if (bool.TryParse(value.ToString(), out var parsedB)) return parsedB;
        }

        // Fallback to ChangeType where possible
        try
        {
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static void ApplyDefaultsIfMissing(Config cfg)
    {
        // Ensure strings are not null
        cfg.BotToken ??= "";
        cfg.dbPW ??= "";
        cfg.SLAPIToken ??= "";
        cfg.SLAccountID ??= "";
        // other types have defaults already
    }

    private static string BuildYamlWithComments(Config cfg)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        var sb = new StringBuilder();
        var props = typeof(Config).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToArray();

        foreach (var prop in props)
        {
            var description = prop.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (!string.IsNullOrWhiteSpace(description))
            {
                foreach (var line in SplitLines(description))
                {
                    sb.AppendLine($"# {line}");
                }
            }

            var camelName = ToCamelCase(prop.Name);
            object? value = prop.GetValue(cfg);

            // Build a single-property dictionary so serializer emits "key: value" in the correct formatting
            var single = new Dictionary<string, object?> { { camelName, PrepareValueForSerialization(value) } };
            var partialYaml = serializer.Serialize(single)?.TrimEnd('\r', '\n') ?? $"{camelName}:";
            sb.AppendLine(partialYaml);
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd('\r', '\n') + Environment.NewLine;
    }

    private static object? PrepareValueForSerialization(object? value)
    {
        // Convert enums to their name (string) so the YAML is readable
        if (value == null) return null;
        var t = value.GetType();
        if (t.IsEnum) return value.ToString();
        return value;
    }

    private static IEnumerable<string> SplitLines(string text)
    {
        return text?.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None) ?? Array.Empty<string>();
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (name.Length == 1) return name.ToLowerInvariant();

        // If first two chars are uppercase, leave as-is (preserve acronyms)
        if (char.IsUpper(name[0]) && char.IsUpper(name[1])) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    // ---------------------------------------------------------
    // Helpers to preserve raw YAML scalar values when parser fails
    // ---------------------------------------------------------
    private static void PreserveRawSensitiveValuesIfPresent(string yaml, Config cfg, params string[] keys)
    {
        if (string.IsNullOrEmpty(yaml) || cfg == null) return;

        foreach (var key in keys)
        {
            try
            {
                var raw = ExtractYamlScalarValue(yaml, key);
                if (!string.IsNullOrEmpty(raw))
                {
                    // Only overwrite when the deserialized value is empty (avoid clobbering valid deserialized values)
                    var prop = typeof(Config).GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (prop != null && prop.PropertyType == typeof(string))
                    {
                        var current = prop.GetValue(cfg) as string;
                        if (string.IsNullOrEmpty(current))
                        {
                            prop.SetValue(cfg, raw);
                            Log.Warn($"Preserved raw config value for '{key}'.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to preserve raw YAML key '{key}': {ex.Message}");
            }
        }
    }

    private static string? ExtractYamlScalarValue(string yaml, string key)
    {
        if (string.IsNullOrEmpty(yaml) || string.IsNullOrEmpty(key)) return null;

        // Match lines like: key: value  or key: 'value'  or key: "value"  (stop before comment #)
        var pattern = @"(?m)^\s*" + Regex.Escape(key) + @"\s*:\s*(?:'([^']*)'|""([^""]*)""|([^\r\n#]+))";
        var m = Regex.Match(yaml, pattern);
        if (!m.Success) return null;

        var v = m.Groups[1].Success ? m.Groups[1].Value
              : m.Groups[2].Success ? m.Groups[2].Value
              : m.Groups[3].Value;

        if (v == null) return null;
        v = v.Trim();

        // Treat explicit empty quotes or empty string as empty
        if (string.Equals(v, "''", StringComparison.Ordinal) || string.Equals(v, "\"\"", StringComparison.Ordinal)) return null;
        // If user put empty single-quote in file it will be captured as empty string; return null to avoid overwriting
        if (v.Length == 0) return null;

        return v;
    }
}