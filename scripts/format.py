#!/usr/bin/env python3
"""Verify formatting without touching files."""

import sys

from harness.commands import SOLUTION, dotnet, verdict


def main() -> int:
    code = dotnet("format", SOLUTION, "--verify-no-changes")
    return verdict("format", code == 0, "" if code == 0 else f"dotnet format exited {code}")


if __name__ == "__main__":
    sys.exit(main())
