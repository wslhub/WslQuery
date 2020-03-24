#include <array>
#include <wtypes.h>
#include <comutil.h>

#include <rapidjson/document.h>
#include <rapidjson/filewritestream.h>
#include <rapidjson/writer.h>
#include <rapidjson/prettywriter.h>

#include "LxssSession.h"

using rapidjson::Document;
using rapidjson::Value;
using rapidjson::kArrayType;

#define ADD_BOOL_MEMBER(target, doc, key, value) \
	target.AddMember( \
		Value().SetString((key), doc.GetAllocator()).Move(), \
		Value().SetBool((value)).Move(), \
		doc.GetAllocator())
#define ADD_STRING_MEMBER(target, doc, key, value) \
	target.AddMember( \
		Value().SetString((key), doc.GetAllocator()).Move(), \
		Value().SetString((value), doc.GetAllocator()).Move(), \
		doc.GetAllocator())
#define ADD_UINT_MEMBER(target, doc, key, value) \
	target.AddMember( \
		Value().SetString((key), doc.GetAllocator()).Move(), \
		Value().SetUint((value)).Move(), \
		doc.GetAllocator())
#define ADD_VALUE_MEMBER(target, doc, key, value) \
	target.AddMember( \
		Value().SetString((key), doc.GetAllocator()).Move(), \
		(value).Move(), \
		doc.GetAllocator())

