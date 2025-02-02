// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Sections.Generic;
using System.Diagnostics;
using System.IO;

namespace DAT1.Sections.TOC {
	public class TextureMetaSection: ByteBufferSection {
		public const uint TAG = 0xC9FB9DDA; // Archive TOC Texture Meta

		// `textureIndex` is according to TextureAssetIdsSection.Ids

		private const int META_SIZE = 72;

		public byte[] GetTextureMeta(int textureIndex) {
			Debug.Assert(textureIndex >= 0 && (textureIndex+1)*META_SIZE <= Buffer.Length);

			return Read(textureIndex * META_SIZE, META_SIZE);
		}

		public void SetTextureMeta(int textureIndex, byte[] meta) {
			Debug.Assert(textureIndex >= 0 && (textureIndex + 1) * META_SIZE <= Buffer.Length);
			Debug.Assert(meta != null && meta.Length == META_SIZE);
			
			Write(textureIndex * META_SIZE, meta);
		}

		public void AddTextureMeta(byte[] meta) {
			Debug.Assert(meta != null && meta.Length == META_SIZE);

			var nextMetaOffset = Buffer.Length;
			Extend(META_SIZE);
			Write(nextMetaOffset, meta);
		}

		public void InsertTextureMeta(int textureIndex, byte[] meta) {
			Debug.Assert(textureIndex >= 0 && (textureIndex + 1) * META_SIZE <= Buffer.Length);
			Debug.Assert(meta != null && meta.Length == META_SIZE);

			var offset = textureIndex * META_SIZE;
			var newBuffer = new byte[Buffer.Length + meta.Length];
			using var ms = new MemoryStream(newBuffer);
			ms.Write(Buffer, 0, offset);
			ms.Write(meta);
			ms.Write(Buffer, offset, Buffer.Length - offset);
		}
	}
}
