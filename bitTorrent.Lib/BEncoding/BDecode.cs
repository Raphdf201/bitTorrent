using System.Text;
using static bitTorrent.Lib.BEncoding.Constants;

namespace bitTorrent.Lib.BEncoding;

public static class BDecode
{
    public static object Decode(byte[] input)
    {
        var enumerator = ((IEnumerable<byte>)input).GetEnumerator();
        enumerator.MoveNext();
        return DecodeNext(enumerator);
    }
    
    public static object DecodeFile(string path)
    {
        return File.Exists(path)
            ? Decode(File.ReadAllBytes(path))
            : throw new FileNotFoundException("unable to find file: " + path);
    }

    private static object DecodeNext(IEnumerator<byte> enumerator)
    {
        return enumerator.Current switch
        {
            Dict => DecodeDictionary(enumerator),
            List => DecodeList(enumerator),
            Number => DecodeNumber(enumerator),
            _ => DecodeByteArray(enumerator)
        };
    }

    private static long DecodeNumber(IEnumerator<byte> enumerator)
    {
        List<byte> bytes = [];

        while (enumerator.MoveNext())
        {
            if (enumerator.Current == End)
                break;
            
            bytes.Add(enumerator.Current);
        }

        return long.Parse(Encoding.UTF8.GetString(bytes.ToArray()));
    }

    private static byte[] DecodeByteArray(IEnumerator<byte> enumerator)
    {
        // Length of string
        List<byte> lengthBytes = [];

        do
        {
            if (enumerator.Current == StringDivider)
                break;

            lengthBytes.Add(enumerator.Current);
        }
        while (enumerator.MoveNext());

        var lengthString = Encoding.UTF8.GetString(lengthBytes.ToArray());

        if (!int.TryParse(lengthString, out var length))
            throw new Exception("unable to parse length of byte array");

        // Actual string
        var bytes = new byte[length];

        for (var i = 0; i < length; i++)
        {
            enumerator.MoveNext();
            bytes[i] = enumerator.Current;
        }

        return bytes;
    }

    private static List<object> DecodeList(IEnumerator<byte> enumerator)
    {
        List<object> list = [];

        while (enumerator.MoveNext())
        {
            if (enumerator.Current == End)
                break;

            list.Add(DecodeNext(enumerator));
        }

        return list;
    }

    private static Dictionary<string, object> DecodeDictionary(IEnumerator<byte> enumerator)
    {
        var dict = new Dictionary<string,object>();
        List<string> keys = [];

        while (enumerator.MoveNext())
        {
            if (enumerator.Current == End)
                break;

            var key = Encoding.UTF8.GetString(DecodeByteArray(enumerator));
            enumerator.MoveNext();
            var val = DecodeNext(enumerator);

            keys.Add(key);
            dict.Add(key, val);
        }

        var sortedKeys = keys.OrderBy(x => BitConverter.ToString(Encoding.UTF8.GetBytes(x)));
        return !keys.SequenceEqual(sortedKeys) ? throw new Exception("error loading dictionary: keys not sorted") : dict;
    }
}
