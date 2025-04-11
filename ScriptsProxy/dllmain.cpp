// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

#ifdef _DEBUG
#define LOGLEVEL 4
#endif

#include "scan.h"
#include "hooking.h"
#include "utils.h"
#include "logging.h"
#include "winmm.h"

#include <Windows.h> 
#include <cstring>
#include <vector>
#include <fstream>

#define SCAN(pt, r, n, p) \
	r (*n) p = nullptr; \
	void Init_ ## n() { \
		auto n ## _res = Scan::Internal::ScanModule(utils::GetGameExecutable().c_str(), pt); \
		if (!n ## _res.found) { FATAL("%s not found!", #n); return; } \
		n = (r (*) p)n ## _res.loc; \
		DEBUG(" - %s found at %p", #n, n); \
	}

#pragma region SM2_Patterns
// SM2 just had to be quirky so it has its own unique patterns :)
SCAN(
	"89 4C 24 ?? 55 56 41 56 48 83 EC 70",
	void, native_ParseArgs_SM2, (int argc, char** argv)
);
#pragma endregion

#pragma region Patterns
SCAN(
	"40 53 56 41 57 48 83 EC 50 48 8B F2",
	void, native_ParseArgs_Other, (int argc, char** argv)
);
#pragma endregion

typedef void (*ScriptEnableFn)();

typedef struct {
	HMODULE mod;
	std::string name;
} Script;

std::vector<Script> scripts = {};

HMODULE LoadScript(const char* param) {
	return LoadLibraryA(param);
}

void InjectSelectedScripts(const char* folder = "./scripts") {
	char pre[MAX_PATH];
	GetDllDirectoryA(sizeof(pre), pre);
	if (SetDllDirectoryA(folder) == 0) {
		FATAL("Failed to set script directory?");
		return;
	}

	HANDLE proc = GetCurrentProcess();
	INFO("Injecting scripts from %s", folder);
	
	std::ifstream selected_scripts("scripts.txt");
	if (!selected_scripts.good()) {
		MessageBoxA(NULL, "Attempting to inject scripts but no 'scripts.txt' found! Re-install mods through Overstrike!", "WARNING!", MB_OK);
		return;
	}
	
	for (std::string line; std::getline(selected_scripts, line); ) {
		INFO("Loading script %s", line.c_str());
		HMODULE mod = LoadScript(line.c_str());

		if (!mod) {
			MessageBoxA(NULL, "Error loading script! Re-install mods through Overstrike!", line.c_str(), MB_OK);
			continue;
		}

		Script s;
		s.mod = mod;
		s.name = line;
		scripts.push_back(s);
	}
	SetDllDirectoryA(pre);

	INFO("%d scripts loaded", scripts.size());
	INFO("Enabling scripts...");
	for (const auto& script : scripts) {
		ScriptEnableFn enable = (ScriptEnableFn)GetProcAddress(script.mod, "script_enable");
		if (enable) {
			enable();
		}
		else {
			WARN("%s does not export 'script_enable'", script.name.c_str());
		}
	}
}

// Pointers to be assigned depending on the running game
void* native_ParseArgs = nullptr;

MAKE_HOOK(void, ParseArgs, (int argc, char** argv), {
	ParseArgs_Call(argc, argv);
	
	bool inject_scripts = false;

	for (int i = 0; i < argc; i++) {
		DEBUG("CMD Arg: %s", argv[i]);
		if (strcmp(argv[i], "-console") == 0) {
			utils::create_console();
		}
		if (strcmp(argv[i], "-scripts") == 0) {
			inject_scripts = true;
		}
	}

	if (inject_scripts) {
		InjectSelectedScripts();
	}
});

bool InitOther() {
	Init_native_ParseArgs_Other();
	if (!native_ParseArgs_Other) return false;

	DEBUG("Running on SMR / MM / RCRA");
	native_ParseArgs = native_ParseArgs_Other;
	return true;
}

bool InitSM2() {
	Init_native_ParseArgs_SM2();
	if (!native_ParseArgs_SM2) return false;

	DEBUG("Running on SM2");
	native_ParseArgs = native_ParseArgs_SM2;
	return true;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
	switch (ul_reason_for_call) {
	case DLL_PROCESS_ATTACH:
		char path[MAX_PATH];
		GetWindowsDirectoryA(path, sizeof(path));

		strcat_s(path, "\\System32\\winmm.dll");
		winmm.dll = LoadLibraryA(path);
		setupFunctions();

#if LOGLEVEL > 0
		utils::create_console();
#endif

		if (InitSM2()) {
			INFO("Initialized for Marvel's Spider-Man 2");
		}
		else if (InitOther()) {
			INFO("Initialized for Marvel's Spider-Man Remastered / Marvel's Spider-Man: Miles Morales / Ratchet & Clank: Rift Apart");
		}
		else {
			WARN("Couldn't find functions for any known games!");
		}

		// Most likely not running in any supported game
		if (!native_ParseArgs) {
			return TRUE;
		}

		MH_Initialize();

		INSTALL_HOOK(ParseArgs, native_ParseArgs);
		break;
	case DLL_PROCESS_DETACH:
		FreeLibrary(winmm.dll);
		break;
	}
	return 1;
}
