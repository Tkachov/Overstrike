// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace DAT1.Sections.Generic {
	public class SerializedSection : Section
	{
		enum NodeType
		{
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
			INSTANCE_ID = 0x11, // 8 byte
			NULL = 0x13, // 1 byte, always zero. maybe null?
		}


		public JObject Root;
		public List<JObject> Extras;
		private DAT1? Dat1;

		private long deserialization_started_from;

		public SerializedSection() {
			Root = new JObject();
			Extras = new List<JObject>();
		}

		override public void Load(byte[] bytes, DAT1 container) {
			Dat1 = container;

			using var r = new BinaryReader(new MemoryStream(bytes));
			var size = bytes.Length;

			Root = Deserialize(r);
			Extras = new List<JObject>();

			while (r.BaseStream.Position < size) {
				Extras.Add(Deserialize(r));
			}
		}

		private JObject Deserialize(BinaryReader r)
		{
			deserialization_started_from = r.BaseStream.Position;
			return DeserializeObject(r);
		}

		private struct ChildHeader
		{
			public uint hash;
			public ushort flags;
			public byte unk;
			public byte node_type;
		}

		private JObject DeserializeObject(BinaryReader r)
		{
			JObject result = new JObject();

			uint zero = r.ReadUInt32();
			uint unk = r.ReadUInt32();
			uint children_count = r.ReadUInt32();
			uint data_length = r.ReadUInt32();

			Utils.Assert(zero == 0);
			Utils.Assert(unk == 0x03150044);

			long start = r.BaseStream.Position;
			List<ChildHeader> children = new List<ChildHeader>();
			for (int i = 0; i < children_count; ++i)
			{
				uint hash = r.ReadUInt32();
				ushort flags = r.ReadUInt16();
				byte unk2 = r.ReadByte();
				byte node_type = r.ReadByte();
				children.Add(new ChildHeader() { hash = hash, flags = flags, unk = unk2, node_type = node_type });
			}

			List<uint> children_offsets = new List<uint>();
			for (int i = 0; i < children_count; ++i)
			{
				children_offsets.Add(r.ReadUInt32());
			}

			for (int i = 0; i < children_count; ++i)
			{
				string name = Dat1.GetStringByOffset(children_offsets[i]);


				int items_count = children[i].flags >> 4;
				bool is_array = items_count > 1;

				if (is_array)
					result[name] = DeserializeArray(r, (NodeType)children[i].node_type, items_count);
				else
					result[name] = DeserializeNode(r, (NodeType)children[i].node_type);
			}

			Align(r, 4);

			long end = r.BaseStream.Position;
			long left = data_length - (end - start);
			Utils.Assert(left >= 0);

			if (left > 0)
				r.ReadBytes((int)left);

			return result;
		}

		private JToken DeserializeNode(BinaryReader r, NodeType itemType)
		{
			switch (itemType)
			{
				case NodeType.UINT8: return new JValue(r.ReadByte());
				case NodeType.UINT16: return new JValue(r.ReadUInt16());
				case NodeType.UINT32: return new JValue(r.ReadUInt32());
				case NodeType.INT8: return new JValue(r.ReadSByte());
				case NodeType.INT16: return new JValue(r.ReadInt16());
				case NodeType.INT32: return new JValue(r.ReadInt32());
				case NodeType.FLOAT: return new JValue(r.ReadSingle());
				case NodeType.STRING: return DeserializeString(r);
				case NodeType.OBJECT: return DeserializeObject(r);
				case NodeType.BOOLEAN: return new JValue(r.ReadBoolean());
				case NodeType.INSTANCE_ID: return new JValue(r.ReadUInt64());
				case NodeType.NULL:
					r.ReadByte();
					return null;
			}

			return null;
		}

		private JArray DeserializeArray(BinaryReader r, NodeType itemType, int itemsCount)
		{
			JArray list = new JArray();
			for (int i = 0; i < itemsCount; ++i)
			{
				list.Add(DeserializeNode(r, itemType));
			}
			return list;
		}

		private JValue DeserializeString(BinaryReader r)
		{
			int length = r.ReadInt32();
			/*uint crc32 = */
			r.ReadUInt32();
			/*ulong crc64 = */
			r.ReadUInt64();
			string value = Encoding.ASCII.GetString(r.ReadBytes(length));
			/*byte nullbyte = */
			r.ReadByte();
			Align(r, 4);

			return new JValue(value);
		}

		private void Align(BinaryReader f, int a)
		{
			int r = (int)((f.BaseStream.Position - deserialization_started_from) % a);
			if (r != 0)
				f.ReadBytes(a - r);
		}

		override public byte[] Save()
		{
			var s = new MemoryStream();
			var w = new BinaryWriter(s);

			Serialize(w, Root);
			foreach (var e in Extras)
			{
				Serialize(w, e);
			}

			return s.ToArray();
		}

		private void Serialize(BinaryWriter w, JObject obj)
		{
			SerializeObject(w, obj);
		}

		private NodeType DetermineIntNodeType(long min, ulong max)
		{
			if (0 <= min)
			{
				NodeType[] types = { NodeType.UINT8, NodeType.UINT16, NodeType.UINT32, NodeType.INSTANCE_ID };
				ulong[] maxs = { 255, 65535, 2147483647, 18446744073709551615 };

				for (int i = 0; i < types.Length; ++i)
				{
					if (max <= maxs[i])
					{
						return types[i];
					}
				}
			}
			else
			{
				NodeType[] types = { NodeType.INT8, NodeType.INT16, NodeType.INT32, NodeType.INSTANCE_ID };
				long[] mins = { -128, -32768, -2147483648, -9223372036854775808 };
				ulong[] maxs = { 127, 32767, 2147483647, 9223372036854775807 };

				for (int i = 0; i < types.Length; ++i)
				{
					if (mins[i] <= min && max <= maxs[i])
					{
						return types[i];
					}
				}
			}

			return NodeType.INSTANCE_ID;
		}

		private NodeType DetermineIntNodeType2(long min, long max)
		{
			if (0 <= min)
			{
				NodeType[] types = { NodeType.UINT8, NodeType.UINT16, NodeType.UINT32, NodeType.INSTANCE_ID };
				ulong[] maxs = { 255, 65535, 2147483647, 18446744073709551615 };

				for (int i = 0; i < types.Length; ++i)
				{
					if ((ulong)max <= maxs[i])
					{
						return types[i];
					}
				}
			}
			else
			{
				NodeType[] types = { NodeType.INT8, NodeType.INT16, NodeType.INT32, NodeType.INSTANCE_ID };
				long[] mins = { -128, -32768, -2147483648, -9223372036854775808 };
				long[] maxs = { 127, 32767, 2147483647, 9223372036854775807 };

				for (int i = 0; i < types.Length; ++i)
				{
					if (mins[i] <= min && max <= maxs[i])
					{
						return types[i];
					}
				}
			}

			return NodeType.INSTANCE_ID;
		}

		private NodeType DetermineSignedIntNodeType(long min, long max)
		{

			NodeType[] types = { NodeType.INT8, NodeType.INT16, NodeType.INT32, NodeType.INSTANCE_ID };
			long[] mins = { -128, -32768, -2147483648, -9223372036854775808 };
			long[] maxs = { 127, 32767, 2147483647, 9223372036854775807 };

			for (int i = 0; i < types.Length; ++i)
			{
				if (mins[i] <= min && max <= maxs[i])
				{
					return types[i];
				}
			}

			return NodeType.INSTANCE_ID;
		}

		private NodeType DetermineUnsignedIntNodeType(ulong min, ulong max)
		{

			NodeType[] types = { NodeType.UINT8, NodeType.UINT16, NodeType.UINT32, NodeType.INSTANCE_ID };
			ulong[] maxs = { 255, 65535, 2147483647, 18446744073709551615 };

			for (int i = 0; i < types.Length; ++i)
			{
				if (max <= maxs[i])
				{
					return types[i];
				}
			}

			return NodeType.INSTANCE_ID;
		}

		private NodeType DetermineIntArrayType(JArray arr)
		{
			bool firstSigned = true;
			bool firstUnsigned = true;
			bool hasSigned = false;
			long signed_min = 0, signed_max = 0;
			ulong unsigned_max = 0;

			foreach (JValue jv in arr)
			{
				if (jv.Value is ulong)
				{
					ulong uv = (ulong)jv.Value;
					if (firstUnsigned)
					{
						firstUnsigned = false;
						unsigned_max = uv;
					}
					else
					{
						if (unsigned_max < uv)
							unsigned_max = uv;
					}
				}
				else
				{
					long sv = (long)jv.Value;
					if (firstSigned)
					{
						firstSigned = false;
						signed_min = sv;
						signed_max = sv;
					}
					else
					{
						if (signed_max < sv)
							signed_max = sv;
						if (signed_min > sv)
							signed_min = sv;
					}
					hasSigned = true;
				}
			}

			if (hasSigned) return DetermineIntNodeType2(signed_min, signed_max);
			else return DetermineUnsignedIntNodeType(0, unsigned_max);
		}

		private NodeType DetermineValueType(JToken value)
		{
			if (value == null || value.Type == JTokenType.Null) return NodeType.NULL;

			switch (value.Type)
			{
				case JTokenType.Boolean: return NodeType.BOOLEAN;
				case JTokenType.Float: return NodeType.FLOAT;
				case JTokenType.String: return NodeType.STRING;
				case JTokenType.Object: return NodeType.OBJECT;
				case JTokenType.Array:
					var arr = (JArray)value;
					Utils.Assert(arr.Count > 0);

					JToken item = arr[0];
					if (item != null && item.Type == JTokenType.Integer)
					{
						return DetermineIntArrayType(arr);

						long min = (long)item;
						ulong max = (ulong)item;
						foreach (var it in arr)
						{
							long sit = (long)it;
							ulong uit = (ulong)it;
							if (sit < min) min = sit;
							if (uit > max) max = uit;
						}
						return DetermineIntNodeType(min, max);
					}

					return DetermineValueType(item);

				case JTokenType.Integer:
					JValue jv = (JValue)value;
					/*
					if (jv.Value is UInt64 || jv.Value is UInt32 || jv.Value is UInt16 || jv.Value is byte || jv.Value is ulong || jv.Value is uint || jv.Value is ushort || jv.Value is byte)
						return DetermineUnsignedIntNodeType(0, (UInt64)jv.Value);
					else
						//return NodeType.INSTANCE_ID;
						//return DetermineSignedIntNodeType((long)jv.Value, (long)jv.Value);
						return DetermineIntNodeType((Int64)jv.Value, (UInt64)jv.Value);
					*/
					if (jv.Value is ulong) return DetermineUnsignedIntNodeType(0, (ulong)jv.Value);
					else return DetermineIntNodeType2((long)jv.Value, (long)jv.Value);

					//return DetermineIntNodeType((Int64)jv.Value, (UInt64)jv.Value);
			}

			return NodeType.NULL;
		}

		private void SerializeObject(BinaryWriter w, JObject obj)
		{
			var s = new MemoryStream();
			var w2 = new BinaryWriter(s);

			var properties = obj.Properties();
			List<uint> string_offsets = new List<uint>();
			List<NodeType> types = new List<NodeType>();

			int i = 0;
			foreach (var prop in properties)
			{
				string_offsets.Add(Dat1.AddString(prop.Name));
				types.Add(DetermineValueType(prop.Value));

				var hash = CRC32.Hash(prop.Name, false);
				ushort flags = 1 << 4;
				if (prop.Value is JArray)
				{
					flags = (ushort)((uint)((JArray)prop.Value).Count << 4);
				}

				w2.Write(hash);
				w2.Write(flags);
				w2.Write((byte)0);
				w2.Write((byte)types[i]);
				++i;
			}

			foreach (var so in string_offsets)
			{
				w2.Write(so);
			}

			i = 0;
			foreach (var prop in properties)
			{
				if (prop.Value is JArray)
				{
					SerializeArray(w2, (JArray)prop.Value, types[i]);
				}
				else
				{
					SerializeNode(w2, prop.Value, types[i]);
				}
				++i;
			}

			Pad(w2, 4);

			byte[] childrenData = s.ToArray();
			w.Write((uint)0);
			w.Write((uint)0x03150044);
			w.Write((uint)string_offsets.Count);
			w.Write((uint)childrenData.Length);
			w.Write(childrenData);
			//Pad(w, 4);
		}

		private void SerializeNode(BinaryWriter w, JToken node, NodeType itemType)
		{
			switch (itemType)
			{
				case NodeType.UINT8: w.Write((byte)node); break;
				case NodeType.UINT16: w.Write((ushort)node); break;
				case NodeType.UINT32: w.Write((uint)node); break;
				case NodeType.INT8: w.Write((sbyte)node); break;
				case NodeType.INT16: w.Write((short)node); break;
				case NodeType.INT32: w.Write((int)node); break;
				case NodeType.FLOAT: w.Write((float)node); break;
				case NodeType.STRING: SerializeString(w, (string)node); break;
				case NodeType.OBJECT: SerializeObject(w, (JObject)node); break;
				case NodeType.BOOLEAN: w.Write((bool)node); break; // (byte)((bool)node == true ? 1 : 0)); break;
				case NodeType.INSTANCE_ID: w.Write((ulong)node); break;
				case NodeType.NULL: w.Write((byte)0); break;
			}
		}

		private void SerializeArray(BinaryWriter w, JArray nodes, NodeType itemType)
		{
			foreach (JToken node in nodes)
			{
				SerializeNode(w, node, itemType);
			}
		}

		private void SerializeString(BinaryWriter w, string str)
		{
			var bts = Encoding.ASCII.GetBytes(str);
			w.Write((uint)bts.Length);
			w.Write(CRC32.Hash(str, false));
			w.Write(CRC64.Hash(str));
			w.Write(bts);
			w.Write((byte)0);
			Pad(w, 4);
		}

		private void Pad(BinaryWriter w, int a)
		{
			var r = w.BaseStream.Position % a;
			if (r > 0)
			{
				for (int i = 0; i < a - r; ++i)
					w.Write((byte)0);
			}
		}
	}
}
