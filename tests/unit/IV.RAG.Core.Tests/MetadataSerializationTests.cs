using System.Text.Json;
using FluentAssertions;

namespace IV.RAG.Tests;

public sealed class MetadataSerializationTests
{
    // Serializes value, deserializes it, then re-serializes to verify stable output.
    private static void AssertRoundtrip<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        var result = JsonSerializer.Deserialize<T>(json)!;
        JsonSerializer.Serialize(result).Should().Be(json);
    }

    // ── Metadata: value equality ─────────────────────────────────────────────

    [Fact]
    public void Metadata_SameContent_AreEqual()
    {
        var a = new Metadata { ["department"] = "engineering", ["year"] = 2020 };
        var b = new Metadata { ["department"] = "engineering", ["year"] = 2020 };

        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Metadata_DifferentContent_AreNotEqual()
    {
        var a = new Metadata { ["department"] = "engineering" };
        var b = new Metadata { ["department"] = "hr" };

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Metadata_EqualityOperator_ConsistentWithEquals()
    {
        var a = new Metadata { ["x"] = "y" };
        var b = new Metadata { ["x"] = "y" };
        var c = new Metadata { ["x"] = "z" };

        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
        (a == null).Should().BeFalse();
        (null as Metadata == null).Should().BeTrue();
    }

    [Fact]
    public void Metadata_EqualInstances_HaveSameHashCode()
    {
        var a = new Metadata { ["x"] = "y", ["n"] = 1 };
        var b = new Metadata { ["x"] = "y", ["n"] = 1 };

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    // ── Metadata: serialization ───────────────────────────────────────────────

    [Fact]
    public void Metadata_Empty_RoundTrips()
    {
        var json = JsonSerializer.Serialize(new Metadata());
        var result = JsonSerializer.Deserialize<Metadata>(json)!;

        result.Should().BeEmpty();
    }

    [Fact]
    public void Metadata_AllValueTypes_RoundTrip()
    {
        var original = new Metadata { ["text"] = "hello", ["number"] = 42.0, ["flag"] = true };

        var json = JsonSerializer.Serialize(original);
        var result = JsonSerializer.Deserialize<Metadata>(json)!;

        result["text"].Should().Be(new MetadataFilterValue.Text("hello"));
        result["number"].Should().Be(new MetadataFilterValue.Number(42.0));
        result["flag"].Should().Be(new MetadataFilterValue.Boolean(true));
    }

    [Fact]
    public void Metadata_SerializesAsPlainJsonObject()
    {
        var json = JsonSerializer.Serialize(new Metadata { ["source"] = "doc.txt", ["page"] = 3 });

        json.Should().Be("{\"source\":\"doc.txt\",\"page\":3}");
    }

    // ── MetadataFilter: type discriminator ───────────────────────────────────

    [Theory]
    [InlineData("field")]
    public void FieldMetadataFilter_SerializesWithDiscriminator(string discriminator)
    {
        var json = JsonSerializer.Serialize<MetadataFilter>(MetadataFilter.Eq("x", "y"));
        json.Should().Contain($"\"type\":\"{discriminator}\"");
    }

    [Theory]
    [InlineData("and")]
    [InlineData("or")]
    [InlineData("not")]
    [InlineData("in")]
    public void CompositeFilters_SerializeWithCorrectDiscriminator(string discriminator)
    {
        MetadataFilter filter = discriminator switch
        {
            "and" => MetadataFilter.And(MetadataFilter.Eq("x", "y")),
            "or"  => MetadataFilter.Or(MetadataFilter.Eq("x", "y")),
            "not" => MetadataFilter.Not(MetadataFilter.Eq("x", "y")),
            "in"  => MetadataFilter.In("x", "y"),
            _     => throw new ArgumentOutOfRangeException()
        };

        JsonSerializer.Serialize<MetadataFilter>(filter).Should().Contain($"\"type\":\"{discriminator}\"");
    }

    // ── MetadataFilter: leaf roundtrips ──────────────────────────────────────

    [Fact]
    public void FieldMetadataFilter_TextValue_RoundTrips() =>
        AssertRoundtrip<MetadataFilter>(MetadataFilter.Eq("department", "engineering"));

    [Fact]
    public void FieldMetadataFilter_NumberValue_RoundTrips() =>
        AssertRoundtrip<MetadataFilter>(MetadataFilter.Gt("year", 2020));

    [Fact]
    public void FieldMetadataFilter_BooleanValue_RoundTrips() =>
        AssertRoundtrip<MetadataFilter>(MetadataFilter.Eq("active", true));

    [Fact]
    public void InMetadataFilter_RoundTrips() =>
        AssertRoundtrip<MetadataFilter>(MetadataFilter.In("status", "active", "pending"));

    // ── MetadataFilter: composite roundtrips ─────────────────────────────────

    [Fact]
    public void AndMetadataFilter_RoundTrips() =>
        AssertRoundtrip<MetadataFilter>(MetadataFilter.And(
            MetadataFilter.Eq("department", "engineering"),
            MetadataFilter.Gt("year", 2020)));

    [Fact]
    public void OrMetadataFilter_RoundTrips() =>
        AssertRoundtrip<MetadataFilter>(MetadataFilter.Or(
            MetadataFilter.Eq("type", "pdf"),
            MetadataFilter.Eq("type", "docx")));

    [Fact]
    public void NotMetadataFilter_RoundTrips() =>
        AssertRoundtrip<MetadataFilter>(MetadataFilter.Not(MetadataFilter.Eq("archived", true)));

    [Fact]
    public void NestedFilter_ComplexTree_RoundTrips() =>
        AssertRoundtrip<MetadataFilter>(MetadataFilter.And(
            MetadataFilter.Eq("department", "engineering"),
            MetadataFilter.Or(
                MetadataFilter.Gt("year", 2020),
                MetadataFilter.Not(MetadataFilter.In("status", "archived", "deleted")))));
}
