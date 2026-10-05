#!/usr/bin/env python3
"""Cross-check builds of Wiry.Base32 against Python and against each other.

    wiry_base32_check.py --probe PROBE.dll --work DIR \\
        --build 1.1.1-ns11=lib/netstandard1.1/Wiry.Base32.dll \\
        --build candidate-ns20=lib/netstandard2.0/Wiry.Base32.dll [--scale N] [--seed N]

run.sh next to this file does the whole gate for a package. One deterministic
case file is generated and every build runs it through wiry_base32_probe/.
Five questions are answered from the results:

  oracle    does each build agree with base64.b32encode/b32decode (RFC 4648),
            with z-base-32 derived from it by alphabet substitution, and with
            the same for custom alphabets; where Python rejects and the build
            accepts, the leniency is counted by Python's reason
  window    does a decode or validate of a window depend on characters outside
            it (an out-of-bounds read would show up as a difference)
  parity    does every pair of builds answer every case identically
  fuzz      did any call throw something other than the argument and format
            exceptions, or did the probe process die
  alphabet  does CustomBase32Encoding refuse an alphabet with a repeated symbol
            or with the padding symbol inside it

Custom-alphabet cases run only on builds that have CustomBase32Encoding; the
others answer NoSuchCodec and are excluded from parity for those cases.
"""

import argparse
import base64
import collections
import os
import random
import subprocess
import sys
from pathlib import Path

RFC = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"
ZB32 = "ybndrfg8ejkmcpqxot1uwisza345h769"
NANO = "13456789abcdefghijkmnopqrstuwxyz"
CYRILLIC = "".join(chr(c) for c in range(0x0410, 0x0430))
WIDE = chr(0x0000) + RFC[1:31] + chr(0xFFFF)
DUPLICATE = RFC[:31] + "A"
PAD_INSIDE = RFC[:31] + "="

# name -> (alphabet, pad or None, valid). Valid ones are checked against Python, invalid ones
# must be refused by the constructor.
CUSTOM = {
    "rfc": (RFC, "=", True),
    "zb32": (ZB32, None, True),
    "nano": (NANO, None, True),
    "cyrillic": (CYRILLIC, "=", True),
    "wide": (WIDE, "=", True),
    "duplicate": (DUPLICATE, "=", False),
    "pad-inside": (PAD_INSIDE, "=", False),
}

ALLOWED = {"System.ArgumentException", "System.ArgumentNullException",
           "System.ArgumentOutOfRangeException", "System.FormatException"}

# The probe answers NoSuchCodec when the build has no CustomBase32Encoding and
# Rejected:<exception> when the constructor throws.
REFUSED = {"NoSuchCodec", "Rejected:System.ArgumentException"}


def utf16(text):
    # Every generated char is a single UTF-16 unit (below U+10000), lone surrogates included.
    return "null" if text is None else "".join(f"{ord(ch):04x}" for ch in text)


def from_utf16(value):
    return "".join(chr(int(value[i:i + 4], 16)) for i in range(0, len(value), 4))


def codec_key(name):
    if name in ("std", "z"):
        return name
    alphabet, pad, _ = CUSTOM[name]
    return f"c:{''.join(f'{ord(c):04x}' for c in alphabet)}:{'-' if pad is None else f'{ord(pad):04x}'}"


def spec(name):
    if name == "std":
        return RFC, "="
    if name == "z":
        return ZB32, None
    alphabet, pad, _ = CUSTOM[name]
    return alphabet, pad


def has_oracle(name):
    return name in ("std", "z") or CUSTOM[name][2]


def oracle_encode(name, data):
    alphabet, pad = spec(name)
    text = base64.b32encode(data).decode("ascii")
    body = text.rstrip("=")
    body = body.translate(str.maketrans(RFC, alphabet))
    return body + ("" if pad is None else pad * (len(text) - len(text.rstrip("="))))


