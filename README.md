# Wiry.Base32

[![NuGet](https://img.shields.io/nuget/v/Wiry.Base32.svg)](https://www.nuget.org/packages/Wiry.Base32) [![License](https://img.shields.io/github/license/wiry-net/base32.svg)](https://github.com/wiry-net/Base32/blob/master/LICENSE)

**Base32 and ZBase32 encoding and decoding library.**

AppVeyor (Windows): [![AppVeyor](https://ci.appveyor.com/api/projects/status/o4vfx9fx35vfmh37?svg=true)](https://ci.appveyor.com/project/dmitry-ra/base32)

Travis CI (Linux & macOS): [![Travis CI](https://travis-ci.org/wiry-net/Wiry.Base32.svg?branch=master)](https://travis-ci.org/wiry-net/Wiry.Base32)

### .NET compatibility:
- .NET Framework 4.5+
- .NET Standard 1.1+ (.NET Core, .NET 5 and later, Mono, Xamarin)

### Installation

To install Wiry.Base32, run the following command in the Package Manager Console
```
PM> Install-Package Wiry.Base32
```

### Usage example

```csharp
using System;
using System.Linq;
using System.Text;
using Wiry.Base32;

namespace Base32ConsoleApp
{
    public class Program
    {
        public static void Main()
        {
            // original text
            string text = "Hello, World!";
            Console.WriteLine($"Text: '{text}'");
            byte[] inputBytes = Encoding.ASCII.GetBytes(text);

            // Convert to RFC 4648 Base32 string
            string base32 = Base32Encoding.Standard.GetString(inputBytes);
            Console.WriteLine($"base32: '{base32}'");

            // Convert to z-base-32 string
            string zbase32 = Base32Encoding.ZBase32.GetString(inputBytes);
            Console.WriteLine($"z-base-32: '{zbase32}'");

            // Convert data back and check it
            byte[] decodedBase32 = Base32Encoding.Standard.ToBytes(base32);
            if (inputBytes.SequenceEqual(decodedBase32))
                Console.WriteLine("Convert back from base32 successfully!");

            byte[] decodedZBase32 = Base32Encoding.ZBase32.ToBytes(zbase32);
            if (inputBytes.SequenceEqual(decodedZBase32))
                Console.WriteLine("Convert back from z-base-32 successfully!");
        }
    }
}
```
Output:
```
Text: 'Hello, World!'
base32: 'JBSWY3DPFQQFO33SNRSCC==='
z-base-32: 'jb1sa5dxfoofq551pt1nn'
Convert back from base32 successfully!
Convert back from z-base-32 successfully!
```
### Validation example
```csharp
using System;
using Wiry.Base32;

namespace ValidateConsoleApp
{
    class Program
    {
        static void Main()
        {
            var standard = Base32Encoding.Standard;
            Console.WriteLine("S1) " + standard.Validate(null));
            Console.WriteLine("S2) " + standard.Validate("MZXW6YQ="));
            Console.WriteLine("S3) " + standard.Validate("MZ@W6YQ="));
            Console.WriteLine("S4) " + standard.Validate("MZXW6YQ=="));
            Console.WriteLine("S5) " + standard.Validate("========"));

            var zbase = Base32Encoding.ZBase32;
            Console.WriteLine("Z1) " + zbase.Validate(null));
            Console.WriteLine("Z2) " + zbase.Validate("gr3doqbw8radnqb3go"));
            Console.WriteLine("Z3) " + zbase.Validate("gr3doqbw8radnqb3goa"));
            Console.WriteLine("Z4) " + zbase.Validate("gr3doqbw!radnqb3go"));
        }
    }
}
```
Output:
```
S1) InvalidArguments
S2) Ok
S3) InvalidCharacter
S4) InvalidLength
S5) InvalidPadding
Z1) InvalidArguments
Z2) Ok
Z3) InvalidLength
Z4) InvalidCharacter
```
### Custom alphabets

`CustomBase32Encoding` uses the bit layout of RFC 4648 with any alphabet of 32 distinct
characters. The padding symbol must not be one of them; pass `null` for no padding, as z-base-32
does. The constructor throws `ArgumentException` otherwise.

For example, base32hex from RFC 4648 section 7:
```csharp
var base32Hex = new CustomBase32Encoding("0123456789ABCDEFGHIJKLMNOPQRSTUV", '=');

string encoded = base32Hex.GetString(Encoding.ASCII.GetBytes("foobar"));
Console.WriteLine(encoded);                                               // CPNMUOJ1E8======
Console.WriteLine(Encoding.ASCII.GetString(base32Hex.ToBytes(encoded)));  // foobar
```
Encodings that are more than an alphabet substitution, such as Crockford's Base32 (case folding,
aliases like `I` and `L` for `1`, a check symbol), cannot be expressed this way.

### Known decoder leniency

`ToBytes` accepts some input that RFC 4648 rejects, and `Validate` returns `Ok` for part of it.
1.x keeps this behavior so that input accepted today is still accepted:

- The number of padding symbols is not checked: `MZX=====` decodes to `66` like `MY======`, and
  `M=======` decodes to an empty array; `Validate` returns `Ok` for both. A single symbol before seven
  padding symbols is not decoded at all: `!=======` gives an empty array, while `Validate` returns
  `InvalidCharacter`.
- Without padding (z-base-32, custom alphabets with `null` padding) a single trailing symbol is
  ignored without being checked, and the bits of an incomplete trailing group are dropped at lengths
  3 and 6 modulo 8: `ZBase32.ToBytes("=")` is an empty array, `ZBase32.ToBytes("yyy")` is `00`.
  `Validate` returns `InvalidLength` for these.
- Unused trailing bits need not be zero, so different strings decode to the same bytes: `MY======`
  and `MZ======` both give `66`, z-base-32 `r3oi` and `r3oo` both give `26 61`. `Validate` returns
  `Ok` for all of them.

To accept canonical input only, compare the input with the encoding of the decoded bytes:
```csharp
byte[] bytes = Base32Encoding.Standard.ToBytes(text);
bool canonical = Base32Encoding.Standard.GetString(bytes) == text;
```

### Benchmarks

[Benchmark repository](https://github.com/dmitry-ra/benchmarks/tree/master/comparative/Base32Encoding)

Test configuration: Win10 x64 @ Intel Core 2 Quad Q9550.

**Test 1**: 1000000 repeats with 64 bytes of data:

|  Library (alphabetical order) | Encoding | Decoding   |
|  ---------------------------- | -------- | --------   |
|  .NET Base64 (baseline)       |  202 ms  |  336 ms    |
|  Albireo.Base32 v1.0.1        |  822 ms  |  3627 ms   |
|  Base3264-UrlEncoder v1.0.2   |  2839 ms |  43972 ms  |
|  SimpleBase v1.3.1            |  480 ms  |  673 ms    |
|  WallF.BaseNEncodings v1.0.0  |  587 ms  |  3342 ms   |
|**Wiry.Base32 v1.1.1**         |**331 ms**|**388 ms**  |

**Test 2**: random data size from 0 to 100 bytes (500 sessions by 20000 repeats):

![Encode duration](https://raw.githubusercontent.com/dmitry-ra/benchmarks/master/comparative/Base32Encoding/Base32BenchmarkNet452/results/encode-duration-chart-920x515_1.1.1_20180108.png)

![Decode duration](https://raw.githubusercontent.com/dmitry-ra/benchmarks/master/comparative/Base32Encoding/Base32BenchmarkNet452/results/decode-duration-chart-920x515_1.1.1_20180108.png)

### References
- [Base32 in Wikipedia](https://en.wikipedia.org/wiki/Base32)
- [RFC4648](https://tools.ietf.org/html/rfc4648)
- [z-base-32](https://philzimmermann.com/docs/human-oriented-base-32-encoding.txt)

### License
Copyright (c) Dmitry Razumikhin, 2016-2018.

Licensed under the MIT License.
