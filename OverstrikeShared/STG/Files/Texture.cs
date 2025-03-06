// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Files;
using DAT1.Sections.Texture;
using System.IO;

namespace OverstrikeShared.STG.Files {
	public class Texture: STG {
		private Texture_I30 _texture;

		#region sections

		public TextureHeaderSection_I30 HeaderSection => _texture.HeaderSection;

		#endregion

		#region STG

		protected override DAT1.DAT1 ReadDat1(BinaryReader br) {
			var header = Header;
			DAT1.Utils.Assert(header != null);
			DAT1.Utils.Assert(header.Magic == Texture_I30.MAGIC);

			_texture = new Texture_I30(br);
			return _texture;
		}

		public override byte[] Save(bool packRawIfNoExtras = true) {
			return base.Save(packRawIfNoExtras);
		}

		#endregion

		#region API

		public byte[] GetDDS() {
			return _texture.GetDDS();
		}

		#endregion
	}
}
