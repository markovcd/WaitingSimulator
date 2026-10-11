namespace WaitingSim.Tests;

public sealed class TraitsTests
{
    private const string Valid = """
        {
          "version": 1,
          "traits": [
            { "id": "patient", "drainScale": 0.5, "recoveryScale": 2.0, "bumpScale": 0.5, "shownStages": ["calm", "restless", "annoyed", "confronting", "fedUp"] },
            { "id": "passive-aggressive", "drainScale": 1.0, "recoveryScale": 0.5, "bumpScale": 1.0, "shownStages": ["calm", "fedUp"] }
          ]
        }
        """;

    [Fact]
    public void The_shipped_traits_load_without_problems()
    {
        var parsed = Traits.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "traits.json")));

        Assert.Empty(parsed.Problems);
        Assert.NotEmpty(parsed.Value!);
    }

    [Fact]
    public void A_traits_file_reads_into_its_traits_in_file_order()
    {
        var traits = Traits.Parse(Valid).Value!;

        Assert.Equal(["patient", "passive-aggressive"], traits.Select(trait => trait.Id).ToList());
        Assert.Equal(1.0f, traits[1].DrainScale);
        Assert.Equal([Stage.Calm, Stage.FedUp], traits[1].ShownStages.Order().ToList());
    }

    [Theory]
    [InlineData("\"id\": \"passive-aggressive\"", "\"id\": \"patient\"", "traits[1].id: \"patient\" is already used by an earlier trait")]
    [InlineData("[\"calm\", \"fedUp\"]", "[\"calm\", \"livid\"]", "traits[1].shownStages[1]: not a stage; stages are calm, restless, annoyed, confronting, fedUp")]
    [InlineData("[\"calm\", \"fedUp\"]", "[\"fedUp\"]", "traits[1].shownStages: must include calm")]
    [InlineData("\"drainScale\": 1.0", "\"drainScale\": 0", "traits[1].drainScale: missing, or not a positive number")]
    public void A_bad_trait_is_reported_by_position_and_field(string field, string replacement, string problem)
    {
        var parsed = Traits.Parse(Valid.Replace(field, replacement, StringComparison.Ordinal));

        Assert.Null(parsed.Value);
        Assert.Equal([problem], parsed.Problems);
    }

    [Fact]
    public void A_file_from_a_newer_version_is_refused_whole()
    {
        var parsed = Traits.Parse(Valid.Replace("\"version\": 1", "\"version\": 2", StringComparison.Ordinal));

        Assert.Equal(["version: 2 is from a newer game; this one reads up to 1"], parsed.Problems);
    }
}
