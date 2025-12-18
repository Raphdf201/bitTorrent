using System.Text;

namespace bitTorrent.Lib;

public class Torrent
{
    public readonly string Announce;
    public readonly InfoDict Info;
    public readonly byte[] InfoHash;

    public Torrent(byte[] file)
    {
        var dict = (Dictionary<string, object>)BDecode.Decode(file);

        Announce = dict["announce"] switch
        {
            string s => s,
            byte[] b => Encoding.UTF8.GetString(b),
            _ => throw new Exception("Invalid announce type")
        };

        Info = new InfoDict(dict["info"]);
        InfoHash = System.Security.Cryptography.SHA1.HashData(
            BEncode.Encode(dict["info"])
        );
    }
}

public class InfoDict
{
    public readonly long PieceLength;
    public readonly byte[][] Pieces;
    public readonly long? Length;
    public readonly List<FileDict>? Files;
    public readonly string Name;
    public readonly bool IsSingleFile;

    public InfoDict(object data)
    {
        var dict = (Dictionary<string, object>)data;

        PieceLength = (long)dict["piece length"];

        var piecesBytes = dict["pieces"] switch
        {
            byte[] b => b,
            string s => Encoding.GetEncoding("ISO-8859-1").GetBytes(s),
            _ => throw new Exception("Invalid pieces type")
        };
        
        Pieces = Enumerable.Range(0, piecesBytes.Length / 20)
            .Select(i => piecesBytes.Skip(i * 20).Take(20).ToArray())
            .ToArray();

        if (dict.TryGetValue("length", out var len))
        {
            IsSingleFile = true;
            Length = (long)len;
        }
        else if (dict.TryGetValue("files", out var files))
        {
            IsSingleFile = false;
            Files = FileDict.GetFiles(files);
        }
        else throw new Exception("Does not contain files or length");

        Name = (string)dict["name"];
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
        var filesList = (List<object>)data;
        var result = new List<FileDict>();
        
        foreach (var fileObj in filesList)
        {
            var fileDict = (Dictionary<string, object>)fileObj;
            var length = (long)fileDict["length"];
            
            var pathList = (List<object>)fileDict["path"];
            var paths = pathList.Select(p => p switch
            {
                string s => s,
                byte[] b => Encoding.UTF8.GetString(b),
                _ => throw new Exception("Invalid path component")
            }).ToArray();
            
            result.Add(new FileDict(length, paths));
        }
        
        return result;
    }
}
