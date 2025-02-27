// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

#pragma once

#include <MinHook.h>
#include "logging.h"

#define MAKE_HOOK(r, n, p, c) \
	typedef r (*n) p; \
	n n ## _Call = nullptr; \
	r n ## _Fn p c \
	hooking::Hook<n> n ## _Hook( &n ## _Fn, &n ## _Call ); \

#define INSTALL_AND_ENABLE(n, a, fm, sm) \
	if (!n ## _Hook.Install(reinterpret_cast<void**>(a)) || !n ## _Hook.Enable()) { \
		fm; \
	} \
	else { \
		sm; \
	}

#define INSTALL_HOOK(n, a) \
	INSTALL_AND_ENABLE( \
		n, \
		a, \
		FATAL("Failed to install/enable hook for " #n), \
		DEBUG("Installed hook for " #n) \
	); \

namespace hooking {
	template<typename T>
	class Hook {
		void* m_target = nullptr;
		void* m_detour = nullptr;
		T* m_original = nullptr;
	public:
		Hook() {}
		template<typename T>
		Hook(void* detour, T* original) {
			m_detour = detour;
			m_original = original;
		}
		bool Install(void* target) {
			m_target = target;
			MH_STATUS install = MH_CreateHook(target, m_detour, reinterpret_cast<void**>(m_original));
			return install == MH_OK;
		}
		bool Enable() {
			return MH_EnableHook(m_target) == MH_OK;
		}
		bool Disable() {
			return MH_DisableHook(m_target) == MH_OK;
		}
	};
}
