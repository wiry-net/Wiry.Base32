#!/usr/bin/env python3
"""Build the solution with the analyzer gate in force."""

import sys

from harness.commands import SOLUTION, dotnet, verdict


def main() -> int:
    code = dotnet("build", SOLUTION, "-c", "Release", "--nologo")
    return verdict("build", code == 0, "" if code == 0 else f"dotnet build exited {code}")


if __name__ == "__main__":
    sys.exit(main())
