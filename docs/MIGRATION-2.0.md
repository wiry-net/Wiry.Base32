# Migrating from 1.x to 2.0

Wiry.Base32 2.0 decodes strictly. Encoding is unchanged: `GetString` returns the same string for
the same bytes as 1.x did. What changes is which strings `ToBytes` and `Validate` accept.

In 2.0 a string is accepted exactly when it is what the encoder would produce for some bytes:

- `ToBytes(s)` succeeds if and only if `Validate(s)` returns `Ok`;
- if `ToBytes(s)` succeeds, then `GetString(ToBytes(s)) == s`.

1.x accepted several kinds of strings that no encoder produces and silently dropped the extra
symbols or bits. RFC 4648 (section 3.5) allows decoders to reject such input, and 2.0 does.

## What 2.0 rejects

| Class | Example | 1.x `ToBytes` | 1.x `Validate` | 2.0 `ToBytes` | 2.0 `Validate` |
|---|---|---|---|---|---|
| A. Padding of 2 or 5 symbols | `MZX=====` | `66` | `Ok` | `FormatException` "Invalid padding" | `InvalidPadding` |
| B. One data symbol before 7 padding symbols | `M=======` | empty array | `Ok` | `FormatException` "Invalid padding" | `InvalidPadding` |
| C. Unpadded input of length 1, 3 or 6 mod 8 | z-base-32 `yyy` | `00` | `InvalidLength` | `FormatException` "Invalid length" | `InvalidLength` (unchanged) |
| D. Last symbol with non-zero unused bits | `MZXR====`, z-base-32 `yn` | `666f`, `00` | `Ok` | `FormatException` "Non-zero trailing bits" | `InvalidCharacter` |

Valid padding is 0, 1, 3, 4 or 6 symbols, the lengths an encoder produces for 5, 4, 3, 2 and 1
remaining bytes. Valid unpadded lengths are 0, 2, 4, 5 or 7 mod 8.

Class D is the "aliases" question: in 1.x `MZXQ====` and `MZXR====` both decode to `666f`, so two
different strings stand for the same bytes. Only the first is canonical.

Two smaller effects follow from the same checks:

- For input that 1.x already rejected because of an invalid character, `Validate` may now report
  `InvalidPadding` instead of `InvalidCharacter` when the padding is also wrong (`!=======`):
  padding is checked before characters.
- `ToBytes` on class B and C input no longer returns an empty array for a lone symbol (`T=======`,
  z-base-32 `y`); it throws.

Nothing else changed: the exception type is still `FormatException`, the `ValidationResult`
values keep their names and numbers, lowercase input to `Standard` is still rejected (as in 1.x),
and `Validate(null)` still returns `InvalidArguments`.

## How often each class showed up in testing

Both versions were run over one deterministic set of 2 312 324 generated cases (random data of
0 to 70 bytes, valid encodings, mutated encodings, random strings, windows into larger strings),
with Python's `base64` as an independent reference. The set is built to hit edge cases, so the
counts show which classes exist, not how common they are in real data.

Against 1.1.1 (`Standard` and `ZBase32`, 1 360 072 comparable cases), 102 541 answers differ:

| Class | Cases | `ToBytes`: bytes or empty array -> exception | `Validate`: `Ok` -> rejection | `Validate`: other rejection -> `InvalidPadding` |
|---|---|---|---|---|
| A | 3 412 | 1 129 | 1 173 | 1 110 |
| B | 1 035 | 420 | 417 | 198 |
| C | 38 625 | 38 625 | 0 | 0 |
| D | 59 469 | 29 710 | 29 759 | 0 |

`CustomBase32Encoding` (not in 1.1.1; compared with the unreleased 1.x code) adds 45 604 more of
the same four classes: A 3 532, B 927, C 15 126, D 26 019.

No other difference was found: every encoding answer is identical, and every case outside these
four classes gets the same answer as in 1.x. Against the Python reference, 2.0 rejects everything
Python rejects; the only disagreement is class D, which Python accepts.

## Finding affected data before upgrading

Strings that 2.0 will reject are exactly the ones that do not survive a round trip in 1.x. Run
this with 1.x over stored or received data (on the 566 404 `Standard` and `ZBase32` decoding cases of
the comparison above it predicted the 2.0 answer every time):

```csharp
static bool AcceptedBy20(Base32Encoding encoding, string s)
{
    try
    {
        return encoding.GetString(encoding.ToBytes(s)) == s;
    }
    catch (FormatException)
    {
        return false; // 1.x rejects it too
    }
}
```

## Accepting non-zero trailing bits

Some producers generate Base32 strings as random symbols rather than by encoding bytes (for
example some one-time password secrets). The last symbol of such a string often has non-zero
unused bits, which 2.0 rejects by default. To decode them as 1.x did, construct the encoding with
`allowNonZeroTrailingBits`:

```csharp
var standardLenient = new CustomBase32Encoding("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567", '=', allowNonZeroTrailingBits: true);
var zBase32Lenient = new CustomBase32Encoding("ybndrfg8ejkmcpqxot1uwisza345h769", null, allowNonZeroTrailingBits: true);
```

With it, class D input decodes again and `Validate` returns `Ok` for it; the unused bits are
ignored. Classes A, B and C have no opt-in: no encoder produces them, and the symbols 1.x dropped
were data that was lost without notice.

A class derived from `Base32Encoding` can override `AllowNonZeroTrailingBits` instead.
