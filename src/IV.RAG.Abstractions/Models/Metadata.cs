using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IV.RAG;

/// <summary>
/// Key-value metadata attached to a <see cref="Document"/> and propagated to every <see cref="Chunk"/> it produces.
/// Values are strongly typed as <see cref="MetadataFilterValue"/> (Text, Number, or Boolean).
/// Supports collection-initializer and index-initializer syntax for ergonomic construction.
/// </summary>
[JsonConverter(typeof(MetadataConverter))]
public sealed class Metadata : IReadOnlyDictionary<string, MetadataFilterValue>
{
    private readonly Dictionary<string, MetadataFilterValue> _data;

    /// <summary>Initializes an empty instance.</summary>
    public Metadata() => _data = new();

    /// <summary>Initializes from existing key-value pairs.</summary>
    public Metadata(IDictionary<string, MetadataFilterValue> data) => _data = new(data);

    /// <summary>Gets or sets the value associated with <paramref name="key"/>.</summary>
    public MetadataFilterValue this[string key]
    {
        get => _data[key];
        set => _data[key] = value;
    }

    /// <summary>Adds a key-value pair. Enables collection-initializer syntax.</summary>
    public void Add(string key, MetadataFilterValue value) => _data.Add(key, value);

    /// <inheritdoc/>
    public bool ContainsKey(string key) => _data.ContainsKey(key);

    /// <inheritdoc/>
    public bool TryGetValue(string key, out MetadataFilterValue value) =>
        _data.TryGetValue(key, out value!);

    /// <inheritdoc/>
    public IEnumerable<string> Keys => _data.Keys;

    /// <inheritdoc/>
    public IEnumerable<MetadataFilterValue> Values => _data.Values;

    /// <inheritdoc/>
    public int Count => _data.Count;

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, MetadataFilterValue>> GetEnumerator() =>
        _data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();

    /// <summary>Returns <see langword="true"/> if both instances have the same keys and values.</summary>
    public static bool operator ==(Metadata? left, Metadata? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Returns <see langword="true"/> if the instances differ in any key or value.</summary>
    public static bool operator !=(Metadata? left, Metadata? right) => !(left == right);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not Metadata other || _data.Count != other._data.Count) return false;
        foreach (var (key, value) in _data)
            if (!other._data.TryGetValue(key, out var otherValue) || !value.Equals(otherValue))
                return false;
        return true;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        foreach (var (key, value) in _data)
            hash ^= HashCode.Combine(key, value);
        return hash;
    }
}

internal sealed class MetadataConverter : JsonConverter<Metadata>
{
    public override Metadata Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of JSON object for Metadata.");

        var result = new Metadata();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var key = reader.GetString()!;
            reader.Read();
            result.Add(key, JsonSerializer.Deserialize<MetadataFilterValue>(ref reader, options)!);
        }
        return result;
    }

    public override void Write(Utf8JsonWriter writer, Metadata value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (key, val) in value)
        {
            writer.WritePropertyName(key);
            JsonSerializer.Serialize(writer, val, options);
        }
        writer.WriteEndObject();
    }
}
