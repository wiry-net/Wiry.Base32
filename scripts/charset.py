#!/usr/bin/env python3
"""Fail the gate on non-ASCII characters in code.

Code is ASCII by rule: the characters a programmer types on a physical
keyboard, nothing an editor or a generator smuggled in. The dash that starts
this trouble is invisible in review and identical to a hyphen at a glance.

Unicode that a test genuinely needs is written as an escape (\\u00e9,
\\U0001F600). That keeps the source ASCII and makes the byte width of the
sequence follow from the code point instead of being guessed from a glyph.

Two exemptions, both narrow:
  * a byte order mark in a .sln, which Visual Studio writes itself;
  * a line carrying the marker below, for a name owned by someone else - a
    path, a URL, a host name - that an escape would break rather than encode.
"""

import sys
from pathlib import Path

from harness.commands import ROOT, stdout_of, verdict

EXEMPTION_MARKER = "non-ascii:"
PROSE_SUFFIXES = {".md"}
BOM = "\ufeff"


def main() -> int:
    listing = stdout_of("git", "ls-files")
    if not listing:
        return verdict("charset", False, "git ls-files returned nothing")

    offences = []
    for rel in listing.split("\n"):
        if not rel or Path(rel).suffix in PROSE_SUFFIXES:
            continue
        offences.extend(_scan(Path(rel)))

    for rel, number, chars in offences:
        points = " ".join(f"U+{ord(ch):04X}" for ch in chars)
        print(f"  {rel}:{number}  {points}", flush=True)
    return verdict("charset", not offences,
                   "" if not offences else f"{len(offences)} line(s) with non-ASCII")


def _scan(path: Path) -> list[tuple[str, int, str]]:
    try:
        text = (ROOT / path).read_text(encoding="utf-8")
    except (OSError, UnicodeDecodeError):
        # Not decodable as text, so not source: images, archives, fonts.
        return []
    if path.suffix == ".sln":
        text = text.lstrip(BOM)

    found = []
    for number, line in enumerate(text.splitlines(), start=1):
        if EXEMPTION_MARKER in line:
            continue
        chars = sorted({ch for ch in line if ord(ch) > 127})
        if chars:
            found.append((str(path), number, "".join(chars)))
    return found


if __name__ == "__main__":
    sys.exit(main())
