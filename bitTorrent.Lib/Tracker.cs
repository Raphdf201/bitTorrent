using System.Net;
using MiscUtil.Conversion;

namespace bitTorrent.Lib;

public enum TrackerEvent
{
    Started,
    Paused,
    Stopped
}

public class Tracker
{
    private HttpWebRequest httpWebRequest;

    public Tracker(string address)
    {
        Address = address;
    }

    public string Address { get; }

    public DateTime LastPeerRequest { get; private set; } = DateTime.MinValue;
    public TimeSpan PeerRequestInterval { get; private set; } = TimeSpan.FromMinutes(30);
    public event EventHandler<List<IPEndPoint>> PeerListUpdated;

    #region Helper

    public override string ToString()
    {
        return string.Format("[Tracker: {0}]", Address);
    }

    #endregion

    #region Announcing

    public void Update(Torrent torrent, TrackerEvent ev, string id, int port)
    {
        if (ev == TrackerEvent.Started && DateTime.UtcNow < LastPeerRequest.Add(PeerRequestInterval))
            return;

        LastPeerRequest = DateTime.UtcNow;

        var url = string.Format(
            "{0}?info_hash={1}&peer_id={2}&port={3}&uploaded={4}&downloaded={5}&left={6}&event={7}&compact=1",
            Address, torrent.UrlSafeStringInfohash,
            id, port,
            torrent.Uploaded, torrent.Downloaded, torrent.Left,
            Enum.GetName(typeof(TrackerEvent), ev).ToLower());

        Request(url);
    }

    private void Request(string url)
    {
        httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
        httpWebRequest.BeginGetResponse(HandleResponse, null);
    }

    private void HandleResponse(IAsyncResult result)
    {
        byte[] data;

        using (var response = (HttpWebResponse)httpWebRequest.EndGetResponse(result))
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("error reaching tracker " + this + ": " + response.StatusCode + " " +
                                  response.StatusDescription);
                return;
            }

            using (var stream = response.GetResponseStream())
            {
                data = new byte[response.ContentLength];
                stream.Read(data, 0, Convert.ToInt32(response.ContentLength));
            }
        }

        var info = BDecode.Decode(data) as Dictionary<string, object>;

        if (info == null)
        {
            Console.WriteLine("unable to decode tracker announce response");
            return;
        }

        PeerRequestInterval = TimeSpan.FromSeconds((long)info["interval"]);
        var peerInfo = (byte[])info["peers"];

        var peers = new List<IPEndPoint>();
        for (var i = 0; i < peerInfo.Length / 6; i++)
        {
            var offset = i * 6;
            var address = peerInfo[offset] + "." + peerInfo[offset + 1] + "." + peerInfo[offset + 2] + "." +
                          peerInfo[offset + 3];
            int port = EndianBitConverter.Big.ToChar(peerInfo, offset + 4);

            peers.Add(new IPEndPoint(IPAddress.Parse(address), port));
        }

        var handler = PeerListUpdated;
        if (handler != null)
            handler(this, peers);
    }

    public void ResetLastRequest()
    {
        LastPeerRequest = DateTime.MinValue;
    }

    #endregion
}