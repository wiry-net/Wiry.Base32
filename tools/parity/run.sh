#!/usr/bin/env bash
# Parity gate for a candidate package: the released 1.1.1 (both assemblies) against every
# assembly inside the candidate nupkg, all through one generated case file.
#
#   tools/parity/run.sh CANDIDATE.nupkg [SCALE] [WORK_DIR]
#
# Needs dotnet (the probe targets net10.0), python3 3.9+, curl, unzip, sha256sum.
set -euo pipefail

here=$(cd "$(dirname "$0")" && pwd)
candidate=$(cd "$(dirname "$1")" && pwd)/$(basename "$1")
scale=${2:-20}
work=${3:-$(mktemp -d)}

baseline_url=https://api.nuget.org/v3-flatcontainer/wiry.base32/1.1.1/wiry.base32.1.1.1.nupkg
baseline_sha256=6ec1fe77e28aab7f8aca18efc5d568246ab813c7f32c8f5c3c2f60e37075a5f3

mkdir -p "$work"
curl -fsSL --retry 3 -o "$work/baseline.nupkg" "$baseline_url"
echo "$baseline_sha256  $work/baseline.nupkg" | sha256sum -c -

rm -rf "$work/baseline" "$work/candidate"
unzip -q "$work/baseline.nupkg" -d "$work/baseline"
unzip -q "$candidate" -d "$work/candidate"

dotnet build "$here/wiry_base32_probe/wiry_base32_probe.csproj" -c Release -o "$work/probe" -nologo -v quiet

builds=(--build "1.1.1-netstandard1.1=$work/baseline/lib/netstandard1.1/Wiry.Base32.dll"
        --build "1.1.1-net45=$work/baseline/lib/net45/Wiry.Base32.dll")
for dll in "$work"/candidate/lib/*/Wiry.Base32.dll; do
    tfm=$(basename "$(dirname "$dll")")
    builds+=(--build "candidate-$tfm=$dll")
done

python3 "$here/wiry_base32_check.py" --probe "$work/probe/wiry_base32_probe.dll" \
    --work "$work/check" --scale "$scale" "${builds[@]}"
