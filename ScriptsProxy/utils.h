// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

#pragma once

#include <string>

namespace utils {
	void create_console();
	void destroy_console();
	void kill_process();
	std::string get_executable_directory();
	std::string GetGameExecutable();
}
