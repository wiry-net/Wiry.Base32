#!/usr/bin/env bash

set -e
exit_handler() {
    if [[ "$1" != "0" ]]
    then
        echo "Error: $1 (command: '$2')"
        exit 1
    fi
    echo "Build succeeded"
}
trap 'exit_handler $? "$BASH_COMMAND"' EXIT

sys_name=`uname`
echo System: $sys_name

dotnet restore

dotnet build --configuration Release --framework netstandard1.1 src/Wiry.Base32

dotnet test --configuration Release tests/UnitTests