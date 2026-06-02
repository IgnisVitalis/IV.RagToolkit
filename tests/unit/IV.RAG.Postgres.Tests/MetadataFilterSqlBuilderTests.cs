using FluentAssertions;

namespace IV.RAG.Tests;

public sealed class MetadataFilterSqlBuilderTests
{
    // ── leaf: field comparisons ──────────────────────────────────────────────

    [Fact]
    public void Eq_Text_ProducesTextExtractAndParameter()
    {
        var (sql, parameters) = MetadataFilterSqlBuilder.Build(MetadataFilter.Eq("department", "engineering"));

        sql.Should().Be("metadata->>'department' = @mf0");
        parameters.Should().ContainSingle(p => p.ParameterName == "mf0" && (string)p.Value! == "engineering");
    }

    [Fact]
    public void Eq_Number_ProducesNumericCastAndParameter()
    {
        var (sql, parameters) = MetadataFilterSqlBuilder.Build(MetadataFilter.Eq("year", 2023));

        sql.Should().Be("(metadata->>'year')::numeric = @mf0");
        parameters.Should().ContainSingle(p => p.ParameterName == "mf0" && (double)p.Value! == 2023d);
    }

    [Fact]
    public void Eq_Boolean_ProducesBooleanCastAndParameter()
    {
        var (sql, parameters) = MetadataFilterSqlBuilder.Build(MetadataFilter.Eq("active", true));

        sql.Should().Be("(metadata->>'active')::boolean = @mf0");
        parameters.Should().ContainSingle(p => p.ParameterName == "mf0" && (bool)p.Value! == true);
    }

    [Theory]
    [InlineData(MetadataOperator.Ne,  "!=")]
    [InlineData(MetadataOperator.Gt,  ">")]
    [InlineData(MetadataOperator.Gte, ">=")]
    [InlineData(MetadataOperator.Lt,  "<")]
    [InlineData(MetadataOperator.Lte, "<=")]
    public void FieldFilter_AllOperators_ProduceCorrectSqlOperator(MetadataOperator op, string expectedOp)
    {
        var filter = new FieldMetadataFilter("score", op, 0.5);
        var (sql, _) = MetadataFilterSqlBuilder.Build(filter);

        sql.Should().Contain(expectedOp);
    }

    // ── leaf: In ─────────────────────────────────────────────────────────────

    [Fact]
    public void In_Text_ProducesInClauseWithAllParameters()
    {
        var (sql, parameters) = MetadataFilterSqlBuilder.Build(
            MetadataFilter.In("status", "active", "pending"));

        sql.Should().Be("metadata->>'status' IN (@mf0, @mf1)");
        parameters.Should().HaveCount(2);
        parameters[0].Value.Should().Be("active");
        parameters[1].Value.Should().Be("pending");
    }

    [Fact]
    public void In_Number_ProducesNumericCastInClause()
    {
        var (sql, _) = MetadataFilterSqlBuilder.Build(
            MetadataFilter.In("year", 2020, 2021, 2022));

        sql.Should().Be("(metadata->>'year')::numeric IN (@mf0, @mf1, @mf2)");
    }

    [Fact]
    public void In_Empty_ReturnsFalse()
    {
        var (sql, parameters) = MetadataFilterSqlBuilder.Build(
            new InMetadataFilter("field", []));

        sql.Should().Be("FALSE");
        parameters.Should().BeEmpty();
    }

    // ── combinators ──────────────────────────────────────────────────────────

    [Fact]
    public void And_TwoConditions_ProducesParenthesizedAndExpression()
    {
        var filter = MetadataFilter.And(
            MetadataFilter.Eq("department", "engineering"),
            MetadataFilter.Gt("year", 2020));

        var (sql, parameters) = MetadataFilterSqlBuilder.Build(filter);

        sql.Should().Be("(metadata->>'department' = @mf0 AND (metadata->>'year')::numeric > @mf1)");
        parameters.Should().HaveCount(2);
    }

