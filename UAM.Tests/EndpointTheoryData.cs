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

    public static IEnumerable<object[]> UserMissingEndpointCases()
    {
        yield return ["update"];
        yield return ["patch"];
        yield return ["delete"];
        yield return ["restore"];
        yield return ["activate"];
        yield return ["deactivate"];
        yield return ["get-preferences"];
        yield return ["put-preferences"];
    }

    public static IEnumerable<object[]> DeleteRestoreSequenceCases()
    {
        yield return ["delete-restore"];
        yield return ["delete-twice"];
    }
}
