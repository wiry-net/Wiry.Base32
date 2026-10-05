"""Process plumbing and the verdict contract shared by every entry point."""

import subprocess
from pathlib import Path

# Paths are anchored to this file, never to the working directory: CI runners
# pick the working directory themselves.
ROOT = Path(__file__).resolve().parents[2]
SOLUTION = ROOT / "Wiry.Base32.sln"
ARTIFACTS = ROOT / "artifacts"


def run(*args: object, cwd: Path | None = None) -> int:
    """Echo a command, run it with its output streaming through, return its code."""
    argv = [str(a) for a in args]
    print("$", " ".join(argv), flush=True)
    return subprocess.run(argv, cwd=cwd or ROOT, check=False).returncode


def dotnet(*args: object, cwd: Path | None = None) -> int:
    return run("dotnet", *args, cwd=cwd)


def stdout_of(*args: object, cwd: Path | None = None) -> str:
    """Run a command for its stdout; empty string if it failed."""
    argv = [str(a) for a in args]
    finished = subprocess.run(argv, cwd=cwd or ROOT, check=False,
                              capture_output=True, text=True)
    return finished.stdout if finished.returncode == 0 else ""


def verdict(step: str, ok: bool, reason: str = "") -> int:
    """Print the closing line of a step and return the exit code to use."""
    # ASCII only: this line lands in other people's consoles, and a Windows
    # runner's code page mangles anything else.
    print(f"{'PASS' if ok else 'FAIL'}: {step}{f' - {reason}' if reason else ''}",
          flush=True)
    return 0 if ok else 1
