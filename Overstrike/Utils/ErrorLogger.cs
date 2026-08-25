// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.IO;

namespace Overstrike.Utils {
	internal static class ErrorLogger {
		private const string LOG_FILENAME = "errors.log";

		private static StreamWriter _log = null;
		private static string _cachedContent = "";
		private static List<string> _warnings = new();

		public static void StartSession() {
			_cachedContent = "";
			_warnings.Clear();
			WriteSeparator();
		}

		// Warnings don't fail an installation, but callers can surface them to the
		// user instead of silently accepting a partial result.
		public static void WriteWarning(string warning) {
			try {
				_warnings.Add(warning);
				WriteInfo($"[!] {warning}\n");
			} catch {}
		}

		public static List<string> GetWarnings() => new(_warnings);

		public static void WriteInfo(string info) {
			try {
				if (_log == null) {
					_cachedContent += info;
				} else {
					_log.Write(info);
				}
			} catch {}
		}

		public static void WriteSeparator() {
			string separator = "";
			for (int i = 0; i < 60; i++) separator += '-';
			separator += '\n';

			WriteInfo(separator);
		}

		public static void WriteError(string error) {
			try {
				if (_log == null) {
					_log = File.AppendText(LOG_FILENAME);
					if (_cachedContent != "") {
						_log.Write(_cachedContent);
						_cachedContent = "";
					}
				}

				_log.Write(error);
				_log.Flush();
			} catch {}
		}

		public static void EndSession() {
			try {
				WriteSeparator();

				// A warning is actionable even when installation otherwise succeeds. Persist
				// the buffered session so the MessageBox is not the only record of it.
				if (_log == null && _warnings.Count > 0) {
					_log = File.AppendText(LOG_FILENAME);
					if (_cachedContent != "") {
						_log.Write(_cachedContent);
						_cachedContent = "";
					}
				}

				if (_log != null) {
					_log.Flush();
					_log.Dispose();
					_log.Close();

					_log = null;
				}
			} catch {}
		}
	}
}
