// Per-launch randomness. Each scene calls Pick once when it is created, so its one random look
// (a colour, a planet type) is chosen when the program opens and stays the same until it closes.
public static class Launch
{
    // Test only: SLEEPY_VARIANT=n forces every scene to look number n (wrapped to the scene's own count), so --shot can check each one.
    static readonly int? forced = int.TryParse(Environment.GetEnvironmentVariable("SLEEPY_VARIANT"), out var v) ? v : null;

    /// <summary>A random whole number from 0 up to (not including) count, or the forced one in tests.</summary>
    public static int Pick(int count) => forced.HasValue ? ((forced.Value % count) + count) % count : Random.Shared.Next(count);
}
