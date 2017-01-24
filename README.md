# Wiry.Base32

[![NuGet](https://img.shields.io/nuget/v/Wiry.Base32.svg)](https://www.nuget.org/packages/Wiry.Base32) [![License](https://img.shields.io/github/license/wiry-net/base32.svg)](https://github.com/wiry-net/Base32/blob/master/LICENSE)

**Base32 and ZBase32 encoding and decoding library.**

AppVeyor (Windows): [![AppVeyor](https://ci.appveyor.com/api/projects/status/o4vfx9fx35vfmh37?svg=true)](https://ci.appveyor.com/project/dmitry-ra/base32)

Travis CI (Linux): [![Travis CI](https://travis-ci.org/wiry-net/Base32.svg?branch=master)](https://travis-ci.org/wiry-net/Base32)

### .NET compatibility:
- .NET Framework (4.5.2+)
- .NET Core (netstandard 1.6+)

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
### Benchmarks

[Benchmark repository](https://github.com/dmitry-ra/benchmarks/tree/master/comparative/Base32Encoding)

Test configuration: Win10 x64 @ Intel Core 2 Quad Q9550.

**Test 1**: 1000000 repeats with 64 bytes of data:

|  Library (alphabetical order) | Encoding | Decoding   |
|  ---------------------------- | -------- | --------   |
|  .NET Base64 (baseline)       |  219 ms  |  371 ms    |
|  Albireo.Base32 v1.0.1        |  954 ms  |  3790 ms   |
|  Base3264-UrlEncoder v1.0.1   |  2581 ms |  46706 ms  |
|  SimpleBase v1.2.0            |  513 ms  |  642 ms    |
|  WallF.BaseNEncodings v1.0.0  |  603 ms  |  3528 ms   |
|**Wiry.Base32 v1.0.5**         |**372 ms**|**410 ms**  |

**Test 2**: random data size from 0 to 100 bytes (500 sessions by 20000 repeats):

![Encode duration](https://raw.githubusercontent.com/dmitry-ra/benchmarks/master/comparative/Base32Encoding/Base32BenchmarkNet452/results/encode-duration-chart-920x515.png)

![Decode duration](https://raw.githubusercontent.com/dmitry-ra/benchmarks/master/comparative/Base32Encoding/Base32BenchmarkNet452/results/decode-duration-chart-920x515.png)

### References
- [Base32 in Wikipedia](https://en.wikipedia.org/wiki/Base32)
- [RFC4648](https://tools.ietf.org/html/rfc4648)
- [z-base-32](https://philzimmermann.com/docs/human-oriented-base-32-encoding.txt)

### License
Copyright (c) Dmitry Razumikhin, 2016-2017.

Licensed under the MIT License.