def oracle_decode(name, text):
    """Bytes, or the reason Python rejects the input."""
    alphabet, pad = spec(name)
    if pad is None:
        if len(text) % 8 in (1, 3, 6):
            return "reject: length"
        if any(ch not in alphabet for ch in text):
            return "reject: character"
        padded = text.translate(str.maketrans(alphabet, RFC))
        padded += "=" * (-len(padded) % 8)
    else:
        body = text.rstrip(pad)
        if any(ch not in alphabet for ch in body):
            return "reject: character"
        padded = body.translate(str.maketrans(alphabet, RFC)) + "=" * (len(text) - len(body))
    try:
        return base64.b32decode(padded)
    except Exception as exc:
        return "reject: " + str(exc).lower()


class Cases:
    def __init__(self, rng):
        self.rng = rng
        self.rows = []
        self.meta = []
        self.pairs = []

    def add(self, op, codec, payload, index="-", count="-", **meta):
        case_id = len(self.rows)
        self.rows.append(f"{case_id}\t{op}\t{codec_key(codec)}\t{payload}\t{index}\t{count}")
        self.meta.append(dict(op=op, codec=codec, **meta))
        return case_id


def random_unit(rng):
    pick = rng.random()
    if pick < 0.1:
        return rng.choice([chr(c) for c in (0x0000, 0xFFFF, 0xFEFF, 0x00E9, 0xD800, 0xDFFF)] + list("=-_ \t"))
    if pick < 0.2:
        return chr(rng.randrange(0xd800, 0xe000))
    return chr(rng.randrange(0, 0x10000))


def mutate(rng, text, alphabet, pad):
    chars = list(text)
    pool = alphabet + (pad or "=") + alphabet.lower() + alphabet.upper() + "=01!~"
    for _ in range(rng.randint(1, 3)):
        kind = rng.randrange(6)
        position = rng.randrange(len(chars) + 1)
        if kind == 0 and chars:
            chars[min(position, len(chars) - 1)] = rng.choice(pool)
        elif kind == 1 and chars:
            chars[min(position, len(chars) - 1)] = random_unit(rng)
        elif kind == 2:
            chars.insert(position, rng.choice(pool))
        elif kind == 3 and chars:
            del chars[min(position, len(chars) - 1)]
        elif kind == 4:
            chars.append(pad or "=")
        elif kind == 5 and chars:
            del chars[rng.randrange(len(chars)):]
    return "".join(chars)


def generate(rng, scale):
    cases = Cases(rng)
    codecs = ["std", "z"] + list(CUSTOM)
    for codec in codecs:
        alphabet, pad = spec(codec)
        per_length = (40 if codec in ("std", "z") else 8) * scale
        for length in range(71):
            for _ in range(per_length):
                data = rng.randbytes(length)
                cases.add("enc", codec, data.hex(), data=data)
                encoded = oracle_encode(codec, data)
                for op in ("dec", "val"):
                    cases.add(op, codec, utf16(encoded), text=encoded, data=data)
                    cases.add(op, codec, utf16(mutate(rng, encoded, alphabet, pad)), text=None)
                # Same window, two different surroundings: the answers must be identical.
                window = mutate(rng, encoded, alphabet, pad) if rng.random() < 0.5 else encoded
                for op in ("dec", "val"):
                    ids = []
                    for _ in range(2):
                        before = "".join(random_unit(rng) if rng.random() < 0.5 else rng.choice(alphabet + "=")
                                         for _ in range(rng.randint(0, 9)))
                        after = "".join(random_unit(rng) if rng.random() < 0.5 else rng.choice(alphabet + "=")
                                        for _ in range(rng.randint(0, 9)))
                        ids.append(cases.add(op, codec, utf16(before + window + after), len(before), len(window),
                                             text=window, windowed=True))
                    cases.pairs.append(tuple(ids))
            for _ in range(per_length):
                buffer = rng.randbytes(rng.randint(0, 80))
                index = rng.randint(0, len(buffer))
                count = rng.randint(0, len(buffer) - index)
                cases.add("enc", codec, buffer.hex(), index, count, data=buffer[index:index + count])
        for _ in range(per_length * 70):
            length = rng.randint(0, 72)
            source = rng.random()
            if source < 0.4:
                text = "".join(rng.choice(alphabet) for _ in range(length))
            elif source < 0.8:
                text = "".join(rng.choice(alphabet + (pad or "")) for _ in range(length))
            else:
                text = "".join(random_unit(rng) for _ in range(length))
            for op in ("dec", "val"):
                cases.add(op, codec, utf16(text), text=text)
        sample = oracle_encode(codec, b"fooba!")
        for index, count in ((-1, 0), (0, -1), (0, 11), (11, 0), (12, 0), (5, 7), (-2147483648, 0),
                             (2147483647, 1), (1, 2147483647), (2147483647, 2147483647)):
            cases.add("enc", codec, b"fooba!".hex(), index, count, data=None)
            for op in ("dec", "val"):
                cases.add(op, codec, utf16(sample), index, count, text=None)
        cases.add("enc", codec, "null", data=None)
        cases.add("enc", codec, "null", 0, 0, data=None)
        for op in ("dec", "val"):
            cases.add(op, codec, "null", text=None)
            cases.add(op, codec, "null", 0, 0, text=None)
    return cases


