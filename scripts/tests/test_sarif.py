"""The SARIF reader is the only part of the harness with logic of its own."""

import json
import tempfile
import unittest
from pathlib import Path

from harness.sarif import ReportError, load, parse


def report(results, **run_overrides):
    run = {
        "results": results,
        "invocations": [{"executionSuccessful": True}],
        "originalUriBaseIds": {"solutionDir": {"uri": "file:///repo/"}},
    }
    run.update(run_overrides)
    return json.dumps({"version": "2.1.0", "runs": [run]})


def result(level, uri="src/A.cs", base="solutionDir", line=7):
    body = {"ruleId": "RuleX", "message": {"text": "something"},
            "locations": [{"physicalLocation": {
                "artifactLocation": {"uri": uri, "uriBaseId": base},
                "region": {"startLine": line}}}]}
    if level is not None:
        body["level"] = level
    return body


class ParseTests(unittest.TestCase):
    def test_Parse_MixedLevels_ClassifiesAndResolves(self):
        findings = parse(report([
            result("error"), result("warning"), result("note"), result("none"),
            result(None), result("shrug"),
            {"ruleId": "NoLocation", "message": {"text": "m"}},
        ]))

        self.assertEqual([f.level for f in findings],
                         ["error", "warning", "note", "none", "", "shrug", ""])
        # Only "none" passes. A note blocks, because a note left standing is a defect left
        # standing: every one this project has seen had a rewrite behind it.
        self.assertEqual([f.blocking for f in findings],
                         [True, True, True, False, True, True, True])
        self.assertEqual(findings[0].path, "/repo/src/A.cs")
        self.assertEqual((findings[0].line, findings[0].rule, findings[0].message),
                         (7, "RuleX", "something"))
        self.assertEqual((findings[-1].path, findings[-1].line), ("", 0))

    def test_Parse_UnknownUriBase_KeepsUriVerbatim(self):
        finding = parse(report([result("error", base="elsewhere")]))[0]
        self.assertEqual(finding.path, "src/A.cs")

    def test_Parse_AllRunsAggregate(self):
        two_runs = json.loads(report([result("error")]))
        two_runs["runs"].append(json.loads(report([result("warning")]))["runs"][0])
        self.assertEqual(len(parse(json.dumps(two_runs))), 2)

    def test_Parse_UntrustworthyReport_Raises(self):
        cases = {
            "empty": "",
            "blank": "   \n",
            "not json": "{oops",
            "not an object": "[]",
            "no version": json.dumps({"runs": []}),
            "wrong version": json.dumps({"version": "3.0", "runs": [{}]}),
            "no runs": json.dumps({"version": "2.1.0"}),
            "empty runs": json.dumps({"version": "2.1.0", "runs": []}),
            "run not an object": json.dumps({"version": "2.1.0", "runs": ["x"]}),
            "no invocation": report([], invocations=[]),
            "failed invocation": report([], invocations=[{"executionSuccessful": False}]),
            "missing success flag": report([], invocations=[{}]),
            "no results array": json.dumps({"version": "2.1.0", "runs": [
                {"invocations": [{"executionSuccessful": True}]}]}),
            "malformed result": report(["not an object"]),
        }
        for name, text in cases.items():
            with self.subTest(name), self.assertRaises(ReportError):
                parse(text)


class LoadTests(unittest.TestCase):
    def test_Load_ByteOrderMark_IsTolerated(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "r.sarif"
            path.write_text(report([result("error")]), encoding="utf-8-sig")
            self.assertEqual(len(load(path)), 1)

    def test_Load_MissingFile_Raises(self):
        with tempfile.TemporaryDirectory() as tmp:
            with self.assertRaises(ReportError):
                load(Path(tmp) / "absent.sarif")


if __name__ == "__main__":
    unittest.main()
