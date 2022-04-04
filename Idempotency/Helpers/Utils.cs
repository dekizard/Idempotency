using Newtonsoft.Json;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Idempotency.Helpers;

public static class Utils
{
    public static string GetHash(string input)
    {
        var hashAlgorithm = SHA256.Create();
        var data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

        var strBuilder = new StringBuilder();
        foreach (var b in data)
        {
            strBuilder.Append(b.ToString("x2"));
        }

        return strBuilder.ToString();
    }

    public static byte[] SerializeAndCompress(this object obj)
    {
        var jsonString = JsonConvert.SerializeObject(obj,
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

        var encodedData = Encoding.UTF8.GetBytes(jsonString);

        return Compress(encodedData);
    }

    public static IReadOnlyDictionary<string, object> DecompressAndDeserialize(this byte[] compressedBytes)
    {
        var decompressedBytes = Decompress(compressedBytes);

        var jsonString = Encoding.UTF8.GetString(decompressedBytes);

        return JsonConvert.DeserializeObject<IReadOnlyDictionary<string, object>>(jsonString,
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
    }

    private static byte[] Compress(byte[] input)
    {
        using var outputStream = new MemoryStream();
        using (var zip = new GZipStream(outputStream, CompressionMode.Compress))
        {
            zip.Write(input, 0, input.Length);
        }

        var compressesData = outputStream.ToArray();

        return compressesData;
    }

    private static byte[] Decompress(byte[] input)
    {    
        using var outputStream = new MemoryStream();
        using (var inputStream = new MemoryStream(input))
        {
            using var zip = new GZipStream(inputStream, CompressionMode.Decompress);
            zip.CopyTo(outputStream);
        }

        var decompressedData = outputStream.ToArray();

        return decompressedData;
    }
}

