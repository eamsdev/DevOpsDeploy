using System.Text.Json;

namespace DevOpsDeploy.Tests.Helpers;

public static class TestDataReader
{
    public static async Task<T> ReadAsync<T>(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        return (await JsonSerializer.DeserializeAsync<T>(stream))!;
    }
}