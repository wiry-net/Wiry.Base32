#!/usr/bin/env bash
# Binary compatibility guard: an app compiled against 1.1.1 runs first on 1.1.1, then on each
# candidate assembly that replaces 1.1.1 for its target framework.
#
#   tools/compat-guard/run.sh CANDIDATE.nupkg [net10.0|net472] [WORK_DIR]
#
# net472 runs the .NET Framework executable, so it needs Windows.
set -euo pipefail

here=$(cd "$(dirname "$0")" && pwd)
candidate=$(cd "$(dirname "$1")" && pwd)/$(basename "$1")
tfm=${2:-net10.0}
work=${3:-$(mktemp -d)}

if [ "$tfm" = net472 ]; then
    libs=(net45)
    run=("$work/app/guard.exe")
else
    libs=(netstandard1.1 netstandard2.0)
    run=(dotnet "$work/app/guard.dll")
fi

dotnet build "$here/guard.csproj" -c Release -f "$tfm" -o "$work/app" -nologo -v quiet
cp "$work/app/Wiry.Base32.dll" "$work/baseline.dll"
"${run[@]}"

rm -rf "$work/candidate"
unzip -q "$candidate" 'lib/*' -d "$work/candidate"
for lib in "${libs[@]}"; do
    echo "candidate lib/$lib:"
    cp "$work/candidate/lib/$lib/Wiry.Base32.dll" "$work/app/Wiry.Base32.dll"
    "${run[@]}"
done
cp "$work/baseline.dll" "$work/app/Wiry.Base32.dll"
