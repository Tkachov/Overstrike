// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.IO;
using DAT1;
using OverstrikeShared.Utils;

namespace OverstrikeShared.STG {
	public class STG {
		private const uint MAGIC = 0x475453; // STG

		public enum Flags {
			INSTALL_HEADER = 1 << 0,
			INSTALL_TEXUTRE_META = 1 << 1,
		}

		private uint _version;
		private uint _flags;

		private byte[] _header;
		private byte[] _textureMeta;
		private byte[] _raw;
		private DAT1.DAT1 _dat1;

		public AssetHeaderHelper? Header {
			get {
				return (_header == null ? null : new AssetHeaderHelper(_header));
			}

			set {
				if (value == null) {
					_header = null;
					return;
				}
				
				_header = value.Save();
			}
		}

		public byte[] RawHeader => _header;
		public byte[] TextureMeta => _textureMeta;
		public byte[] Raw => _raw;
		public DAT1.DAT1 Dat1 => _dat1;

		public STG() {
			Clear();
		}

		public void Load(string filename, bool allowRawData = true) {
			Clear();

			using var f = File.OpenRead(filename);
			using var r = new BinaryReader(f);

			byte[] ReadAllBytes(BinaryReader r) {
				using var ms = new MemoryStream();
				r.BaseStream.CopyTo(ms);
				return ms.ToArray();
			}

			if (r.BaseStream.Length >= 4) {
				var magicAndVersion = r.ReadUInt32();
				var magic = magicAndVersion & 0xFFFFFF;
				var version = (magicAndVersion >> 24) & 0xFF;

				if (magic == 0x475453) {
					if (version != 0) {
						throw new Exception("Unknown STG version");
					}

					_version = version;
					_flags = r.ReadUInt32();
					var headerSize = r.ReadUInt32();
					var textureMetaSize = r.ReadUInt32();

					_header = r.ReadBytes((int)headerSize);
					BinaryStreams.Align16(r);

					_textureMeta = r.ReadBytes((int)textureMetaSize);
					BinaryStreams.Align16(r);

					_raw = ReadAllBytes(r);
					TryReadingDat1();
					return;
				}
			}

			if (!allowRawData) {
				throw new Exception("Not STG");
			}

			_version = 0;
			_flags = 0;

			_header = null;
			_textureMeta = null;
				
			r.BaseStream.Position = 0;
			_raw = ReadAllBytes(r);
			TryReadingDat1();
		}

		public void Load(TOCBase toc, byte span, ulong assetId) {
			Clear();

			_raw = toc.GetAssetBytes(span, assetId);
			if (toc is TOC_I29 toc_i29) {
				var index = toc.FindAssetIndex(span, assetId);
				_header = toc_i29.GetHeaderByAssetIndex(index);
				_textureMeta = toc_i29.GetTextureMetaByAssetIndex(index);
			}

			_version = 0;
			_flags = 0;
			if (_header != null) _flags |= (uint)Flags.INSTALL_HEADER;
			if (_textureMeta != null) _flags |= (uint)Flags.INSTALL_TEXUTRE_META;

			TryReadingDat1();
		}

		private void TryReadingDat1() {
			try {
				using var ms = new MemoryStream(_raw);
				using var r = new BinaryReader(ms);
				_dat1 = ReadDat1(r);
			} catch {
				_dat1 = null;
			}
		}

		protected virtual DAT1.DAT1 ReadDat1(BinaryReader br) {
			return new DAT1.DAT1(br);
		}
		
		public virtual byte[] Save(bool packRawIfNoExtras = true) {
			var hasExtras = (_header != null || _textureMeta != null);

			using var ms = new MemoryStream();
			using var w = new BinaryWriter(ms);

			if (hasExtras || !packRawIfNoExtras) {
				uint magicAndVersion = MAGIC | (_version << 24);
				w.Write(magicAndVersion);
				w.Write(_flags);

				var len = (uint)(_header == null ? 0 : _header.Length);
				w.Write(len);

				len = (uint)(_textureMeta == null ? 0 : _textureMeta.Length);
				w.Write(len);

				if (_header != null) {
					w.Write(_header);
					BinaryStreams.Align16(w);
				}

				if (_textureMeta != null) {
					w.Write(_textureMeta);
					BinaryStreams.Align16(w);
				}
			}

			if (_dat1 != null) {
				_raw = _dat1.Save();
			}

			w.Write(_raw);

			return ms.ToArray();
		}

		public bool HasFlag(Flags flag) {
			return (_flags & (uint)flag) != 0;
		}

		public void SetFlag(Flags flag, bool on) {
			if (on) {
				_flags |= (uint)flag;
			} else {
				uint mask = 0;
				mask |= (uint)flag;
				_flags &= ~mask;
			}
		}

		private void Clear() {
			_version = 0;
			_flags = 0;

			_header = null;
			_textureMeta = null;
			_raw = new byte[0];
			_dat1 = null;
		}

		protected void SetDat1(DAT1.DAT1 dat1) {
			_dat1 = dat1;
		}

		protected void SetRaw(byte[] raw) {
			_raw = raw;
		}

		public void ClearDat1() { // prevent additional .Save(), so original _raw gets saved and not repacked one
			_dat1 = null;
		}
	}

	public class AssetHeaderHelper {
		public class Pair {
			public uint A, B;
		}

		public uint Magic;
		public byte Unknown;
		public List<Pair> Pairs;
		public byte[] Extra;

		public AssetHeaderHelper() {
			Magic = 0;
			Unknown = 0;
			Pairs = new();
			Extra = new byte[0];
		}

		public AssetHeaderHelper(byte[] header) {
			using var br = new BinaryReader(new MemoryStream(header));

			Magic = br.ReadUInt32();
			Unknown = br.ReadByte();
			
			var pairsCount = br.ReadByte();
			var extraSize = br.ReadUInt16();

			Pairs = new();
			for (int i = 0; i < pairsCount; ++i) {
				var a = br.ReadUInt32();
				var b = br.ReadUInt32();
				Pairs.Add(new Pair { A = a, B = b });
			}

			Extra = br.ReadBytes(extraSize);
		}

		public byte[] Save() {
			using var ms = new MemoryStream();
			using var bw = new BinaryWriter(ms);

			bw.Write(Magic);
			bw.Write(Unknown);

			byte pairsCount = (byte)Pairs.Count;
			bw.Write(pairsCount);

			ushort extraSize = (ushort)Extra.Length;
			bw.Write(extraSize);

			foreach (var pair in Pairs) {
				bw.Write(pair.A);
				bw.Write(pair.B);
			}

			bw.Write(Extra);

			return ms.ToArray();
		}
	}
}
