// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Ionic.Crc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpCompress.Archives;
using SharpCompress.Compressors.Xz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Overstrike.Detectors {
	internal class ModsDetection {
		private List<DetectorBase> _detectors;
		private string _currentFile;

		public string CurrentFile { get => _currentFile; }

		public ModsDetection() {
			_detectors = new List<DetectorBase>() {
				new SMPCModDetector(),
				new SuitModDetector(),
				new StageModDetector(),
				new ArchiveDetector(this)
			};
		}

		public virtual void Detect(string path, List<ModEntry> mods) {
			mods.Clear();
			
			foreach (var detector in _detectors) {
				string[] extensions = detector.GetExtensions();
				foreach (var extension in extensions) {
					string[] files = Directory.GetFiles(path, "*." + extension, SearchOption.AllDirectories);
					foreach (var file in files) {
						var relativePath = GetRelativePath(file, path);
						_currentFile = relativePath;

						using var stream = File.OpenRead(file);
						Detect(detector, stream, relativePath, mods);
					}
				}
			}
		}

		internal virtual void Detect(DetectorBase detector, FileStream stream, string relativePath, List<ModEntry> mods) {
			detector.Detect(stream, relativePath, mods);
		}

		internal void Detect(IArchive archive, string path, List<ModEntry> mods) {
			Dictionary<string, DetectorBase> detectors = new Dictionary<string, DetectorBase>();
			foreach (var detector in _detectors) {
				string[] extensions = detector.GetExtensions();
				foreach (var extension in extensions) {
					detectors[extension] = detector;
				}
			}

			foreach (var entry in archive.Entries) {
				if (entry.IsDirectory) continue;

				foreach (var extension in detectors.Keys) {
					if (entry.Key.EndsWith("." + extension, StringComparison.OrdinalIgnoreCase)) {
						using var file = new MemoryStream();
						using (var entryStream = entry.OpenEntryStream()) {
							entryStream.CopyTo(file);
						}
						file.Seek(0, SeekOrigin.Begin);

						var internalPath = path + "||" + entry.Key;
						detectors[extension].Detect(file, internalPath, mods);
						break;
					}
				}
			}
		}

		protected string GetRelativePath(string file, string basepath) {
			Debug.Assert(file.StartsWith(basepath, StringComparison.OrdinalIgnoreCase));
			var result = file.Substring(basepath.Length);
			if (result.Length > 0) {
				if (result[0] == '/' || result[0] == '\\')
					result = result.Substring(1);
			}
			return result;
		}
	}

	internal class ModsDetectionCached: ModsDetection {
		private const string CURRENT_CACHE_FORMAT = "1";
		private Dictionary<string, CacheEntry> _cache = new();

		public ModsDetectionCached(): base() {}

		public override void Detect(string path, List<ModEntry> mods) {
			LoadCache();
			base.Detect(path, mods);
			SaveCache();
		}

		internal override void Detect(DetectorBase detector, FileStream stream, string relativePath, List<ModEntry> mods) {
			if (CacheHit(stream, relativePath, mods)) return;

			_cache[relativePath] = new CacheEntry {
				FileLength = stream.Length,
				FileChecksum1 = CacheEntry.CalculateChecksum1(stream),
				FileChecksum2 = CacheEntry.CalculateChecksum2(stream),
				ProducedEntries = new()
			};

			var modsBefore = mods.Count;
			detector.Detect(stream, relativePath, mods);

			var entries = _cache[relativePath].ProducedEntries;
			for (var i = modsBefore; i < mods.Count; ++i) {
				entries.Add(mods[i]);
			}
		}

		#region cache file

		private string GetCacheFilePath() {
			var cwd = Directory.GetCurrentDirectory();
			return Path.Combine(cwd, "Mods Library/Cache.json");
		}

		private bool CacheFormatsCompatible(string fileFormat, string codeFormat) {
			return (fileFormat == codeFormat);
		}

		private void LoadCache() {
			_cache.Clear();

			try {
				var json = JObject.Parse(File.ReadAllText(GetCacheFilePath()));
				var cacheFormat = (string?)json["format"];
				if (!CacheFormatsCompatible(cacheFormat, CURRENT_CACHE_FORMAT)) return;

				var cache = (JArray)json["cache"];
				if (cache == null) return;

				foreach (var entry in cache) {
					var entryArray = (JArray)entry;
					if (entryArray == null || entryArray.Count != 5) continue;

					var filename = (string?)entryArray[0];
					var fileLength = (long?)entryArray[1];
					var checksum1 = (uint?)entryArray[2];
					var checksum2 = (int?)entryArray[3];
					var produced = (JArray)entryArray[4];

					if (filename == null || fileLength == null || checksum1 == null || checksum2 == null) continue;

					var producedMods = new List<ModEntry>();
					foreach (var mod in produced) {
						var modArray = (JArray)mod;
						if (modArray == null || modArray.Count != 3) continue;

						var name = (string?)modArray[0];
						var path = (string?)modArray[1];
						var type = (string?)modArray[2];

						if (name == null || path == null || type == null) continue;

						try {
							var enumType = JsonConvert.DeserializeObject<ModEntry.ModType>("\"" + type + "\"");
							producedMods.Add(new ModEntry(name, path, enumType));
						} catch {}
					}

					_cache[filename] = new CacheEntry {
						FileLength = (long)fileLength,
						FileChecksum1 = (uint)checksum1,
						FileChecksum2 = (int)checksum2,
						ProducedEntries = producedMods
					};
				}
			} catch {}
		}

		private void SaveCache() {
			try {
				JArray entries = new();
				foreach (var filename in _cache.Keys) {
					var entry = _cache[filename];

					JArray mods = new();
					foreach (var mod in entry.ProducedEntries) {
						mods.Add(new JArray {
							mod.Name,
							mod.Path,
							mod.Type.ToString()
						});
					}

					entries.Add(new JArray {
						filename,
						entry.FileLength,
						entry.FileChecksum1,
						entry.FileChecksum2,
						mods
					});
				}

				JObject j = new();
				j["cache"] = entries;
				j["format"] = CURRENT_CACHE_FORMAT;
				File.WriteAllText(GetCacheFilePath(), j.ToString());
			} catch {}
		}

		#endregion

		private bool CacheHit(FileStream stream, string relativePath, List<ModEntry> mods) {
			if (!_cache.ContainsKey(relativePath)) {
				return false;
			}

			var entry = _cache[relativePath];

			var len = stream.Length;
			if (entry.FileLength != len) {
				return false;
			}

			var checksum1 = CacheEntry.CalculateChecksum1(stream);
			if (entry.FileChecksum1 != checksum1) {
				return false;
			}

			var checksum2 = CacheEntry.CalculateChecksum2(stream);
			if (entry.FileChecksum2 != checksum2) {
				return false;
			}

			foreach (var e in entry.ProducedEntries) {
				mods.Add(e);
			}
			return true;
		}

		private class CacheEntry {
			public long FileLength;
			public uint FileChecksum1;
			public int FileChecksum2;
			public List<ModEntry> ProducedEntries;

			private const int FIRST_PIECE_LENGTH = 4096;

			public static uint CalculateChecksum1(FileStream stream) {
				long len = stream.Length;
				if (len < FIRST_PIECE_LENGTH) {
					return (uint)len;
				}

				var oldPosition = stream.Position;
				byte[] buffer = new byte[FIRST_PIECE_LENGTH];
				stream.Position = 0;
				stream.Read(buffer);
				stream.Position = oldPosition;
				return Crc32.Compute(buffer);
			}

			public static int CalculateChecksum2(FileStream stream) {
				var oldPosition = stream.Position;
				stream.Position = 0;

				var c = new CRC32();
				var r = c.GetCrc32(stream);

				stream.Position = oldPosition;
				return r;
			}
		}
	}
}
