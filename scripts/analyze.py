#!/usr/bin/env python3
"""Analyze the solution with ReSharper and gate on its SARIF report.

Not named inspect.py: this directory is sys.path[0], and the name would shadow
the stdlib `inspect` that dataclasses imports.
"""

import sys
from collections import Counter
from pathlib import Path

from harness.commands import ARTIFACTS, ROOT, SOLUTION, dotnet, verdict
from harness.sarif import Finding, ReportError, load

REPORT = ARTIFACTS / "inspect.sarif"


def main() -> int:
    # jb needs built assemblies but must not build them itself: its own build is
    # a second, slower path, while --no-build on an unbuilt tree cannot resolve
    # references and reports the fallout as errors.
    code = dotnet("build", SOLUTION, "-c", "Release", "--nologo")
    if code != 0:
        return verdict("analyze", False, f"dotnet build exited {code}")

    ARTIFACTS.mkdir(exist_ok=True)
    # Machine-global ReSharper settings are disabled so the verdict depends on
    # the repository alone; `dotnet jb` (not `jb`) honours the pinned version.
    code = dotnet("jb", "inspectcode", SOLUTION,
                  "-f=Sarif", "-e=INFO", "--no-build", "--no-updates",
                  # Without this jb evaluates the Debug configuration and would
                  # analyze assemblies other than the ones just built.
                  "--properties:Configuration=Release",
                  "-dsl=GlobalAll;GlobalPerProduct", f"-o={REPORT}")
    if code != 0:
        return verdict("analyze", False, f"jb inspectcode exited {code}")

    try:
        findings = load(REPORT)
    except ReportError as exc:
        return verdict("analyze", False, str(exc))

    _report(findings)
    blocking = sum(1 for finding in findings if finding.blocking)
    return verdict("analyze", blocking == 0,
                   "" if blocking == 0 else f"{blocking} blocking finding(s)")


def _report(findings: list[Finding]) -> None:
    for finding in sorted(findings, key=lambda f: (not f.blocking, f.path, f.line)):
        print(f"  {finding.level or '<no level>':8} {finding.rule:45} "
              f"{_relative(finding.path)}:{finding.line}  {finding.message}")
    counts = Counter(finding.level or "<no level>" for finding in findings)
    print(f"  {len(findings)} finding(s): "
          f"{', '.join(f'{level} {count}' for level, count in sorted(counts.items())) or 'none'}")


def _relative(path: str) -> str:
    try:
        return str(Path(path).relative_to(ROOT))
    except ValueError:
        return path


if __name__ == "__main__":
    sys.exit(main())
