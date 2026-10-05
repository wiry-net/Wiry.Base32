#!/usr/bin/env python3
"""Analyze the solution with ReSharper and gate on its SARIF report.

A finding stops blocking only where the code says why it stays: the marker
below on the finding's line or in the // comment block right above it, with
the reason after it. Such a finding is still printed on every run, so the
signal is kept rather than silenced - the case the marker exists for is a
rewrite that would change behaviour or public surface.

Not named inspect.py: this directory is sys.path[0], and the name would shadow
the stdlib `inspect` that dataclasses imports.
"""

import re
import sys
from collections import Counter
from pathlib import Path

from harness.commands import ARTIFACTS, ROOT, SOLUTION, dotnet, verdict
from harness.sarif import Finding, ReportError, load

REPORT = ARTIFACTS / "inspect.sarif"
KEPT_MARKER = "analysis-kept:"


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

    kept = {finding for finding in findings if finding.blocking and _kept(finding)}
    _report(findings, kept)
    blocking = sum(1 for finding in findings if finding.blocking and finding not in kept)
    return verdict("analyze", blocking == 0,
                   "" if blocking == 0 else f"{blocking} blocking finding(s)")


def _kept(finding: Finding) -> bool:
    try:
        lines = _local(finding.path).read_text(encoding="utf-8").splitlines()
    except (OSError, UnicodeDecodeError):
        return False
    if not 0 < finding.line <= len(lines):
        return False
    index = finding.line - 1
    if KEPT_MARKER in lines[index]:
        return True
    # Plain comments only: a marker inside a /// block would ship in the XML documentation.
    while index > 0 and _plain_comment(lines[index - 1]):
        index -= 1
        if KEPT_MARKER in lines[index]:
            return True
    return False


def _plain_comment(line: str) -> bool:
    stripped = line.lstrip()
    return stripped.startswith("//") and not stripped.startswith("///")


def _report(findings: list[Finding], kept: set[Finding]) -> None:
    def label(finding: Finding) -> str:
        return "kept" if finding in kept else finding.level or "<no level>"

    for finding in sorted(findings, key=lambda f: (not f.blocking or f in kept, f.path, f.line)):
        print(f"  {label(finding):8} {finding.rule:45} "
              f"{_relative(finding.path)}:{finding.line}  {finding.message}")
    counts = Counter(label(finding) for finding in findings)
    print(f"  {len(findings)} finding(s): "
          f"{', '.join(f'{level} {count}' for level, count in sorted(counts.items())) or 'none'}")


def _local(path: str) -> Path:
    # Report paths come from file URIs, which put a slash before a Windows drive letter.
    return Path(path[1:] if re.match(r"/[A-Za-z]:/", path) else path)


def _relative(path: str) -> str:
    try:
        return _local(path).relative_to(ROOT).as_posix()
    except ValueError:
        return path


if __name__ == "__main__":
    sys.exit(main())
