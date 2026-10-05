// Compiled against Wiry.Base32 1.1.1 and then run with a candidate assembly in its place. The
// subclass is written the only way 1.1.1 allowed, so any 1.x release must load and run it.
using System;
using System.Reflection;
using System.Text;
using Wiry.Base32;

internal sealed class LowerCaseStandard : Base32Encoding
{
    public override string GetString(byte[] bytes, int index, int count) =>
        Standard.GetString(bytes, index, count).ToLowerInvariant();

    public override byte[] ToBytes(string encoded, int index, int length) =>
        Standard.ToBytes(encoded.ToUpperInvariant(), index, length);

    public override ValidationResult Validate(string encoded, int index, int length) =>
        Standard.Validate(encoded.ToUpperInvariant(), index, length);
}

internal static class Program
{
    private static int Main()
    {
        var library = typeof(Base32Encoding).Assembly;
        Console.WriteLine(library.GetName().FullName + ", file version " +
            library.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);

        Base32Encoding encoding = new LowerCaseStandard();
        var foobar = Encoding.ASCII.GetBytes("foobar");
        var encoded = encoding.GetString(foobar);
        var actual = string.Join(" ",
            encoded,
            Encoding.ASCII.GetString(encoding.ToBytes(encoded)),
            encoding.Validate(encoded),
            Base32Encoding.ZBase32.GetString(foobar));

        const string expected = "mzxw6ytboi====== foobar Ok c3zs6aubqe";
        Console.WriteLine(actual == expected ? "PASS " + actual : "FAIL " + actual + ", expected " + expected);
        return actual == expected ? 0 : 1;
    }
}
