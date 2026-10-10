using Xunit;

namespace InspiredCodes.BarcodeScanDetector.Tests;

/// <summary>
/// R10: the sender is the engine; OD-5: the event carries the scan's timestamp.
/// </summary>
public class EventTests
{

    [Fact]
    public void Sender_IsTheEngine()
    {
        var r = new Recorder();

        r.Type("ABC", 0);
        r.Key("\r", 3);

        Assert.Same(r.Engine, Assert.Single(r.Senders));
    }

    [Fact]
    public void Timestamp_IsWhenTheTerminatorArrived()
    {
        var r = new Recorder();

        r.Type("ABC", 0);
        r.Key("\r", 7.5);

        Assert.Equal(Recorder.Ms(7.5), Assert.Single(r.Events).Timestamp);
    }

    [Fact]
    public void InputText_DoesNotContainTheTerminator()
    {
        var r = new Recorder();

        r.Type("ABC", 0);
        r.Key("\r\n", 3);

        Assert.Equal("ABC", Assert.Single(r.Events).InputText);
    }

}
