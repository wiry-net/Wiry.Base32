@echo off
cd %~dp0

if not "%APPVEYOR_REPO_COMMIT_MESSAGE%"=="%APPVEYOR_REPO_COMMIT_MESSAGE:release=%" (
    echo Make the release build
    ..\build.cmd
) else (
    echo Make the regular dev build
    ..\build.cmd --version-suffix %APPVEYOR_BUILD_VERSION%
)