def run_probe(dotnet, probe, assembly, cases_path, out_path, timeout):
    env = dict(os.environ, DOTNET_GCgen0size="0x40000")
    finished = subprocess.run([dotnet, str(probe), "run", str(assembly), str(cases_path), str(out_path)],
                              env=env, capture_output=True, text=True, timeout=timeout)
    return finished.returncode, finished.stderr.strip()


def load_results(path):
    results = {}
    with open(path, encoding="utf-8") as handle:
        for line in handle:
            case_id, status, value = line.rstrip("\n").split("\t")
            results[int(case_id)] = (status, value)
    return results


def check_oracle(cases, results):
    tally = collections.Counter()
    examples = {}
    for case_id, meta in enumerate(cases.meta):
        status, value = results[case_id]
        if value == "NoSuchCodec" or not has_oracle(meta["codec"]):
            continue
        op, codec = meta["op"], meta["codec"]
        if op == "enc":
            data = meta["data"]
            if data is None:
                key = (op, codec, "argument error", status, value if status == "ex" else "")
            else:
                expected = oracle_encode(codec, data)
                key = (op, codec, "match" if status == "ok" and from_utf16(value) == expected else "MISMATCH")
        else:
            text = meta["text"]
            row = cases.rows[case_id].split("\t")
            if text is None:
                payload, index, count = row[3], row[4], row[5]
                if payload == "null":
                    key = (op, codec, "argument error", status, value)
                    tally[key] += 1
                    continue
                full = from_utf16(payload)
                if index == "-":
                    text = full
                else:
                    index, count = int(index), int(count)
                    if index < 0 or count < 0 or index > len(full) or count > len(full) - index:
                        key = (op, codec, "argument error", status, value)
                        tally[key] += 1
                        continue
                    text = full[index:index + count]
            expected = oracle_decode(codec, text)
            accepted = not isinstance(expected, str)
            if op == "dec":
                if accepted and status == "ok":
                    canonical = oracle_encode(codec, expected) == text
                    key = (op, codec, "match" if bytes.fromhex(value) == expected else "MISMATCH",
                           "canonical" if canonical else "non-canonical (alias)")
                elif accepted:
                    key = (op, codec, "STRICTER: python accepts, build throws " + value)
                elif status == "ok":
                    key = (op, codec, "LENIENT: python " + expected + ", build returns bytes")
                else:
                    key = (op, codec, "both reject", value)
            else:
                if accepted:
                    canonical = oracle_encode(codec, expected) == text
                    key = (op, codec, "python accepts" + ("" if canonical else " (alias)"), value)
                else:
                    key = (op, codec, "python " + expected, value)
        tally[key] += 1
        examples.setdefault(key, cases.rows[case_id])
    return tally, examples


def check_windows(cases, results):
    differing = [pair for pair in cases.pairs if results[pair[0]] != results[pair[1]]
                 and results[pair[0]][1] != "NoSuchCodec"]
    return len(cases.pairs), differing


