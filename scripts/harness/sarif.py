"""Fail-closed reading of the SARIF report produced by `jb inspectcode`.

A report that cannot be understood is a failure, never an empty result set -
a broken analyzer must not read as clean code. `jb` exits 0 whether it found
nothing or found errors, so the verdict comes from here and nowhere else.
"""

import json
from dataclasses import dataclass
from pathlib import Path
from urllib.parse import unquote, urlparse

# Only "none" - the level the analyzer uses for results it does not rate at all. A note used to
# be advisory here, which meant the gate could stay green while ten of them stood; every one of
# those ten turned out to have a rewrite behind it, and two were pointing at real defects. The
# owner's rule is that a note is a hint the place can be fixed without suppressing it, so the gate
# has to be the thing that insists.
ADVISORY_LEVELS = frozenset({"none"})


class ReportError(Exception):
    """The report cannot be trusted; the gate must fail."""


@dataclass(frozen=True)
class Finding:
    level: str
    rule: str
    path: str
    line: int
    message: str

    @property
    def blocking(self) -> bool:
        # Missing or unrecognised levels block: SARIF leaves `level` optional,
        # and an unread level is not evidence of a harmless finding.
        return self.level not in ADVISORY_LEVELS


def load(path: Path) -> list[Finding]:
    if not path.is_file():
        raise ReportError(f"report not found: {path}")
    # utf-8-sig: a BOM would otherwise break json.loads on the first byte.
    return parse(path.read_text(encoding="utf-8-sig"))


def parse(text: str) -> list[Finding]:
    if not text.strip():
        raise ReportError("report is empty")
    try:
        report = json.loads(text)
    except ValueError as exc:
        raise ReportError(f"report is not valid JSON: {exc}") from exc
    if not isinstance(report, dict):
        raise ReportError("report is not a JSON object")

    version = report.get("version")
    if not isinstance(version, str) or not version.startswith("2.1"):
        raise ReportError(f"unsupported SARIF version: {version!r}")

    runs = report.get("runs")
    if not isinstance(runs, list) or not runs:
        raise ReportError("report carries no runs")

    findings: list[Finding] = []
    for index, run in enumerate(runs):
        if not isinstance(run, dict):
            raise ReportError(f"run {index} is not an object")
        _require_completion(run, index)
        results = run.get("results")
        if not isinstance(results, list):
            raise ReportError(f"run {index} carries no results array")
        bases = _base_paths(run)
        findings.extend(_finding(result, bases, index) for result in results)
    return findings


def _require_completion(run: dict, index: int) -> None:
    invocations = run.get("invocations")
    if not isinstance(invocations, list) or not invocations:
        raise ReportError(f"run {index} reports no invocation")
    for invocation in invocations:
        if not isinstance(invocation, dict) or invocation.get("executionSuccessful") is not True:
            raise ReportError(f"run {index} did not complete successfully")


def _base_paths(run: dict) -> dict[str, str]:
    raw = run.get("originalUriBaseIds")
    if not isinstance(raw, dict):
        return {}
    bases = {}
    for key, value in raw.items():
        uri = value.get("uri") if isinstance(value, dict) else None
        if isinstance(uri, str):
            bases[key] = unquote(urlparse(uri).path)
    return bases


def _finding(result: object, bases: dict[str, str], index: int) -> Finding:
    if not isinstance(result, dict):
        raise ReportError(f"run {index} carries a malformed result")
    path, line = _location(result, bases)
    message = result.get("message")
    return Finding(
        level=result.get("level") if isinstance(result.get("level"), str) else "",
        rule=result.get("ruleId") if isinstance(result.get("ruleId"), str) else "<unknown>",
        path=path,
        line=line,
        message=message.get("text", "") if isinstance(message, dict) else "",
    )


def _location(result: dict, bases: dict[str, str]) -> tuple[str, int]:
    locations = result.get("locations")
    if not isinstance(locations, list) or not locations:
        return "", 0
    physical = locations[0].get("physicalLocation") if isinstance(locations[0], dict) else None
    if not isinstance(physical, dict):
        return "", 0

    artifact = physical.get("artifactLocation")
    path = ""
    if isinstance(artifact, dict) and isinstance(artifact.get("uri"), str):
        # Paths are relative to a named base unless --absolute-paths was used.
        base = bases.get(artifact.get("uriBaseId"), "")
        # Joined as text, not with the local separator: this is where the report says
        # the finding is, and it has to read the same on every runner.
        path = f'{base.rstrip("/")}/{artifact["uri"]}' if base else artifact["uri"]

    region = physical.get("region")
    line = region.get("startLine") if isinstance(region, dict) else None
    return path, line if isinstance(line, int) else 0
