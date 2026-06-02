using System.Text.Json;
using System.Text.Json.Serialization;

namespace IV.RAG;

/// <summary>A typed scalar value used in chunk metadata and filter comparisons.</summary>
[JsonConverter(typeof(MetadataFilterValueConverter))]
public abstract record MetadataFilterValue
{
    /// <summary>A UTF-8 string value.</summary>
    public sealed record Text(string Value) : MetadataFilterValue;

    /// <summary>A 64-bit floating-point numeric value.</summary>
    public sealed record Number(double Value) : MetadataFilterValue;

    /// <summary>A boolean value.</summary>
    public sealed record Boolean(bool Value) : MetadataFilterValue;

    /// <summary>Implicitly wraps a <see cref="string"/> as <see cref="Text"/>.</summary>
    public static implicit operator MetadataFilterValue(string v) => new Text(v);

    /// <summary>Implicitly wraps an <see cref="int"/> as <see cref="Number"/>.</summary>
    public static implicit operator MetadataFilterValue(int v) => new Number(v);

    /// <summary>Implicitly wraps a <see cref="long"/> as <see cref="Number"/>.</summary>
    public static implicit operator MetadataFilterValue(long v) => new Number(v);

    /// <summary>Implicitly wraps a <see cref="float"/> as <see cref="Number"/>.</summary>
    public static implicit operator MetadataFilterValue(float v) => new Number(v);

    /// <summary>Implicitly wraps a <see cref="double"/> as <see cref="Number"/>.</summary>
    public static implicit operator MetadataFilterValue(double v) => new Number(v);

    /// <summary>Implicitly wraps a <see cref="bool"/> as <see cref="Boolean"/>.</summary>
    public static implicit operator MetadataFilterValue(bool v) => new Boolean(v);
}

internal sealed class MetadataFilterValueConverter : JsonConverter<MetadataFilterValue>
{
    public override MetadataFilterValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String  => new MetadataFilterValue.Text(reader.GetString()!),
            JsonTokenType.Number  => new MetadataFilterValue.Number(reader.GetDouble()),
            JsonTokenType.True    => new MetadataFilterValue.Boolean(true),
            JsonTokenType.False   => new MetadataFilterValue.Boolean(false),
            _ => throw new JsonException($"Unexpected token '{reader.TokenType}' for MetadataFilterValue.")
        };

    public override void Write(Utf8JsonWriter writer, MetadataFilterValue value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case MetadataFilterValue.Text(var s):    writer.WriteStringValue(s);  break;
            case MetadataFilterValue.Number(var n):  writer.WriteNumberValue(n);  break;
            case MetadataFilterValue.Boolean(var b): writer.WriteBooleanValue(b); break;
            default: throw new JsonException($"Unsupported MetadataFilterValue subtype: {value.GetType().Name}");
        }
    }
}

/// <summary>Comparison operator for a <see cref="FieldMetadataFilter"/>.</summary>
public enum MetadataOperator
{
    /// <summary>Equal to.</summary>
    Eq,
    /// <summary>Not equal to.</summary>
    Ne,
    /// <summary>Greater than.</summary>
    Gt,
    /// <summary>Greater than or equal to.</summary>
    Gte,
    /// <summary>Less than.</summary>
    Lt,
    /// <summary>Less than or equal to.</summary>
    Lte
}

/// <summary>
/// A composable predicate applied to chunk metadata during retrieval.
/// Use the static factory methods to build filter trees:
/// <see cref="Eq"/>, <see cref="In"/>, <see cref="And"/>, <see cref="Or"/>, <see cref="Not"/>, etc.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(FieldMetadataFilter), "field")]
[JsonDerivedType(typeof(InMetadataFilter),    "in")]
[JsonDerivedType(typeof(AndMetadataFilter),   "and")]
[JsonDerivedType(typeof(OrMetadataFilter),    "or")]
[JsonDerivedType(typeof(NotMetadataFilter),   "not")]
public abstract record MetadataFilter
{
    /// <summary>Matches chunks where <paramref name="field"/> equals <paramref name="value"/>.</summary>
    public static MetadataFilter Eq(string field, MetadataFilterValue value) =>
        new FieldMetadataFilter(field, MetadataOperator.Eq, value);

