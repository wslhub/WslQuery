@echo off
pushd "%~dp0"

pushd WslQuery
dotnet publish -c Release -r win-x64
popd

:exit
popd
@echo on