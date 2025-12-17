using System.Text;
using bitTorrent.Lib.Extensions;
using static bitTorrent.Lib.BEncoding.Constants;

namespace bitTorrent.Lib.BEncoding;

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
