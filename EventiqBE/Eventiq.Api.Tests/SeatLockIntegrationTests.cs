namespace Eventiq.Api.Tests;
[Trait("category", "integration")]
public class SeatLockIntegrationTests : RedisIntegrationTestBase
{
    [Fact]
    public async Task LockSeats_A_and_B_Should_Succeed()
    {
        var eventId = Guid.NewGuid();
        var seats = new List<string> { "A", "B" };

        var success = await RedisService.LockSeatsAsync(
            eventId,
            seats,
            TimeSpan.FromSeconds(30));

        Assert.True(success);
    }
    
    [Fact]
    public async Task LockSeats_AC_When_A_Already_Locked_Should_Fail()
    {
        var eventId = Guid.NewGuid();

        await RedisService.LockSeatsAsync(
            eventId,
            new List<string> { "A" },
            TimeSpan.FromSeconds(30));

        var success = await RedisService.LockSeatsAsync(
            eventId,
            new List<string> { "A", "C" },
            TimeSpan.FromSeconds(30));

        Assert.False(success);
    }

    [Fact]
    public async Task Concurrent_Lock_Seat_E_Should_Allow_Only_One()
    {
        var eventId = Guid.NewGuid();

        var start = new ManualResetEventSlim(false);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(async () =>
            {
                start.Wait();
                return await RedisService.LockSeatsAsync(
                    eventId,
                    new List<string> { "E" },
                    TimeSpan.FromSeconds(30));
            }))
            .ToList();

        start.Set(); 

        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, results.Count(r => r)); // count true
    }

}