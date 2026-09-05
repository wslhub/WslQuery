@echo off
setlocal
pushd "%~dp0.."
dotnet publish src\WslQuery\WslQuery.csproj -c Release -r win-x64 -p:PublishAot=true -o artifacts\win-x64
set "publishResult=%errorlevel%"
popd
exit /b %publishResult%
