// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using System.IO;
using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Overstrike.Data;
using Overstrike.Utils;
using System.Text;
using OverstrikeShared.STG;
using OverstrikeShared.STG.Files;

namespace Overstrike.Installers {
	internal class MSM2SuitStyleInstaller: InstallerBase_I29 {
		public MSM2SuitStyleInstaller(TOC_I29 toc, string gamePath): base(toc, gamePath) {}

		private string _suitId;
		private string _id;
		private string _icon;
		private Dictionary<string, string> _overrides = new();

		class AssetDescription {
			public byte Span;
			public ulong Id;

			public uint Size;
			public uint Offset;
			public byte HeaderSize;
			public byte TextureMetaSize;

			public byte[] Header = null;
			public byte[] TexutreMeta = null;
		}

		public override void Install(ModEntry mod, int index) {
			_mod = mod;

			var fullPath = NestedFiles.GetAbsoluteModPath(_mod.Path);
			using var file = NestedFiles.GetNestedFile(fullPath);

			List<AssetDescription> assets;
			ReadData(file, out assets);

			var suitsPath = Path.Combine(_gamePath, "asset_archive", "Suits");

			var modsPath = Path.Combine(_gamePath, "d", "mods");
			var modPath = Path.Combine(modsPath, "mod" + index);

			WriteAssetsFile(file, assets, modPath);
			WriteProgressionConfig(modsPath, modPath);

			_toc.SortAssets();
		}

		private void ReadData(Stream file, out List<AssetDescription> assets) {
			var prefix = new byte[16];
			var header = new byte[16];

			file.Seek(0, SeekOrigin.Begin);
			file.Read(prefix, 0, 16);

			file.Seek(-16, SeekOrigin.End);
			file.Read(header, 0, 16);

			for (int i = 0; i < 16; ++i) {
				header[i] ^= prefix[i];
			}

			using var hr = new BinaryReader(new MemoryStream(header));
			var magic = hr.ReadUInt32();
			var payloadSize = hr.ReadUInt32();
			var version = hr.ReadByte();
			var game = hr.ReadByte();

			if (magic != 0x4C595453) {
				throw new Exception("Invalid magic value!");
			}

			if (version != 1) {
				throw new Exception("Unexpected version value!");
			}

			hr.ReadByte();
			hr.ReadByte();
			var overridesCount = hr.ReadUInt32();

			// actual data

			file.Seek(-16 - payloadSize, SeekOrigin.End);
			var dr = new DataReader(file);

			_suitId = dr.ReadString();
			_id = dr.ReadString();
			var name = dr.ReadString();
			var author = dr.ReadString();
			_icon = dr.ReadString();
			dr.Align();
			
			_overrides.Clear();
			for (int i = 0; i < overridesCount; ++i) {
				var slot = dr.ReadString();
				var path = dr.ReadString();
				_overrides[slot] = path;
			}
			dr.Align();

			//

			var spansCount = dr.ReadByte();
			var spanIndexes = new byte[spansCount];
			var spanSizes = new uint[spansCount];
			uint assetsCount = 0;
			for (int i = 0; i < spansCount; ++i) {
				spanIndexes[i] = dr.ReadByte();
				spanSizes[i] = dr.ReadUint32();
				assetsCount += spanSizes[i];
			}

			var assetSpans = new byte[assetsCount];
			var assetIndex = 0;
			for (int i = 0; i < spansCount; ++i) {
				for (int j = 0; j < spanSizes[i]; ++j) {
					assetSpans[assetIndex] = spanIndexes[i];
					++assetIndex;
				}
			}

			dr.Align();

			//

			assets = new();
			for (int i = 0; i < assetsCount; ++i) {
				var assetId = dr.ReadUint64();
				var offset = dr.ReadUint32();
				var size = dr.ReadUint32();

				assets.Add(new AssetDescription() {
					Span = assetSpans[i],
					Id = assetId,
					Offset = offset,
					Size = size,
				});
			}

			for (int i = 0; i < assetsCount; ++i) {
				assets[i].HeaderSize = dr.ReadByte();
				assets[i].TextureMetaSize = dr.ReadByte();
			}

			dr.Align();

			foreach (var asset in assets) {
				if (asset.HeaderSize > 0) {
					asset.Header = dr.ReadBytes(asset.HeaderSize);
				}
			}


			foreach (var asset in assets) {
				if (asset.TextureMetaSize > 0) {
					asset.TexutreMeta = dr.ReadBytes(asset.TextureMetaSize);
				}
			}

			dr.Align();
		}

