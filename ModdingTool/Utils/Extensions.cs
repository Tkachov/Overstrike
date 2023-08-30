// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;

namespace ModdingTool.Utils {
	public static class Extensions {
		public static void Set<K, V>(this Dictionary<K, V> d, K k, V v) {
			if (d.ContainsKey(k))
				d[k] = v;
			else
				d.Add(k, v);
		}

		public static void Update<K, V>(this Dictionary<K, V> d, K k, V v, Func<V, V, V> update) {
			if (d.ContainsKey(k))
				d[k] = update(d[k], v);
			else
				d.Add(k, v);
		}
	}
}
