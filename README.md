# WslQuery

![WslQuery Screenshot](Screenshot.png)

[Download latest version](https://github.com/rkttu/WslQuery/releases/latest/download/WslQuery.exe)

This program is a utility that reads the internal information of Windows Subsystem for Linux from the system and outputs the data to a standard output device in JSON format.

As of 2019, Microsoft has not released any publicly available LXSS related SDKs or interfaces. As a result, the project is based entirely on the code of Biswa96's WslReverse repository and is accordingly licensed under the GPL v3. If the full SDK code is released later, the license may change.

This program uses Tencent's rapidjson library to produce JSON output.

## Usage

The usage of the program is simple.

`WslQuery.exe [--pretty]`

* `--pretty`: This option prints JSON data in a human-readable form.

## Build Environment

This project built with below version of Visual C++ and Windows 10 SDK.

- Visual Studio 2019 16.3.0 Preview 1.0
- Windows SDK 10.0.16299.0
- NuGet 4.9.3.5777

## Credits

```
GitHub - Biswa96/WslReverse
GNU GPL v3 (https://github.com/Biswa96/WslReverse/blob/master/LICENSE)

Tencent rapidjson
MIT license (https://github.com/Tencent/rapidjson/blob/master/license.txt)

Gears Icon
icons8.com (https://icons8.com/icons/set/gears)
```
