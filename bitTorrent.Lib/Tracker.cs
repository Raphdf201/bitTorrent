namespace bitTorrent.Lib;

public class Tracker
{
    public readonly byte[] InfoHash;
    public readonly string PeerId;
    public readonly string? Ip;
    public readonly ushort Port;
    public string Uploaded;
    public string Downloaded;
    public string Left;
    public Event? Event;
    public string? FailureReason;
    public long? Interval;
    public List<Peer> Peers;
}

public class Peer
{
    public readonly string PeerId;
    public readonly string Ip;
    public readonly ushort Port;

    public Peer()
    {
        
    }
}

public enum Event
{
    Started,
    Completed,
    Stopped
}
