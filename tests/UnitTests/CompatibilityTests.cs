// Copyright (c) Dmitry Razumikhin, 2016-2026.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

using Wiry.Base32;
using Xunit;

namespace UnitTests;

// Guards for the surface other people's code is compiled against: removing a virtual, or
// adding a member a subclass must implement, breaks the build here first.
public class CompatibilityTests
{
    private const string Encoded = "MZXW6YTBOI======";
    private const string Lower = "mzxw6ytboi======";
    private static readonly byte[] Foobar = "foobar"u8.ToArray();

    [Fact]
    public void Subclass_GetString_OverridesBothForms()
    {
        Base32Encoding encoding = new CaseInsensitiveEncoding();

        Assert.Equal(Encoded, encoding.GetString(Foobar));
        Assert.Equal(Encoded, encoding.GetString([0, .. Foobar, 0], 1, Foobar.Length));
    }

    [Fact]
    public void Subclass_ToBytes_OverridesBothForms()
    {
        Base32Encoding encoding = new CaseInsensitiveEncoding();

        Assert.Equal(Foobar, encoding.ToBytes(Lower));
        Assert.Equal(Foobar, encoding.ToBytes("--" + Lower + "--", 2, Lower.Length));
    }

    [Fact]
    public void Subclass_Validate_OverridesBothForms()
    {
        Base32Encoding encoding = new CaseInsensitiveEncoding();

        Assert.Equal(ValidationResult.Ok, encoding.Validate(Lower));
        Assert.Equal(ValidationResult.Ok, encoding.Validate("--" + Lower + "--", 2, Lower.Length));
        Assert.Equal(ValidationResult.InvalidArguments, encoding.Validate(null));
    }

    public static TheoryData<IBase32Encoding, string> Encodings => new()
    {
        { Base32Encoding.Standard, Encoded },
        { Base32Encoding.ZBase32, "c3zs6aubqe" },
        { new CustomBase32Encoding("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567", null), "MZXW6YTBOI" },
    };

    [Theory]
    [MemberData(nameof(Encodings))]
    public void Interface_GetString_DispatchesToEncoding(IBase32Encoding encoding, string encoded)
    {
        Assert.Equal(encoded, encoding.GetString(Foobar, 0, Foobar.Length));
    }

    [Theory]
    [MemberData(nameof(Encodings))]
    public void Interface_ToBytes_DispatchesToEncoding(IBase32Encoding encoding, string encoded)
    {
        Assert.Equal(Foobar, encoding.ToBytes(encoded, 0, encoded.Length));
    }

    [Theory]
    [MemberData(nameof(Encodings))]
    public void Interface_Validate_AcceptsNull(IBase32Encoding encoding, string encoded)
    {
        Assert.Equal(ValidationResult.Ok, encoding.Validate(encoded, 0, encoded.Length));
        Assert.Equal(ValidationResult.InvalidArguments, encoding.Validate(null, 0, 0));
    }

    // Written the way 1.1.1 has subclasses written: it supplies the six public members and
    // nothing else. Alphabet and PadSymbol are here only because this source declares them
    // abstract, which a subclass compiled against 1.1.1 cannot satisfy.
    private sealed class CaseInsensitiveEncoding : Base32Encoding
    {
        protected override string Alphabet => throw new NotSupportedException();
        protected override char? PadSymbol => throw new NotSupportedException();

        public override string GetString(byte[] bytes) => Standard.GetString(bytes);

        public override string GetString(byte[] bytes, int index, int count) =>
            Standard.GetString(bytes, index, count);

        public override byte[] ToBytes(string encoded) => Standard.ToBytes(encoded.ToUpperInvariant());

        public override byte[] ToBytes(string encoded, int index, int length) =>
            Standard.ToBytes(encoded.ToUpperInvariant(), index, length);

        public override ValidationResult Validate(string? encoded) =>
            Standard.Validate(encoded?.ToUpperInvariant());

        public override ValidationResult Validate(string? encoded, int index, int length) =>
            Standard.Validate(encoded?.ToUpperInvariant(), index, length);
    }
}
