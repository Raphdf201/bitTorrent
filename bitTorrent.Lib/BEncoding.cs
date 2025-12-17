using System.Runtime.InteropServices.JavaScript;
using System.Text;
using static bitTorrent.Lib.Constants;

namespace bitTorrent.Lib;

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

public static class BEncode
{
    public static byte[] Encode(object obj)
    {
        var buffer = new System.IO.MemoryStream();

        EncodeNextObject(buffer, obj);

        return buffer.ToArray();
    }

    public static void EncodeToFile(object obj, string path)
    {
        File.WriteAllBytes(path, Encode(obj));
    }

    private static void EncodeNextObject(System.IO.MemoryStream buffer, object obj)
    {
        switch (obj)
        {
            case byte[] bytes:
                EncodeByteArray(buffer, bytes);
                break;
            case string s:
                EncodeString(buffer, s);
                break;
            case long l:
                EncodeNumber(buffer, l);
                break;
            default:
            {
                if (obj.GetType() == typeof(List<object>))
                    EncodeList(buffer, (List<object>)obj);
                else if (obj.GetType() == typeof(Dictionary<string,object>))
                    EncodeDictionary(buffer, (Dictionary<string,object>)obj);
                else
                    throw new Exception("unable to encode type " + obj.GetType());
                break;
            }
        }
    }
    
    private static void EncodeNumber(System.IO.MemoryStream buffer, long input)
    {
        buffer.Append(Number);
        buffer.Append(Encoding.UTF8.GetBytes(Convert.ToString(input)));
        buffer.Append(End);
    }
    
    private static void EncodeByteArray(System.IO.MemoryStream buffer, byte[] input)
    {                        
        buffer.Append(Encoding.UTF8.GetBytes(Convert.ToString(input.Length)));
        buffer.Append(StringDivider);
        buffer.Append(input);
    }

    private static void EncodeString(System.IO.MemoryStream buffer, string input)
    {   
        EncodeByteArray(buffer, Encoding.UTF8.GetBytes(input));
    }
    
    private static void EncodeList(System.IO.MemoryStream buffer, List<object> input)
    {
        buffer.Append(List);
        foreach (var item in input)
            EncodeNextObject(buffer, item);
        buffer.Append(End);
    }
    
    private static void EncodeDictionary(System.IO.MemoryStream buffer, Dictionary<string,object> input)
    {
        buffer.Append(Dict);
        foreach (var key in input.Keys.ToList().OrderBy(x => BitConverter.ToString(Encoding.UTF8.GetBytes(x))))
        {
            EncodeString(buffer, key);
            EncodeNextObject(buffer, input[key]);
        }
        buffer.Append(End);
    }
}

internal static class Constants
{
    internal const byte Dict = 100;          // d
    internal const byte List = 108;          // l
    internal const byte Number = 105;        // i
    internal const byte End = 101;           // e
    internal const byte StringDivider = 58;  // :
}
