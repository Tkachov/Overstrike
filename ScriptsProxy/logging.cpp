// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

#include "logging.h"

#include <stdio.h>
#include <stdarg.h>
#include <sstream>

#include <termcolor/termcolor.hpp>

namespace logging {
	void log(const LogLevel level, const char* fmt, ...) {
		switch (level) {
			case LogLevel::INFO:
				std::cout << termcolor::white << "[INFO ]: ";
				break;
			case LogLevel::WARN:
				std::cout << termcolor::yellow << "[WARN ]: ";
				break;
			case LogLevel::DEBUG:
				std::cout << termcolor::bright_grey << "[DEBUG]: ";
				break;
			case LogLevel::FATAL:
				std::cout << termcolor::red << "[FATAL]: ";
				break;
			default:
				break;
		}

		va_list vargs;
		va_start(vargs, fmt);
		vprintf(fmt, vargs);

		std::cout << termcolor::reset << std::endl;
	}
}
