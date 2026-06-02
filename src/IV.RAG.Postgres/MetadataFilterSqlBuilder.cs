using System.Text.RegularExpressions;
using Npgsql;

namespace IV.RAG;

internal sealed class MetadataFilterSqlBuilder
{
    private static readonly Regex SafeField =
        new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

    private readonly List<NpgsqlParameter> _parameters = [];
    private int _index;

    /// <summary>Translates a <see cref="MetadataFilter"/> tree into a SQL fragment and its parameters.</summary>
    public static (string Sql, List<NpgsqlParameter> Parameters) Build(MetadataFilter filter)
    {
        var builder = new MetadataFilterSqlBuilder();
        return (builder.Node(filter), builder._parameters);
    }

    private string Node(MetadataFilter filter) => filter switch
    {
        FieldMetadataFilter f                  => Field(f),
        InMetadataFilter f                     => In(f),
        AndMetadataFilter { Filters: var fs }  => Logical(fs, "AND", "TRUE"),
        OrMetadataFilter  { Filters: var fs }  => Logical(fs, "OR",  "FALSE"),
        NotMetadataFilter { Filter: var f }    => $"NOT ({Node(f)})",
        _ => throw new ArgumentException($"Unsupported filter type: {filter.GetType().Name}")
    };

    private string Field(FieldMetadataFilter f)
    {
        ValidateField(f.Field);
        var op = f.Operator switch
        {
            MetadataOperator.Eq  => "=",
            MetadataOperator.Ne  => "!=",
            MetadataOperator.Gt  => ">",
            MetadataOperator.Gte => ">=",
            MetadataOperator.Lt  => "<",
            MetadataOperator.Lte => "<=",
            _ => throw new ArgumentException($"Unknown operator: {f.Operator}")
        };
        var p = AddParam(f.Value);
        return $"{Lhs(f.Field, f.Value)} {op} @{p}";
    }

    private string In(InMetadataFilter f)
    {
        ValidateField(f.Field);
        if (f.Values.Count == 0) return "FALSE";

        var firstType = f.Values[0].GetType();
        for (var i = 1; i < f.Values.Count; i++)
            if (f.Values[i].GetType() != firstType)
                throw new ArgumentException(
                    "All values in an In filter must be the same type (Text, Number, or Boolean).");

        var pnames = f.Values.Select(v => AddParam(v)).ToList();
        var lhs = Lhs(f.Field, f.Values[0]);
        return $"{lhs} IN ({string.Join(", ", pnames.Select(p => $"@{p}"))})";
    }

    private string Logical(IReadOnlyList<MetadataFilter> filters, string op, string whenEmpty)
    {
        if (filters.Count == 0) return whenEmpty;
        if (filters.Count == 1) return Node(filters[0]);
        return $"({string.Join($" {op} ", filters.Select(Node))})";
    }

    private static string Lhs(string field, MetadataFilterValue value) => value switch
    {
        MetadataFilterValue.Text    => $"metadata->>'{field}'",
        MetadataFilterValue.Number  => $"(metadata->>'{field}')::numeric",
        MetadataFilterValue.Boolean => $"(metadata->>'{field}')::boolean",
        _ => throw new ArgumentException($"Unsupported value type: {value.GetType().Name}")
    };

    private string AddParam(MetadataFilterValue value)
    {
        var name = $"mf{_index++}";
        object dbValue = value switch
        {
            MetadataFilterValue.Text(var s)    => s,
            MetadataFilterValue.Number(var n)  => n,
            MetadataFilterValue.Boolean(var b) => b,
            _ => throw new ArgumentException($"Unsupported value type: {value.GetType().Name}")
        };
        _parameters.Add(new NpgsqlParameter(name, dbValue));
        return name;
    }

    private static void ValidateField(string field)
    {
        if (!SafeField.IsMatch(field))
            throw new ArgumentException(
                $"Metadata field name '{field}' is invalid. Use only letters, digits, and underscores, starting with a letter or underscore.",
                nameof(field));
    }
}
