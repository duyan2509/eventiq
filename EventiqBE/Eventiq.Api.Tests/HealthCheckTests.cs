namespace Eventiq.Api.Tests;

public class HealthCheckTests
{
    [Fact]
    public async Task Api_Should_Return_Healthy()
    {
        using var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:5000/health");
        Assert.True(response.IsSuccessStatusCode);
    }
}
