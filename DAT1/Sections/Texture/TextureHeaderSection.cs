using System;
using System.IO;

namespace DAT1.Sections.Texture {
	public class TextureHeaderSection : Section
    {
        public const uint TAG = 0x4EDE3593;

        public uint sd_len, hd_len;
        public ushort hd_width, hd_height;
        public ushort sd_width, sd_height;
        public ushort array_size;
        public byte stex_format, planes;
        public ushort fmt;
        public UInt64 unk;
        public byte sd_mipmaps, unk2, hd_mipmaps, unk3;
        public byte[] unk4;

        public TextureHeaderSection(BinaryReader r, uint size)
        {
            sd_len = r.ReadUInt32();
            hd_len = r.ReadUInt32();
            hd_width = r.ReadUInt16();
            hd_height = r.ReadUInt16();
            sd_width = r.ReadUInt16();
            sd_height = r.ReadUInt16();
            array_size = r.ReadUInt16();
            stex_format = r.ReadByte();
            planes = r.ReadByte();
            fmt = r.ReadUInt16();
            unk = r.ReadUInt64();
            sd_mipmaps = r.ReadByte();
            unk2 = r.ReadByte();
            hd_mipmaps = r.ReadByte();
            unk3 = r.ReadByte();
            unk4 = r.ReadBytes((int)(size - 34));

            /*
self.sd_len, self.hd_len = struct.unpack("<II", data[:8])
    self.hd_width, self.hd_height = struct.unpack("<HH", data[8:12])
    self.sd_width, self.sd_height = struct.unpack("<HH", data[12:16])
    self.array_size, self.stex_format, self.planes = struct.unpack("<HBB", data[16:20])
    self.fmt, self.unk = struct.unpack("<HQ", data[20:30])
    self.sd_mipmaps, self.unk2, self.hd_mipmaps, self.unk3 = struct.unpack("<BBBB", data[30:34])
    self.unk4 = data[34:]
             */
        }

        override public byte[] Save()
            {
            return null; // TODO
            }
        }

}