    [Fact]
    public void And_Empty_ReturnsTrue()
    {
        var (sql, parameters) = MetadataFilterSqlBuilder.Build(new AndMetadataFilter([]));

        sql.Should().Be("TRUE");
        parameters.Should().BeEmpty();
    }

    [Fact]
    public void And_Single_ReturnsSingleNodeWithoutParentheses()
    {
        var (sql, _) = MetadataFilterSqlBuilder.Build(
            MetadataFilter.And(MetadataFilter.Eq("x", "y")));

        sql.Should().Be("metadata->>'x' = @mf0");
    }

    [Fact]
    public void Or_TwoConditions_ProducesParenthesizedOrExpression()
    {
        var filter = MetadataFilter.Or(
            MetadataFilter.Eq("type", "pdf"),
            MetadataFilter.Eq("type", "docx"));

        var (sql, _) = MetadataFilterSqlBuilder.Build(filter);

        sql.Should().Be("(metadata->>'type' = @mf0 OR metadata->>'type' = @mf1)");
    }

    [Fact]
    public void Or_Empty_ReturnsFalse()
    {
        var (sql, _) = MetadataFilterSqlBuilder.Build(new OrMetadataFilter([]));

        sql.Should().Be("FALSE");
    }

    [Fact]
    public void Not_WrapsChildInNotParentheses()
    {
        var (sql, _) = MetadataFilterSqlBuilder.Build(
            MetadataFilter.Not(MetadataFilter.Eq("archived", true)));

        sql.Should().Be("NOT ((metadata->>'archived')::boolean = @mf0)");
    }

    // ── nested ───────────────────────────────────────────────────────────────

    [Fact]
    public void Nested_AndWithOr_ProducesCorrectSqlAndParameters()
    {
        var filter = MetadataFilter.And(
            MetadataFilter.Eq("department", "engineering"),
            MetadataFilter.Or(
                MetadataFilter.Gt("year", 2020),
                MetadataFilter.Lt("year", 2010)));

        var (sql, parameters) = MetadataFilterSqlBuilder.Build(filter);

        sql.Should().Be(
            "(metadata->>'department' = @mf0 AND ((metadata->>'year')::numeric > @mf1 OR (metadata->>'year')::numeric < @mf2))");
        parameters.Should().HaveCount(3);
    }

    // ── In: mixed type guard ──────────────────────────────────────────────────

    [Fact]
    public void In_MixedTypes_ViaFactory_ThrowsArgumentException()
    {
        var act = () => MetadataFilter.In("field", "text", 2020);

        act.Should().Throw<ArgumentException>().WithMessage("*same type*");
    }

    [Fact]
    public void In_MixedTypes_ViaDirectConstruction_ThrowsOnBuild()
    {
        var filter = new InMetadataFilter("field",
        [
            new MetadataFilterValue.Text("value"),
            new MetadataFilterValue.Number(42)
        ]);

        var act = () => MetadataFilterSqlBuilder.Build(filter);

        act.Should().Throw<ArgumentException>().WithMessage("*same type*");
    }

    // ── field name validation ─────────────────────────────────────────────────

    [Theory]
    [InlineData("field with spaces")]
    [InlineData("field-with-hyphen")]
    [InlineData("1starts_with_digit")]
    [InlineData("field.nested")]
    [InlineData("field;injection")]
    public void InvalidFieldName_ThrowsArgumentException(string badField)
    {
        var act = () => MetadataFilterSqlBuilder.Build(MetadataFilter.Eq(badField, "value"));

        act.Should().Throw<ArgumentException>().WithMessage($"*'{badField}'*");
    }

    [Theory]
    [InlineData("simple")]
    [InlineData("with_underscore")]
    [InlineData("_leading_underscore")]
    [InlineData("with123digits")]
    public void ValidFieldName_DoesNotThrow(string goodField)
    {
        var act = () => MetadataFilterSqlBuilder.Build(MetadataFilter.Eq(goodField, "value"));

        act.Should().NotThrow();
    }
}