int main()
{
	HRESULT hr = S_OK;
	ILxssUserSession* wslSession = NULL;

	Document doc;
	doc.SetObject();

	ULONG DistroCount = 0;
	PLXSS_ENUMERATE_INFO DistroInfo = NULL, tDistroInfo = NULL;
	BOOL failFound = FALSE;
	Value distroList(kArrayType);

	int exitCode = 0;
	char writeBuffer[1024];
	rapidjson::FileWriteStream os(stdout, writeBuffer, sizeof(writeBuffer));

	hr = CoInitializeEx(NULL, COINIT_MULTITHREADED);

	if (FAILED(hr)) {
		ADD_BOOL_MEMBER(doc, doc, "succeed", false);
		ADD_STRING_MEMBER(doc, doc, "error", "CoInitializeEx failed.");
		ADD_UINT_MEMBER(doc, doc, "hresult", hr);
		exitCode = 1;
		goto endOfApp;
	}

	hr = CoInitializeSecurity(NULL, -1, NULL, NULL, RPC_C_AUTHN_LEVEL_DEFAULT, SecurityDelegation, NULL, EOAC_STATIC_CLOAKING, NULL);

	if (FAILED(hr)) {
		ADD_BOOL_MEMBER(doc, doc, "succeed", false);
		ADD_STRING_MEMBER(doc, doc, "error", "CoInitializeSecurity failed.");
		ADD_UINT_MEMBER(doc, doc, "hresult", hr);
		exitCode = 1;
		goto endOfApp;
	}

	hr = CoCreateInstance(CLSID_LxssUserSession, NULL, CLSCTX_LOCAL_SERVER, IID_ILxssUserSession, (PVOID*)& wslSession);

	if (FAILED(hr)) {
		ADD_BOOL_MEMBER(doc, doc, "succeed", false);
		ADD_STRING_MEMBER(doc, doc, "error", "IID_ILxssUserSession::CoCreateInstance failed.");
		ADD_UINT_MEMBER(doc, doc, "hresult", hr);
		exitCode = 1;
		goto endOfApp;
	}

	hr = wslSession->lpVtbl->EnumerateDistributions(wslSession, &DistroCount, &DistroInfo);

	if (DistroCount) {
		tDistroInfo = DistroInfo;
		for (ULONG i = 0u; i < DistroCount; i++) {
			Value distro(rapidjson::kObjectType);
			WSL_DISTRIBUTION_FLAGS Flags = WSL_DISTRIBUTION_FLAGS::WSL_DISTRIBUTION_FLAGS_NONE;
			PWSTR BasePath = NULL, DistributionName = NULL;
			PSTR KernelCommandLine = NULL, * DefaultEnvironment = NULL;
			ULONG Version = 0, DefaultUid = 0, EnvironmentCount = 0;
			GUID DistroId = { 0 };

			hr = wslSession->lpVtbl->GetDistributionConfiguration(
				wslSession, &DistroInfo->DistributionID, &DistributionName, &Version, &BasePath,
				&KernelCommandLine, &DefaultUid, &EnvironmentCount, &DefaultEnvironment, (DWORD *)&Flags);

			if (FAILED(hr)) {
				ADD_BOOL_MEMBER(distro, doc, "succeed", false);
				ADD_STRING_MEMBER(distro, doc, "error", "ILxssUserSession::GetDistribution failed.");
				ADD_UINT_MEMBER(distro, doc, "hresult", hr);
				distroList.PushBack(distro, doc.GetAllocator());
				failFound = TRUE;
				continue;
			}

			std::string guidString;
			LPOLESTR guidStringPtr = NULL;
			hr = StringFromCLSID(DistroInfo->DistributionID, &guidStringPtr);
			if (FAILED(hr) || !guidStringPtr) {
				ADD_BOOL_MEMBER(distro, doc, "succeed", false);
				ADD_STRING_MEMBER(distro, doc, "error", "StringFromCLSID failed.");
				ADD_UINT_MEMBER(distro, doc, "hresult", hr);
				distroList.PushBack(distro, doc.GetAllocator());
				failFound = TRUE;
				continue;
			}

			guidString = _bstr_t(guidStringPtr);
			CoTaskMemFree(guidStringPtr);

			ADD_BOOL_MEMBER(distro, doc, "succeed", true);
			ADD_STRING_MEMBER(distro, doc, "basePath", std::string(_bstr_t(BasePath)).data());
			ADD_STRING_MEMBER(distro, doc, "distroName", std::string(_bstr_t(DistributionName)).data());
			ADD_STRING_MEMBER(distro, doc, "kernelCommandLine", std::string(_bstr_t(KernelCommandLine)).data());

			Value ae(kArrayType);
			for (ULONG uleCount = 0u; uleCount < EnvironmentCount; uleCount++) {
				ae.PushBack(
					Value().SetString(DefaultEnvironment[uleCount], doc.GetAllocator()).Move(),
					doc.GetAllocator());
			}
			ADD_VALUE_MEMBER(distro, doc, "defaultEnvironmentVariables", ae);
			ADD_UINT_MEMBER(distro, doc, "defaultUid", DefaultUid);

			ADD_BOOL_MEMBER(distro, doc, "enableInterop",
				(int)Flags& (int)WSL_DISTRIBUTION_FLAGS::WSL_DISTRIBUTION_FLAGS_ENABLE_INTEROP);
			ADD_BOOL_MEMBER(distro, doc, "enableDriveMounting",
				(int)Flags& (int)WSL_DISTRIBUTION_FLAGS::WSL_DISTRIBUTION_FLAGS_ENABLE_DRIVE_MOUNTING);
			ADD_BOOL_MEMBER(distro, doc, "appendNtPath",
				(int)Flags& (int)WSL_DISTRIBUTION_FLAGS::WSL_DISTRIBUTION_FLAGS_APPEND_NT_PATH);
			ADD_BOOL_MEMBER(distro, doc, "hasDefaultFlag",
				(int)Flags& (int)WSL_DISTRIBUTION_FLAGS::WSL_DISTRIBUTION_FLAGS_DEFAULT);
			ADD_UINT_MEMBER(distro, doc, "distroFlags", (int)Flags);

			ADD_STRING_MEMBER(distro, doc, "distroId", guidString.data());
			ADD_BOOL_MEMBER(distro, doc, "isDefaultDistro", DistroInfo->Default);

			std::string state;
			switch (DistroInfo->State) {
			case LXSS_DISTRIBUTION_STATES::Stopped:
				state = "Stopped";
				break;
			case LXSS_DISTRIBUTION_STATES::Running:
				state = "Running";
				break;
			case LXSS_DISTRIBUTION_STATES::Installing:
				state = "Installing";
				break;
			case LXSS_DISTRIBUTION_STATES::Uninstalling:
				state = "Uninstalling";
				break;
			case LXSS_DISTRIBUTION_STATES::Converting:
				state = "Converting";
				break;
			default:
				state = "Unknown";
				break;
			}
			ADD_STRING_MEMBER(distro, doc, "distroStatus", state.data());
			ADD_UINT_MEMBER(distro, doc, "wslVersion", DistroInfo->Version);
			
			distroList.PushBack(distro, doc.GetAllocator());
			DistroInfo = (PLXSS_ENUMERATE_INFO)((PBYTE)DistroInfo + sizeof(*DistroInfo));
		}
	}

	if (failFound) {
		ADD_BOOL_MEMBER(doc, doc, "succeed", failFound);
		ADD_STRING_MEMBER(doc, doc, "error", "One or more items cannot be processed.");
		ADD_UINT_MEMBER(doc, doc, "hresult", S_FALSE);
		exitCode = 1;
		goto endOfApp;
	}

	ADD_BOOL_MEMBER(doc, doc, "succeed", true);
	ADD_VALUE_MEMBER(doc, doc, "distros", distroList);

endOfApp:
	if (tDistroInfo) {
		CoTaskMemFree(tDistroInfo);
	}

	if (__argc > 1 && strcmp(__argv[1], "--pretty") == 0) {
		rapidjson::PrettyWriter<rapidjson::FileWriteStream> writer(os);
		doc.Accept(writer);
	}
	else {
		rapidjson::Writer<rapidjson::FileWriteStream> writer(os);
		doc.Accept(writer);
	}

	exit(exitCode);
}
