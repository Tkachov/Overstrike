// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.IO;

namespace Overstrike.Utils {
	internal static class ErrorLogger {
		private const string LOG_FILENAME = "errors.log";

		private static StreamWriter _log = null;
		private static string _cachedContent = "";

		public static void StartSession() {
			_cachedContent = "";
			WriteSeparator();
		}

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
