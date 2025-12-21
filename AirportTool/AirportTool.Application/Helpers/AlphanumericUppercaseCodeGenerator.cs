using System.Security.Cryptography;
using System.Text;

namespace AirportTool.Application;

public class AlphanumericUppercaseCodeGenerator : IUniqueCodeGenerator
{
    private const string Charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int CodeLength = 8;

    public string Generate()
    {
        var sb = new StringBuilder(CodeLength);
        for (int i = 0; i < CodeLength; i++)
        {
            int index = RandomNumberGenerator.GetInt32(Charset.Length);
            sb.Append(Charset[index]);
        }
        return sb.ToString();
    }
}
