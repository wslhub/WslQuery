# WslQuery

[English](README.md)

WslQuery는 현재 Windows 사용자가 등록한 WSL 배포판의 정보를 조회해 표준 출력으로 JSON을 내보냅니다. 2026년 9월 5일 소스 개정에서는 .NET 10을 대상으로 설정하고 Windows x64 Native AOT 빌드를 구성했습니다.

이번 개정에서는 한국어 문서, 기존 JSON 필드 호환성, 빌드 도구 교체, 보안 관련 코드 수정을 다룹니다. [공개 릴리스](https://github.com/wslhub/WslQuery/releases)는 현재 소스보다 이전 버전을 제공할 수 있습니다.

사용 방법과 출력 규약부터 살펴보겠습니다. 빌드와 검증 절차를 설명한 뒤 보안 변경 사항과 기여 정보를 정리합니다.

## JSON 조회 명령과 종료 코드

Windows 터미널에서 다음 명령을 실행합니다.

```console
WslQuery.exe [--pretty] [--help]
```

- `--pretty`: 들여쓰기를 적용한 JSON 출력
- `--help` 또는 `-h`: WSL 조회 없이 사용 방법 출력
- 종료 코드 `0`: 조회 성공, 빈 배포판 목록 또는 도움말 출력
- 종료 코드 `1`: 지원하지 않는 플랫폼, WSL 조회 실패 또는 일부 배포판 조회 실패
- 종료 코드 `2`: 알 수 없는 인자

프로그램은 옵션의 대소문자를 구분하지 않습니다. WSL API를 사용할 수 있으나 등록한 배포판이 없으면 빈 JSON 배열을 출력합니다. 오류 설명은 표준 오류로 내보냅니다. 일부 배포판 조회에 실패하면 JSON 결과를 유지하면서 종료 코드와 각 항목의 `hResult`, `succeed`로 실패를 알립니다. [명령 처리 코드](src/WslQuery/Program.cs)에서 세부 동작을 확인할 수 있습니다.

## 기존 JSON 필드와 수정한 값

출력은 Wslhub.Sdk 0.1.2의 속성 이름 16개를 유지합니다. `hResult`, `succeed`, `isDefault`, `isDefaultDistro`를 포함하며 플래그도 숫자로 출력합니다. [기존 SDK 모델](https://github.com/wslhub/wsl-sdk-dotnet/blob/78e6c9d/src/Wslhub.Sdk/DistroInfo.cs)을 기준으로 호환성을 검증합니다.

다만 기본 배포판 표시와 성공 여부의 계산 오류는 수정했습니다. 등록하지 않은 배포판을 성공으로 표시하지 않으며 `defaultUid`는 Windows API가 정의한 부호 없는 32비트 범위를 사용합니다. 선택 값인 `basePath`가 없으면 JSON에 `null`을 기록합니다. JSON 직렬화기 교체로 이스케이프 표기는 달라질 수 있지만 파싱한 문자열 값은 유지합니다.

## .NET 10과 Windows Native AOT 빌드

빌드에 사용하는 도구를 정리하겠습니다.

- 최신 서비스 업데이트를 적용한 .NET 10 SDK
- Windows x64 환경
- Desktop development with C++ 워크로드와 Windows SDK를 설치한 Visual Studio Build Tools
- 실제 배포판 조회를 위한 WSL

저장소 루트에서 다음 명령으로 실행 파일을 생성합니다.

```powershell
dotnet publish src/WslQuery/WslQuery.csproj -c Release -r win-x64 -p:PublishAot=true -o artifacts/win-x64
```

`src\publish.cmd`도 같은 빌드를 수행하고 종료 코드를 전달합니다. 생성한 실행 파일은 별도의 .NET 설치 없이 동작합니다. 배포 폴더의 라이선스 안내도 함께 전달합니다. macOS에서 Windows Native AOT 실행 파일을 직접 생성하는 방식은 지원하지 않습니다. Microsoft의 [Native AOT 문서](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)에서 플랫폼별 빌드 조건을 설명합니다.

## 회귀 테스트와 Windows 검증 범위

저장소 루트에서 빌드와 회귀 테스트를 실행합니다.

```console
dotnet build src/WslQuery.sln -c Release -warnaserror
dotnet run --project src/WslQuery.Tests -c Release --no-build
```

테스트는 JSON 필드, 한글과 특수문자, 인자 처리, 실패 시 종료 코드, 네이티브 문자열 처리를 검증합니다. Windows 레지스트리 테스트는 `HKCU\Software\WslQuery.Tests` 아래의 고유한 테스트 키를 사용한 뒤 삭제합니다. 실제 WSL 등록 정보는 수정하지 않습니다. 다른 운영체제에서는 Windows 전용 테스트를 건너뜁니다.

이어서 [GitHub Actions](.github/workflows/ci.yml)는 Linux와 Windows에서 코드를 빌드하고 Windows x64 Native AOT 실행 파일을 검증합니다. 실행기에 사용할 수 있는 WSL이 없으면 오류 처리 경로까지만 검사합니다. 실제 배포판을 등록한 WSL 1과 WSL 2의 구성 조회는 별도 통합 검증 범위로 남습니다.

## 의존성 제거와 네이티브 호출 제한

프로젝트는 Newtonsoft.Json, Wslhub.Sdk, 실험판 ILCompiler 참조와 실험용 NuGet 피드를 제거했습니다. .NET 10에 포함한 JSON 직렬화기와 공식 Native AOT 빌드 도구를 사용합니다. Newtonsoft.Json 제거로 [CVE-2024-21907](https://github.com/advisories/GHSA-5crp-9r3c-p9vr)의 원인이 된 패키지 의존성을 없앴습니다. 기존 앱이 외부 JSON 입력을 받아 원격 공격으로 이어지는 경로는 이번 검토에서 확인되지 않았습니다.

또한 네이티브 DLL 검색은 Windows System32로 제한하고 현재 사용자의 64비트 레지스트리를 읽기 전용으로 엽니다. [WSL API 문서](https://learn.microsoft.com/en-us/windows/win32/api/wslapi/nf-wslapi-wslgetdistributionconfiguration)에 따라 환경 변수 문자열과 배열의 메모리를 해제합니다. 프로그램은 배포판 내부 명령을 실행하거나 조회 결과를 네트워크로 전송하지 않습니다.

NuGet 취약성 감사와 CodeQL을 CI에 추가하고 Dependabot을 구성했습니다. Native AOT 실행 파일은 런타임 코드를 포함하므로 [.NET 10 서비스 업데이트](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)를 적용해 다시 빌드하고 배포하면 해당 런타임 수정을 반영할 수 있습니다.

## 기존 기여와 라이선스

한국어 문서는 [Xeppetto의 PR #5](https://github.com/wslhub/WslQuery/pull/5)를 반영했으며 원래 작성자 커밋을 보존했습니다. WSL 조회 모델과 API 호출은 [Wslhub.Sdk](https://github.com/wslhub/wsl-sdk-dotnet)를 바탕으로 구성했고 MIT 라이선스 고지를 [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt)에 포함했습니다.

프로젝트의 사용 조건은 [LICENSE](LICENSE)에 명시했습니다. 톱니바퀴 아이콘은 [Icons8](https://icons8.com/icons/set/gears)에서 제공합니다.

## 개정 소스의 적용 범위

여기까지 정리하면 WslQuery는 현재 사용자의 WSL 정보를 JSON으로 조회하는 도구로 동작합니다. 이번 소스 개정은 오래된 의존성과 조회 오류를 수정하며 공개 릴리스 갱신은 별도의 배포 절차로 진행합니다.

기존 스크립트는 JSON 필드와 종료 코드 변경을 기준으로 검증할 수 있습니다. 배포 담당자는 Windows Native AOT 빌드 결과와 실제 WSL 배포판 조회 결과를 함께 확인하면 적용 범위를 판단할 수 있습니다. 런타임 서비스 업데이트를 반영하는 작업은 이후 유지보수 과정에서도 이어집니다.
