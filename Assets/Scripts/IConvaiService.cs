using System.Threading.Tasks;

/// <summary>
/// Defines the interface for any AI service that returns a "good"/"bad" response.
/// </summary>
public interface IConvaiService
{
    Task<(string good, string bad)> GetGoodBadResponses(string npcLine);
}

/// <summary>
/// Stub implementation you can test with before wiring up Convai.
/// </summary>
public class StubConvaiService : IConvaiService
{
    public Task<(string good, string bad)> GetGoodBadResponses(string npcLine)
    {
        return Task.FromResult((
            good: $"(GOOD) I’m sorry about \"{npcLine}\"—let me fix that now.",
            bad:  $"(BAD) That’s on you—maybe try ordering earlier next time."
        ));
    }
}
