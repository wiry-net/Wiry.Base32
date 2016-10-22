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

prepare_linux () {
    echo Linux should be prepared
}

prepare_darwin () {
    echo Darwin dependencies installation
    echo Updating Homebrew...
    brew update
    echo Install openssl...
    brew install openssl
    brew link --force openssl
}

skip_dependencies_installation=1

while [ "$1" != "" ]; do
    if [ $1 = "--install-dependencies" ]
    then
        skip_dependencies_installation=0
    fi
    shift
done

if [ $skip_dnx_install = "0" ]
then
    echo Starting dependencies installation...
    if [ $sys_name = "Linux" ]
    then
        prepare_linux
    elif [ $sys_name = "Darwin" ]
    then
        prepare_darwin
    else
        exit 1
    fi
    install_dnx
else
    echo Skipping dependencies installation
fi

dotnet restore

dotnet build --configuration Release --framework netstandard1.6 src/Wiry.Base32

dotnet test --configuration Release tests/UnitTests