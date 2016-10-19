@echo off
SetLocal EnableExtensions EnableDelayedExpansion
cd %~dp0
echo Current folder: %cd%

set projects=src\Wiry.Base32 tests\UnitTests
set temp_dirs=bin obj
set version_suffix=

:parse_args
if not "%1"=="" (
    if "%1"=="--version-suffix" (
        set version_suffix="%2"
        shift
    )
    shift
    goto parse_args
)

if exist "artifacts" (
    echo Cleanup artifacts...
    rmdir /S /Q "artifacts"
)

for %%p in (%projects%) do (
    for %%t in (%temp_dirs%) do (
        set dir=%%p\%%t
        if exist "!dir!" (
            echo Removing folder "!dir!"...
            rmdir /S /Q "!dir!"
        )
    )
)

dotnet restore

dotnet build --configuration Release "src\Wiry.Base32"

dotnet test --configuration Release "tests\UnitTests"

if "!version_suffix!"=="" (
    dotnet pack "src\Wiry.Base32" --configuration Release --no-build --output "artifacts"
) else (
    echo Packing with version suffix !version_suffix!...
    dotnet pack "src\Wiry.Base32" --configuration Release --no-build --output "artifacts" --version-suffix !version_suffix!
)