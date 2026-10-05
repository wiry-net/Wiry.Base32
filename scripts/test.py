#!/usr/bin/env python3
"""Run both test suites: the solution's and the harness's own."""

import sys

from harness.commands import ROOT, SOLUTION, dotnet, run, verdict


def main() -> int:
    # MTP mode names its target explicitly, and its option set is its own: a
    # bare path is no longer read as the solution, and --nologo is gone, which
    # the runner reports as exit code 5 (bad arguments) rather than ignoring.
    code = dotnet("test", "--solution", SOLUTION, "-c", "Release")
    if code != 0:
        return verdict("test", False, f"dotnet test exited {code}")

    # The harness gates the solution, so it is tested by the same step - a
    # parser that only runs in CI would be a gate nobody checks.
    # Pattern spelled out: the default `test*.py` would import this very script.
    code = run(sys.executable, "-m", "unittest", "discover",
               "-s", "scripts", "-t", "scripts", "-p", "test_*.py", cwd=ROOT)
    return verdict("test", code == 0, "" if code == 0 else f"harness tests exited {code}")


if __name__ == "__main__":
    sys.exit(main())
