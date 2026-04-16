namespace UAM.Tests;

internal static class EndpointTheoryData
{
    public static IEnumerable<object[]> PaginationNormalizationCases()
    {
        yield return [-1, -1, 0, 20];
        yield return [-5, 1000, 0, 100];
        yield return [0, 0, 0, 20];
        yield return [5, 10, 5, 10];
    }

    public static IEnumerable<object[]> StoryMissingEndpointCases()
    {
        yield return ["publish"];
        yield return ["unpublish"];
        yield return ["views"];
        yield return ["likes"];
        yield return ["dislikes"];
    }

    public static IEnumerable<object[]> StoryUpdatePayloadCases()
    {
        yield return ["trim-fields"];
        yield return ["duplicate-slug"];
    }

    public static IEnumerable<object[]> DeleteRestoreSequenceCases()
    {
        yield return ["delete-restore"];
        yield return ["delete-twice"];
    }
}
