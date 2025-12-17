namespace bitTorrent.Lib.Extensions;

// source: Fredrik Mörk (http://stackoverflow.com/a/4015634)
// Simplified by Raphdf201
internal static class MemoryStream
{
    extension(System.IO.MemoryStream stream)
    {
        public void Append(byte value)
        {
            stream.Append([value]);
        }

        public void Append(byte[] values)
        {
            stream.Write(values, 0, values.Length);
        }
    }
}