    /// <summary>Matches chunks where <paramref name="field"/> does not equal <paramref name="value"/>.</summary>
    public static MetadataFilter Ne(string field, MetadataFilterValue value) =>
        new FieldMetadataFilter(field, MetadataOperator.Ne, value);

    /// <summary>Matches chunks where <paramref name="field"/> is greater than <paramref name="value"/>.</summary>
    public static MetadataFilter Gt(string field, MetadataFilterValue value) =>
        new FieldMetadataFilter(field, MetadataOperator.Gt, value);

    /// <summary>Matches chunks where <paramref name="field"/> is greater than or equal to <paramref name="value"/>.</summary>
    public static MetadataFilter Gte(string field, MetadataFilterValue value) =>
        new FieldMetadataFilter(field, MetadataOperator.Gte, value);

    /// <summary>Matches chunks where <paramref name="field"/> is less than <paramref name="value"/>.</summary>
    public static MetadataFilter Lt(string field, MetadataFilterValue value) =>
        new FieldMetadataFilter(field, MetadataOperator.Lt, value);

    /// <summary>Matches chunks where <paramref name="field"/> is less than or equal to <paramref name="value"/>.</summary>
    public static MetadataFilter Lte(string field, MetadataFilterValue value) =>
        new FieldMetadataFilter(field, MetadataOperator.Lte, value);

    /// <summary>Matches chunks where <paramref name="field"/> equals any of the provided <paramref name="values"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values"/> contains mixed types.</exception>
    public static MetadataFilter In(string field, params MetadataFilterValue[] values)
    {
        if (values.Length > 1)
        {
            var firstType = values[0].GetType();
            for (var i = 1; i < values.Length; i++)
                if (values[i].GetType() != firstType)
                    throw new ArgumentException(
                        "All values in an In filter must be the same type (Text, Number, or Boolean).",
                        nameof(values));
        }
        return new InMetadataFilter(field, values);
    }

    /// <summary>Matches chunks satisfying all of the provided <paramref name="filters"/> (logical AND). Empty list matches everything.</summary>
    public static MetadataFilter And(params MetadataFilter[] filters) =>
        new AndMetadataFilter(filters);

    /// <summary>Matches chunks satisfying any of the provided <paramref name="filters"/> (logical OR). Empty list matches nothing.</summary>
    public static MetadataFilter Or(params MetadataFilter[] filters) =>
        new OrMetadataFilter(filters);

    /// <summary>Matches chunks that do not satisfy <paramref name="filter"/> (logical NOT).</summary>
    public static MetadataFilter Not(MetadataFilter filter) =>
        new NotMetadataFilter(filter);
}

/// <summary>Filters chunks by comparing a metadata field to a scalar value using a <see cref="MetadataOperator"/>.</summary>
public sealed record FieldMetadataFilter(
    [property: JsonPropertyName("field")]    string Field,
    [property: JsonPropertyName("operator")] MetadataOperator Operator,
    [property: JsonPropertyName("value")]    MetadataFilterValue Value) : MetadataFilter;

/// <summary>Filters chunks by checking whether a metadata field belongs to a set of values.</summary>
public sealed record InMetadataFilter(
    [property: JsonPropertyName("field")]  string Field,
    [property: JsonPropertyName("values")] IReadOnlyList<MetadataFilterValue> Values) : MetadataFilter;

/// <summary>Matches chunks satisfying all child filters (logical AND). Empty list matches everything.</summary>
public sealed record AndMetadataFilter(
    [property: JsonPropertyName("filters")] IReadOnlyList<MetadataFilter> Filters) : MetadataFilter;

/// <summary>Matches chunks satisfying any child filter (logical OR). Empty list matches nothing.</summary>
public sealed record OrMetadataFilter(
    [property: JsonPropertyName("filters")] IReadOnlyList<MetadataFilter> Filters) : MetadataFilter;

/// <summary>Matches chunks that do not satisfy the child filter (logical NOT).</summary>
public sealed record NotMetadataFilter(
    [property: JsonPropertyName("filter")] MetadataFilter Filter) : MetadataFilter;
