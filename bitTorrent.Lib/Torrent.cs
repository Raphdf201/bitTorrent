namespace bitTorrent.Lib;

public class Torrent
{
    public readonly string Announce;
    public readonly InfoDict Info;

    public Torrent(byte[] file)
    {
        var thing = (Dictionary<string, object>)BDecode.Decode(file);
        Announce = (string)thing["announce"];
        Info = new InfoDict(thing["info"]);
    }
}

public class InfoDict
{
    public readonly long PieceLength;
    public readonly string[] Pieces;// TODO : use byte array
    public readonly long? Length;
    public readonly List<FileDict>? Files;
    public readonly string Name;
    public readonly bool IsSingleFile;
    // TODO : compute info hash

    public InfoDict(object data)
    {
        var thing = (Dictionary<string, object>)data;

        PieceLength = (long)thing["piece length"];

        var allPieces = (string)thing["pieces"];
        Pieces = Enumerable.Range(0, allPieces.Length / 20)
            .Select(i => allPieces.Substring(i * 20, 20))
            .ToArray();

        if (thing.TryGetValue("length", out var value))
        {
            IsSingleFile = true;
            Length = (long)value;
        }
        else
        {
            IsSingleFile = false;
            Files = FileDict.GetFiles(thing["files"]);
        }

        Name = (string)thing["name"];
    }
}

public class FileDict
{
    public readonly long Length;
    public readonly string[] Path;
    
    private FileDict(long len, string[] paths)
    {
        Length = len;
        Path = paths;
    }

    public static List<FileDict> GetFiles(object data)
    {
        return [new FileDict(0, [""])];// TODO
    }
}
