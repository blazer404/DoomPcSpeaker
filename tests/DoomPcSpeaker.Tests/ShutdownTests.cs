using NUnit.Framework;

[TestFixture]
internal sealed class ShutdownTests
{
    [Test]
    public void Cleanup_performs_actions_only_once_for_repeated_calls()
    {
        Shutdown.Cleanup();
        Shutdown.Cleanup();
        Shutdown.Cleanup();

        Assert.That(Shutdown.CleanupCount, Is.EqualTo(1));
    }
}
