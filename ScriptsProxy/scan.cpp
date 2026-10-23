// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

#include "scan.h"
#include "logging.h"

#include <Windows.h>
#include <Psapi.h>
#include <stdio.h>
#include <string.h>
#include <map>

std::map<Scan::Pattern, Scan::ScanResult> ScanCache = {};

Scan::Pattern Scan::Parse(const char* pattern) {
	int pattern_len = (strlen(pattern) + 1) / 3;
	char* pattern_bytes = new char[pattern_len]();
	char* mask = new char[pattern_len]();

	for (int i = 0; i < strlen(pattern); i++) {
		if (pattern[i] == ' ') {
			continue;
		}
		else if (pattern[i] == '?') {
			mask[(i + 1) / 3] = '?';
			i += 2;
		}
		else if (pattern [i] == '*') {
			mask[(i + 1) / 3] = '*';
			i += 2;
		}
		else {
			char byte = (char)strtol(&pattern[i], 0, 16) & 0xFF;
			pattern_bytes[(i + 1) / 3] = byte;
			mask[(i + 1) / 3] = 'x';
			i += 2;
		}
	}

	return Pattern { pattern_bytes, mask, pattern_len };
}

Scan::ScanResult Scan::Internal::ScanModule(const char* moduleName, Scan::Pattern pattern) {
	if (ScanCache.contains(pattern)) {
		DEBUG("Cached pattern found.");
		return ScanCache[pattern];
	}

	HMODULE module = GetModuleHandleA(moduleName);
	MODULEINFO info {};
	if (!GetModuleInformation(GetCurrentProcess(), module, &info, sizeof(info))) {
		return Scan::ScanResult { false, NULL, {} };
	}

	if (pattern.len <= 0 || static_cast<unsigned int>(pattern.len) > info.SizeOfImage) {
		return Scan::ScanResult { false, NULL, {} };
	}

	uintptr_t base = (uintptr_t)module;

	char store[4] = {0};
	uintptr_t loc = 0;

	int anchor = -1;
	for (int j = 0; j < pattern.len; j++) {
		if (pattern.mask[j] != '?' && pattern.mask[j] != '*') {
			anchor = j;
			break;
		}
	}

	const char* image = (const char*)base;
	const size_t candidates = (size_t)info.SizeOfImage - (size_t)pattern.len + 1;

	for (size_t c = 0; c < candidates; ) {
		if (anchor >= 0) {
			const char* hit = (const char*)memchr(image + c + anchor, pattern.pattern[anchor], candidates - c);
			if (!hit) {
				break;
			}
			c = (size_t)(hit - image) - (size_t)anchor;
		}

		bool found = true;
		int store_used = 0;

		memset(store, 0, sizeof(store));

		for (int j = 0; j < pattern.len; j++) {
			if (pattern.mask[j] == '*') {
				if (store_used < sizeof(store)) {
					store[store_used++] = image[c + j];
				}
				continue;
			}

			if (pattern.mask[j] != '?' && pattern.pattern[j] != image[c + j]) {
				found = false;
				break;
			}
		}

		if (found) {
			loc = base + c;
			break;
		}

		c++;
	}

	Scan::ScanResult result = Scan::ScanResult { loc != NULL, loc, {} };
	memcpy(result.store, store, sizeof(store));

	ScanCache.insert(std::pair(pattern, result));

	return result;
}
