// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Overstrike.Data {
	public class SuitsCache {
		private JObject _cache;
		private bool _loaded = false;

		#region cache file

		private static string GetCacheFilePath() {
			var cwd = Directory.GetCurrentDirectory();
			return Path.Combine(cwd, "Mods Library/Suits Cache.json");
		}

		private void LoadCache() {
			_cache = new JObject();
			_loaded = true;

			try {
				_cache = JObject.Parse(File.ReadAllText(GetCacheFilePath()));
			} catch {}
		}

		private void SaveCache() {
			try {
				File.WriteAllText(GetCacheFilePath(), _cache.ToString());
			} catch {}
		}

		#endregion

		public bool HasConfig(string tocPath) {
			if (!_loaded) LoadCache();
			return _cache.ContainsKey(tocPath);
		}

		public JObject GetCachedData(string tocPath) {
			if (!_loaded) LoadCache();
			return (JObject)_cache[tocPath];
		}

		public JObject GetConfig(string tocPath) {
			var data = GetCachedData(tocPath);
			return (JObject)data["config"];
		}

		public long GetTimestamp(string tocPath) {
			var data = GetCachedData(tocPath);
			return (long)data["timestamp"];
		}

		public void SetConfig(string tocPath, JObject config) {
			if (!_loaded) LoadCache();
			_cache[tocPath] = new JObject() {
				["config"] = config,
				["timestamp"] = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds()
			};
			SaveCache();
		}

		public static string NormalizePath(string path) {
			path = path.Replace('\\', '/');

			// get rid of repeating slashes
			var result = "";
			bool slash = false;
			foreach (var c in path) {
				if (c == '/') {
					if (slash) continue;
					slash = true;
				} else {
					slash = false;
				}

				result += c;
			}

			// this is a directory path, so make it always end with slash
			if (!result.EndsWith('/'))
				result += "/";

			return result;
		}
	}
}