		class DataReader {
			private BinaryReader _reader;
			private long _position;

			public DataReader(Stream file) {
				_reader = new BinaryReader(file);
				_position = 0;
			}

			public void Align(int n = 16) {
				long rem = _position % n;
				if (rem == 0) return;

				var toRead = (int)(n - rem);
				_reader.ReadBytes(toRead);
				_position += toRead;
			}

			public string ReadString() {
				var len = _reader.ReadByte();
				var raw = _reader.ReadBytes(len);
				_position += len + 1;
				return Encoding.UTF8.GetString(raw, 0, raw.Length);
			}

			public byte ReadByte() {
				_position += 1;
				return _reader.ReadByte();
			}

			public uint ReadUint32() {
				_position += 4;
				return _reader.ReadUInt32();
			}

			public ulong ReadUint64() {
				_position += 8;
				return _reader.ReadUInt64();
			}

			public byte[] ReadBytes(int n) {
				_position += n;
				return _reader.ReadBytes(n);
			}
		}

		private void WriteAssetsFile(Stream file, List<AssetDescription> assets, string modPath) {
			var archivePath = Path.GetRelativePath(_gamePath, modPath);
			var archiveIndex = GetArchiveIndex(archivePath);
			
			using (var f = new FileStream(modPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
				using var w = new BinaryWriter(f);
				file.Position = 0;
				file.CopyTo(w.BaseStream);
			}

			foreach (var asset in assets) {
				OverwriteAsset(
					asset.Span, asset.Id,
					archiveIndex, asset.Offset, asset.Size,
					asset.HeaderSize > 0 ? asset.Header : null,
					asset.TextureMetaSize > 0 ? asset.TexutreMeta : null
				);
			}
		}

		private void WriteProgressionConfig(string suitsPath, string modPath) {
			var suitName = $"SUIT_{_suitId}";
			
			//

			const ulong SYSTEM_PROGRESSION_CONFIG_AID = 0x9C9C72A303FCFA30; // configs/system/system_progression.config

			var config = new Config();
			config.Load(_toc, 0, SYSTEM_PROGRESSION_CONFIG_AID);

			var data = config.ContentSection.Data;
			var suits = (JArray)data["SuitList"]["Suits"];

			JObject suit = null;
			foreach (var s in suits) {
				if ((string)s["Name"] == suitName) {
					suit = (JObject)s;
					break;
				}
			}

			if (suit == null) {
				// TODO: failed to install: main suit is not installed
				return;
			}

			/*
			var suit = new JObject {
				["DisplayName"] = displayName,
				["EnableUnlockedHUDMessage"] = false,
				["Hidden"] = true,
				["Icon"] = new JObject {
					["AssetPath"] = _icon,
					["Autoload"] = false
				},
				["Item"] = rewardConfigPath,
				["Name"] = suitName,
				["VariantGroup"] = new JObject {
					["Icon"] = new JObject {
						["AssetPath"] = _icon,
						["Autoload"] = false
					},
					["Item"] = varGroupConfigPath,
					["Name"] = varGroupName,
					["Variants"] = new JArray()
				},
				["WhenValidAutoGive"] = true,
			};
			*/
			
			var configsDir = $"suits/{_suitId}/configs/";

			if (!suit.ContainsKey("VariantGroup")) {				
				var varGroupConfigPath = configsDir + $"inv_{_suitId}_variant_group.config";
				var varGroupName = $"suit_{_suitId}_var_group";

				var groupIcon = suit["Icon"]["AssetPath"];
				suit["VariantGroup"] = new JObject {
					["Icon"] = new JObject {
						["AssetPath"] = groupIcon,
						["Autoload"] = false
					},
					["Item"] = varGroupConfigPath,
					["Name"] = varGroupName,
					["Variants"] = new JArray()
				};
			}

			var variants = (JArray)suit["VariantGroup"]["Variants"];
			if (variants.Count >= 3) {
				// silently skip installing this one as game doesn't support more than 3 styles
				return;
			}

			var v = $"var{variants.Count + 1}";
			// var configsDir = $"suits/{_suitId}/configs/";
			var varGroupLoadoutConfigPath = configsDir + $"itemloadout_{_suitId}_variant_group.config";
			var variantItemConfigPath = configsDir + $"equip_{_suitId}_{v}.config";
			var varName = $"suit_{_suitId}_{v}";

			variants.Add(new JObject {
				["Icon"] = new JObject {
					["AssetPath"] = _icon,
					["Autoload"] = false,
				},
				["Item"] = variantItemConfigPath,
				["Name"] = varName,
			});

			/*
			// hopefully not required
			suit["VariantGroup"]["Variants"] = variants;
			suits[<index>] = suit;
			data["SuitList"]["Suits"] = suits;
			*/
			config.ContentSection.Data = data;

			config.AddReference(_icon);
			config.AddReference(variantItemConfigPath);

			var archivePath = Path.Combine(suitsPath, "suits");
			WriteArchive(archivePath, 0, SYSTEM_PROGRESSION_CONFIG_AID, config);

			WriteVarGroupLoadoutConfig(varGroupLoadoutConfigPath, variantItemConfigPath);
			WriteVariantItemConfig(modPath, variantItemConfigPath, varName);
		}

		private void WriteVarGroupLoadoutConfig(string assetPath, string variantItemConfigPath) {
			var assetId = CRC64.Hash(assetPath);

			var config = new Config();
			config.Load(_toc, 0, assetId);

			var data = config.ContentSection.Data;
			var items = (JArray)data["Loadout"]["ItemLoadoutLists"][0]["Items"];
			items.Add(new JObject {
				["AutoEquip"] = false,
				["Item"] = variantItemConfigPath,
			});

			config.ContentSection.Data = data;

			config.AddReference(variantItemConfigPath);

			var index = _toc.FindAssetIndex(0, assetId);
			var archiveIndex = _toc.GetArchiveIndexByAssetIndex(index);
			var archivePath = Path.Combine(_gamePath, _toc.GetArchiveFilename((uint)archiveIndex));
			WriteArchive(archivePath, 0, assetId, config);
		}

		private void WriteVariantItemConfig(string modPath, string assetPath, string varName) {
			var config = Config.Make("VanityVariantItemConfig");

			var overrides = new JArray();
			foreach (var pair in _overrides) {
				var slot = pair.Key;
				var path = pair.Value;
				overrides.Add(new JObject {
					["Material"] = new JObject {
						["AssetPath"] = path,
						["Autoload"] = false,
					},
					["MaterialMappingName"] = slot,
				});
			}

			config.ContentSection.Data = new JObject {
				["MaterialOverrides"] = overrides,
				["Name"] = varName,
			};

			// TODO: sometimes configs have "ValidCharacters": []
			// TODO: sometimes materials don't have "Autoload" key and are referenced in ref section instead

			WriteArchive(modPath + "_cfg", 0, CRC64.Hash(assetPath), config);
		}

		protected void WriteArchive(string archivePath, byte span, ulong assetId, STG stg) {
			stg.Save();
			File.WriteAllBytes(archivePath, stg.Raw);

			var archiveIndex = GetArchiveIndex(Path.GetRelativePath(_gamePath, archivePath));
			OverwriteAsset(
				span, assetId,
				archiveIndex, /*offset=*/0, (uint)stg.Raw.Length,
				stg.HasFlag(STG.Flags.INSTALL_HEADER) ? stg.RawHeader : null,
				stg.HasFlag(STG.Flags.INSTALL_TEXUTRE_META) ? stg.TextureMeta : null
			);
		}
	}
}
