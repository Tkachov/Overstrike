// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

#pragma once

#if LOGLEVEL >= 4
	#define DEBUG(fmt, ...) logging::log(logging::LogLevel::DEBUG, fmt, __VA_ARGS__)
#else
	#define DEBUG(fmt, ...)
#endif

#if LOGLEVEL >= 3
	#define WARN(fmt, ...) logging::log(logging::LogLevel::WARN, fmt, __VA_ARGS__)
#else
	#define WARN(fmt, ...)
#endif

#if LOGLEVEL >= 2
	#define INFO(fmt, ...) logging::log(logging::LogLevel::INFO, fmt, __VA_ARGS__)
#else
	#define INFO(fmt, ...)
#endif

#if LOGLEVEL >= 1
	#define FATAL(fmt, ...) logging::log(logging::LogLevel::FATAL, fmt, __VA_ARGS__)
#else
	#define FATAL(fmt, ...)
#endif

namespace logging {
	enum LogLevel {
		FATAL,
		INFO,
		WARN,
		DEBUG
	};

	void log(const LogLevel level, const char* fmt, ...);
}
