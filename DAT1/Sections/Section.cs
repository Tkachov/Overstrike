// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

namespace DAT1.Sections {
	public abstract class Section {
		abstract public void Load(byte[] bytes, DAT1 container);
		abstract public byte[] Save();
	}

	public class UnknownSection: Section {
		public byte[] Raw;

		public UnknownSection() {
			Raw = System.Array.Empty<byte>();
		}

		override public void Load(byte[] bytes, DAT1 container) { Raw = bytes; }
		override public byte[] Save() { return Raw; }
	}
}
