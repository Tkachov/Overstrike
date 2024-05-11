// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Linq;

namespace DAT1.Files {
	public class LocalizationHelper {
		private class Entry {
			public string Value;
			public uint Unknown;
		}

		private Dictionary<string, Entry> _values = new();

		public LocalizationHelper(Localization l) {
			var n = l.EntriesCountSection.Value;

			for (var i = 0; i < n; ++i) {
				var keyOffset = l.KeysOffsetsSection.Values[i];
				var valueOffset = l.ValuesOffsetsSection.Values[i];
				var key = l.KeysDataSection.GetStringByOffset(keyOffset);
				var value = l.ValuesDataSection.GetStringByOffset(valueOffset);

				_values[key] = new Entry { Value = value, Unknown = l.UnknownSection.Values[i] };
			}
		}

		public LocalizationHelper() { }

		public List<string> Keys => _values.Keys.ToList();

		public string GetValue(string key) {
			if (!_values.ContainsKey(key)) return null;
			return _values[key].Value;
		}

		public uint GetUnknown(string key) {
			if (!_values.ContainsKey(key)) return 0;
			return _values[key].Unknown;
		}

		public void Remove(string key) {
			_values.Remove(key);
		}

		public void Add(string key, string value, uint unknown = 0) {
			if (_values.ContainsKey(key)) {
				_values[key].Value = value;
				_values[key].Unknown = unknown;
			} else {
				_values.Add(key, new Entry { Value = value, Unknown = unknown });
			}
		}

		public void Apply(Localization l) {
			var n = _values.Count;

			l.EntriesCountSection.Value = (uint)n;

			l.KeysOffsetsSection.Values.Clear();
			l.ValuesOffsetsSection.Values.Clear();
			l.KeyHashesSection.Values.Clear();

			l.KeysDataSection.Clear();
			l.ValuesDataSection.Clear();
			l.UnknownSection.Values.Clear();

			Dictionary<string, bool> alreadyAdded = new();

			void AddRecord(string key, string value, uint unknown = 0) {
				if (alreadyAdded.ContainsKey(key) && alreadyAdded[key]) {
					return;
				}

				alreadyAdded[key] = true;

				var keyOffset = l.KeysDataSection.Add(key);
				var valueOffset = l.ValuesDataSection.Add(value);

				l.KeysOffsetsSection.Values.Add(keyOffset);
				l.ValuesOffsetsSection.Values.Add(valueOffset);
				l.KeyHashesSection.Values.Add(CRC32.Hash(key, false));

				l.UnknownSection.Values.Add((byte)(unknown & 255));
			}

			const string FIRST_KEY = "INVALID";
			AddRecord(FIRST_KEY, "", GetUnknown(FIRST_KEY));

			var sortedKeys = _values.Keys.ToList();
			sortedKeys.Sort(string.CompareOrdinal);
			foreach (var key in sortedKeys) {
				var v = _values[key];
				AddRecord(key, v.Value, v.Unknown);
			}

			l.SortedKeyHashesSection.Values.Clear();
			l.SortedIndexesSection.Values.Clear();

			List<(uint, ushort)> hashesWithIndexes = new();
			for (ushort i = 0; i < n; ++i) {
				hashesWithIndexes.Add((l.KeyHashesSection.Values[i], i));
			}
			var compare = (uint a, uint b) => {
				if (a == b) return 0;
				return (a < b ? -1 : 1);
			};
			hashesWithIndexes.Sort((x, y) => compare(x.Item1, y.Item1));

			foreach (var pair in hashesWithIndexes) {
				l.SortedKeyHashesSection.Values.Add(pair.Item1);
				l.SortedIndexesSection.Values.Add(pair.Item2);
			}

			l.UnknownSection.Pad(n);
		}
	}
}