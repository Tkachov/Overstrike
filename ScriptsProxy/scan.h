// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

#pragma once

#include <Windows.h>

namespace Scan {
	struct Pattern {
		char* pattern;
		char* mask;
		int len;

		bool operator<(const Pattern& other) const {
			if (len != other.len) {
				return len < other.len;
			}

			for (int i = 0; i < len; i++) {
				if (pattern[i] != other.pattern[i]) {
					return pattern[i] < other.pattern[i];
				}
			}

			for (int i = 0; i < len; i++) {
				if (mask[i] != other.mask[i]) {
					return mask[i] < other.mask[i];
				}
			}

			return false;
		}
	};

	typedef struct Pattern Pattern;
	
	Pattern Parse(const char* pattern);

	typedef struct {
		bool found;
		uintptr_t loc;
		char* store;
	} ScanResult;

	namespace Internal {
		ScanResult ScanModule(const char* moduleName, Pattern pattern);
		inline ScanResult ScanModule(const char* moduleName, const char* pattern) {
			return ScanModule(moduleName, Parse(pattern));
		}
	}
}