def check_alphabets(cases, results):
    """Invalid alphabets: how each build answered, by verdict."""
    verdicts = collections.Counter()
    for case_id, meta in enumerate(cases.meta):
        if not has_oracle(meta["codec"]):
            value = results[case_id][1]
            verdicts[(meta["codec"], value if value in REFUSED else "ACCEPTED")] += 1
    return verdicts


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--probe", required=True, type=Path)
    parser.add_argument("--build", action="append", required=True, metavar="NAME=ASSEMBLY")
    parser.add_argument("--work", required=True, type=Path)
    parser.add_argument("--dotnet", default="dotnet")
    parser.add_argument("--scale", type=int, default=1)
    parser.add_argument("--seed", type=int, default=20261004)
    parser.add_argument("--timeout", type=int, default=1800)
    args = parser.parse_args()

    builds = [item.split("=", 1) for item in args.build]
    args.work.mkdir(parents=True, exist_ok=True)
    rng = random.Random(args.seed)
    cases = generate(rng, args.scale)
    cases_path = args.work / "cases.tsv"
    cases_path.write_text("\n".join(cases.rows) + "\n", encoding="ascii")
    print(f"cases: {len(cases.rows)} (seed {args.seed}, scale {args.scale}), window pairs: {len(cases.pairs)}")

    everything = {}
    failed = False
    for name, assembly in builds:
        out_path = args.work / f"results-{name}.tsv"
        code, stderr = run_probe(args.dotnet, args.probe, assembly, cases_path, out_path, args.timeout)
        print(f"\n=== {name}: {assembly}\nprobe exit code {code}{' - ' + stderr[-500:] if stderr else ''}")
        if code != 0:
            failed = True
            continue
        results = load_results(out_path)
        if len(results) != len(cases.rows):
            print(f"FAIL: {len(results)} results for {len(cases.rows)} cases")
            failed = True
            continue
        everything[name] = results

        foreign = collections.Counter(value for status, value in results.values()
                                      if status == "ex" and value not in ALLOWED
                                      and value != "NoSuchCodec" and value.removeprefix("Rejected:") not in ALLOWED)
        print(f"fuzz: exceptions outside the argument/format family: {dict(foreign) or 'none'}")
        failed |= bool(foreign)

        tally, examples = check_oracle(cases, results)
        print("oracle:")
        for key, count in sorted(tally.items(), key=lambda kv: [str(k) for k in kv[0]]):
            example = examples.get(key, "")
            shown = "" if "match" in key or "both reject" in key or "argument error" in key else f"   e.g. {example}"
            print(f"  {count:8}  {' | '.join(str(k) for k in key)}{shown}")
            failed |= any("MISMATCH" in str(k) for k in key)

        total, differing = check_windows(cases, results)
        print(f"window: {total} pairs, {len(differing)} answered differently"
              + (f", e.g. {differing[:3]}" if differing else ""))
        failed |= bool(differing)

        for (codec, verdict), count in sorted(check_alphabets(cases, results).items()):
            print(f"alphabet {codec}: {count} cases, {verdict}")
            failed |= verdict == "ACCEPTED"

    # Every pair, not only against the first build: cases a build cannot run (custom alphabets
    # on 1.1.1) would otherwise never be compared between the builds that can.
    names = list(everything)
    for position, base_name in enumerate(names):
        base = everything[base_name]
        for name in names[position + 1:]:
            results = everything[name]
            diff = collections.Counter()
            sample = {}
            compared = 0
            for case_id in range(len(cases.rows)):
                a, b = base[case_id], results[case_id]
                if "NoSuchCodec" in (a[1], b[1]):
                    continue
                compared += 1
                if a != b:
                    meta = cases.meta[case_id]
                    key = (meta["op"], meta["codec"], f"{a[0]}:{a[1][:24]} -> {b[0]}:{b[1][:24]}")
                    diff[key] += 1
                    sample.setdefault(key, cases.rows[case_id])
            print(f"\nparity {base_name} vs {name}: {compared} comparable cases, {sum(diff.values())} differ")
            for key, count in diff.most_common(20):
                print(f"  {count:8}  {key}   e.g. {sample[key]}")
            failed |= bool(diff)

    print(f"\n{'FAIL' if failed else 'PASS'}: wiry_base32_check")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
