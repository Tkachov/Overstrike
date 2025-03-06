// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace DAT1.Sections.Generic {
	public class SerializedSection_I30: Section {
		public JObject Data;
		private DAT1? _dat1;

		public SerializedSection_I30() {
			Data = new JObject();
		}

		public override void Load(byte[] bytes, DAT1 container) {
			_dat1 = container;

			using var r = new BinaryReader(new MemoryStream(bytes));
			Data = ReadObject(r);
		}

		private long _globalOffset;

		public override byte[] Save() {
			using var s = new MemoryStream();
			using var w = new BinaryWriter(s);

			_globalOffset = 0;
			WriteObject(w, Data);

			return s.ToArray();
		}

		public void SetDat1(DAT1 dat1) {
			_dat1 = dat1;
		}

		#region serialization

		private enum NodeType {
			UINT8 = 0x00,
			UINT16 = 0x01,
			UINT32 = 0x02,
			INT8 = 0x04,
			INT16 = 0x05,
			INT32 = 0x06,
			FLOAT = 0x08,
			STRING = 0x0A,
			OBJECT = 0x0D,
			BOOLEAN = 0x0F,
			INSTANCE_ID = 0x11,
			NULL = 0x13,
		}

		private struct ChildHeader {
			public uint Hash;
			public ushort Flags;
			public byte Unknown;
			public byte NodeType;
		}

		#region read

		private JObject ReadObject(BinaryReader r) {
			var result = new JObject();

			var zero = r.ReadUInt32();
			var marker = r.ReadUInt32();
			var childrenCount = r.ReadUInt32();
			var dataLength = r.ReadUInt32();
			Utils.Assert(zero == 0);
			Utils.Assert(marker == 0x03150044);

			var start = r.BaseStream.Position;
			if (childrenCount > 0) {
				var childrenHeaders = new List<ChildHeader>();
				for (var i = 0; i < childrenCount; ++i) {
					var hash = r.ReadUInt32();
					var flags = r.ReadUInt16();
					var unknown = r.ReadByte();
					var nodeType = r.ReadByte();
					childrenHeaders.Add(new ChildHeader() { Hash = hash, Flags = flags, Unknown = unknown, NodeType = nodeType });
				}

				var childrenOffsets = new List<uint>();
				for (var i = 0; i < childrenCount; ++i) {
					childrenOffsets.Add(r.ReadUInt32());
				}

				Align(r, 4);

				for (var i = 0; i < childrenCount; ++i) {
					var name = _dat1.GetStringByOffset(childrenOffsets[i]);

					var itemsCount = childrenHeaders[i].Flags >> 4;
					var isArray = ((childrenHeaders[i].Flags & 1) == 1);
					var nodeType = (NodeType)childrenHeaders[i].NodeType;

					if (isArray) {
						result[name] = ReadArray(r, nodeType, itemsCount);
					} else {
						result[name] = ReadNode(r, nodeType);
					}
				}
			}

			var end = r.BaseStream.Position;
			var left = dataLength - (end - start);
			Utils.Assert(left >= 0);
			if (left > 0) {
				r.ReadBytes((int)left);
			}

			return result;
		}

		private JArray ReadArray(BinaryReader r, NodeType itemType, int itemsCount) {
			var list = new JArray();

			if (itemsCount == 0) {
				r.ReadByte();
			} else {
				for (var i = 0; i < itemsCount; ++i) {
					list.Add(ReadNode(r, itemType));
				}
			}

			return list;
		}

		private JToken ReadNode(BinaryReader r, NodeType itemType) {
			switch (itemType) {
				case NodeType.UINT8: return new JValue(r.ReadByte());
				case NodeType.UINT16: return new JValue(r.ReadUInt16());
				case NodeType.UINT32: return new JValue(r.ReadUInt32());
				case NodeType.INT8: return new JValue(r.ReadSByte());
				case NodeType.INT16: return new JValue(r.ReadInt16());
				case NodeType.INT32: return new JValue(r.ReadInt32());
				case NodeType.FLOAT: return new JValue(r.ReadSingle());
				case NodeType.STRING: return ReadString(r);
				case NodeType.OBJECT: return ReadObject(r);
				case NodeType.BOOLEAN: return new JValue(r.ReadBoolean());
				case NodeType.INSTANCE_ID: return new JValue(r.ReadUInt64());
				default: return null;
			}
		}

		private JValue ReadString(BinaryReader r) {
			var length = r.ReadUInt32();
			_ = r.ReadUInt32();
			_ = r.ReadUInt64();

			uint extra = 1;
			var rem = (length + extra) % 4;
			if (rem != 0) {
				extra += (4 - rem);
			}

			var bytes = r.ReadBytes((int)(length + extra));
			var value = Encoding.UTF8.GetString(bytes, 0, (int)length);
			return new JValue(value);
		}

		private void Align(BinaryReader r, int count) {
			var rem = (int)(r.BaseStream.Position % count);
			if (rem > 0) {
				r.ReadBytes(count - rem);
			}
		}

		#endregion

		#region write

		private void WriteObject(BinaryWriter w, JObject obj) {
			_globalOffset += 16;

			var ms = new MemoryStream();
			var w2 = new BinaryWriter(ms);

			var properties = obj.Properties();
			var stringOffsets = new List<uint>();
			var childrenTypes = new List<NodeType>();

			int i = 0;
			foreach (var prop in properties) {
				stringOffsets.Add(_dat1.AddString(prop.Name, true));
				childrenTypes.Add(DetermineValueType(prop.Value));

				var hash = CRC32.Hash(prop.Name, false);
				ushort flags = 1 << 4;
				if (prop.Value is JArray) {
					flags = (ushort)((uint)((JArray)prop.Value).Count << 4);
					flags |= 1;
				}

				w2.Write(hash);
				w2.Write(flags);
				w2.Write((byte)0);
				w2.Write((byte)childrenTypes[i]);
				++i;
			}

			foreach (var so in stringOffsets) {
				w2.Write(so);
			}

			_globalOffset += i * 12;
			if (i > 0) {
				PadGlobal(w2, 4);
			}

			i = 0;
			foreach (var prop in properties) {
				if (prop.Value is JArray array) {
					WriteArray(w2, array, childrenTypes[i]);
				} else {
					WriteNode(w2, prop.Value, childrenTypes[i]);
				}
				++i;
			}

			uint sizeOfEmptyChildrenData = 0;
			if (i > 0) {
				PadGlobal(w2, 4);
			} else {
				var before = _globalOffset;
				PadGlobal(w2, 4);
				var after = _globalOffset;
				sizeOfEmptyChildrenData = (uint)(after - before);
			}

			var childrenData = ms.ToArray();
			w.Write((uint)0);
			w.Write((uint)0x03150044);
			w.Write((uint)stringOffsets.Count);
			if (i > 0) {
				w.Write((uint)childrenData.Length);
			} else {
				w.Write((uint)sizeOfEmptyChildrenData);
			}
			w.Write(childrenData);
		}

		private void WriteArray(BinaryWriter w, JArray nodes, NodeType itemType) {
			if (nodes.Count == 0) {
				w.Write((byte)0);
				++_globalOffset;
				return;
			}

			foreach (var node in nodes) {
				WriteNode(w, node, itemType);
			}
		}

		private void WriteNode(BinaryWriter w, JToken node, NodeType itemType) {
			switch (itemType) {
				case NodeType.UINT8: w.Write((byte)node); _globalOffset += 1; break;
				case NodeType.UINT16: w.Write((ushort)node); _globalOffset += 2; break;
				case NodeType.UINT32: w.Write((uint)node); _globalOffset += 4; break;
				case NodeType.INT8: w.Write((sbyte)node); _globalOffset += 1; break;
				case NodeType.INT16: w.Write((short)node); _globalOffset += 2; break;
				case NodeType.INT32: w.Write((int)node); _globalOffset += 4; break;
				case NodeType.FLOAT: w.Write((float)node); _globalOffset += 4; break;
				case NodeType.STRING: WriteString(w, (string)node); break;
				case NodeType.OBJECT: WriteObject(w, (JObject)node); break;
				case NodeType.BOOLEAN: w.Write((bool)node); _globalOffset += 1; break;
				case NodeType.INSTANCE_ID: w.Write((ulong)node); _globalOffset += 8; break;
				default: w.Write((byte)0); _globalOffset += 1; break;
			}
		}

		private void WriteString(BinaryWriter w, string str) {
			var bytes = Encoding.UTF8.GetBytes(str);
			var byteLength = bytes.Length;
			var extra = 1;
			var rem = (byteLength + extra) % 4;
			if (rem != 0) {
				extra += (4 - rem);
			}

			var crc32 = CRC32.Hash(str, false);
			var crc64 = CRC64.Hash(str);
			if (str == "") { // TODO: should be fixed in the functions themselves?
				crc32 = 0;
				crc64 = 0;
			}

			w.Write((uint)byteLength);
			w.Write(crc32);
			w.Write(crc64);
			w.Write(bytes);
			for (var i = 0; i < extra; ++i) {
				w.Write((byte)0);
			}

			_globalOffset += 16 + byteLength + extra;
		}

		#region node type

		private NodeType DetermineValueType(JToken value) {
			if (value == null || value.Type == JTokenType.Null) return NodeType.NULL;

			switch (value.Type) {
				case JTokenType.Boolean: return NodeType.BOOLEAN;
				case JTokenType.Float: return NodeType.FLOAT;
				case JTokenType.String: return NodeType.STRING;
				case JTokenType.Object: return NodeType.OBJECT;
				case JTokenType.Array:
					var arr = (JArray)value;
					if (arr.Count == 0) return NodeType.NULL;

					var item = arr[0];
					if (item != null && item.Type == JTokenType.Integer) {
						return DetermineIntArrayType(arr);
					}

					return DetermineValueType(item);

				case JTokenType.Integer:
					var jv = (JValue)value;

					if (jv.Value is System.Numerics.BigInteger bi) {
						if (bi.Sign >= 0) return DetermineUnsignedIntNodeType(0, ulong.MaxValue);
						else return DetermineIntNodeType(long.MinValue, long.MaxValue);
					}

					if (jv.Value is ulong || jv.Value is uint) return DetermineUnsignedIntNodeType(0, (ulong)jv.Value);
					else return DetermineIntNodeType((long)jv.Value, (long)jv.Value);
			}

			return NodeType.NULL;
		}

		private NodeType DetermineIntArrayType(JArray arr) {
			bool firstSigned = true;
			bool hasSigned = false;
			long signed_min = 0, signed_max = 0;
			ulong unsigned_max = 0;

			foreach (JValue jv in arr) {
				if (jv.Value is System.Numerics.BigInteger bi) {
					if (bi.Sign >= 0) {
						var uv2 = (ulong)bi;
						if (unsigned_max < uv2)
							unsigned_max = uv2;
					} else {
						var sv = (long)bi;
						if (firstSigned) {
							firstSigned = false;
							signed_min = sv;
							signed_max = sv;
						} else {
							if (signed_max < sv)
								signed_max = sv;
							if (signed_min > sv)
								signed_min = sv;
						}
						hasSigned = true;
					}

					continue;
				}

				if (jv.Value is ulong uv) {
					if (unsigned_max < uv)
						unsigned_max = uv;
				} else {
					var sv = (long)jv.Value;
					if (firstSigned) {
						firstSigned = false;
						signed_min = sv;
						signed_max = sv;
					} else {
						if (signed_max < sv)
							signed_max = sv;
						if (signed_min > sv)
							signed_min = sv;
					}
					hasSigned = true;
				}
			}

			if (hasSigned) return DetermineIntNodeType(signed_min, Math.Max(signed_max, (long)unsigned_max));
			else return DetermineUnsignedIntNodeType(0, Math.Max((ulong)signed_max, unsigned_max));
		}

		private NodeType DetermineIntNodeType(long min, long max) {
			if (0 <= min) {
				return DetermineUnsignedIntNodeType(0, (ulong)max);
			}

			NodeType[] types = { NodeType.INT8, NodeType.INT16, NodeType.INT32, NodeType.INSTANCE_ID };
			long[] mins = { -128, -32768, -2147483648, -9223372036854775808 };
			long[] maxs = { 127, 32767, 2147483647, 9223372036854775807 };

			for (var i = 0; i < types.Length; ++i) {
				if (mins[i] <= min && max <= maxs[i]) {
					return types[i];
				}
			}

			return NodeType.INSTANCE_ID;
		}

		private NodeType DetermineUnsignedIntNodeType(ulong min, ulong max) {
			NodeType[] types = { NodeType.UINT8, NodeType.UINT16, NodeType.UINT32, NodeType.INSTANCE_ID };
			ulong[] maxs = { 255, 65535, 4294967295, 18446744073709551615 };

			for (var i = 0; i < types.Length; ++i) {
				if (max <= maxs[i]) {
					return types[i];
				}
			}

			return NodeType.INSTANCE_ID;
		}

		#endregion

		/*
		private void Pad(BinaryWriter w, int a) {
			var r = w.BaseStream.Position % a;
			if (r > 0) {
				for (var i = 0; i < a - r; ++i) {
					w.Write((byte)0);
				}
			}
		}
		*/

		private void PadGlobal(BinaryWriter w, int a) {
			var r = _globalOffset % a;
			if (r > 0) {
				for (var i = 0; i < a - r; ++i) {
					w.Write((byte)0);
				}
				_globalOffset += (a - r);
			}
		}

		#endregion

		#endregion
	}
}
