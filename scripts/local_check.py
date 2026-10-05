#!/usr/bin/env python3
"""Run the sequence CI runs.

The step list must stay identical to ci.yml - the moment it diverges, a green
local run stops meaning the build will pass.
"""

import sys
from pathlib import Path

from harness.commands import run, verdict

STEPS = (("charset",), ("format",), ("build",), ("test",), ("analyze",))
SCRIPTS = Path(__file__).resolve().parent


def main() -> int:
    for step, *args in STEPS:
        print(f"\n=== {step} ===", flush=True)
        code = run(sys.executable, SCRIPTS / f"{step}.py", *args)
        if code != 0:
            return verdict("local-check", False, f"{step} failed")
    return verdict("local-check", True)


if __name__ == "__main__":
    sys.exit(main())
