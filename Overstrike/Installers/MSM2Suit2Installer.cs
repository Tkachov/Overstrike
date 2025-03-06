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
	internal class MSM2Suit2Installer: InstallerBase_I29 {
		private string _language;

		public MSM2Suit2Installer(TOC_I29 toc, string gamePath, string language = ""): base(toc, gamePath) {
			_language = "";
		}

		private string _id;
		private string _displayName;
		private string _icon;

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

			var writeName = (_language != "");

			var modsPath = Path.Combine(_gamePath, "d", "mods");
			var modPath = Path.Combine(modsPath, "mod" + index);

			WriteAssetsFile(file, assets, modPath);
			WriteProgressionConfig(modsPath, writeName, _displayName);
			WriteVarGroupLoadoutConfig(modPath);

			/*
			if (writeName) {
				WriteLanguage(suitsPath, id, name);
			}
			*/

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

			if (magic != 0x54495553) {
				throw new Exception("Invalid magic value!");
			}

			if (version != 1) {
				throw new Exception("Unexpected version value!");
			}

			// actual data

			file.Seek(-16 - payloadSize, SeekOrigin.End);
			var dr = new DataReader(file);

			_id = dr.ReadString();
			_ = dr.ReadString();
			var author = dr.ReadString();
			_displayName = dr.ReadString();
			_icon = dr.ReadString();
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

		private void WriteProgressionConfig(string suitsPath, bool writeName, string displayNameOverride = null) { // TODO: remove name override when localization writing is implemented
			var configsDir = $"suits/{_id}/configs/";

			var nameKey = (writeName ? ("SUIT_" + _id.ToUpper()) : "CHARWEB_MAY"); // TODO: choose other default lockey
			var displayName = nameKey;
			if (displayNameOverride != null) displayName = displayNameOverride;

			var rewardConfigPath = configsDir + $"inv_reward_loadout_{_id}.config";
			var suitName = $"SUIT_{_id}";
			var varGroupConfigPath = configsDir + $"inv_{_id}_variant_group.config";
			var varGroupName = $"suit_{_id}_var_group";
			var loadoutConfigPath = configsDir + $"itemloadout_{_id}.config";
			var varGroupLoadoutConfigPath = configsDir + $"itemloadout_{_id}_variant_group.config";

			//

			const ulong SYSTEM_PROGRESSION_CONFIG_AID = 0x9C9C72A303FCFA30; // configs/system/system_progression.config

			var config = new Config();
			config.Load(_toc, 0, SYSTEM_PROGRESSION_CONFIG_AID);

			var data = config.ContentSection.Data;
			var suits = (JArray)data["SuitList"]["Suits"];

			foreach (var s in suits) {
				if ((string)s["Name"] == suitName) {
					return; // already added
				}
			}

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
				/*
				["VariantGroup"] = new JObject {
					["Icon"] = new JObject {
						["AssetPath"] = _icon,
						["Autoload"] = false
					},
					["Item"] = varGroupConfigPath,
					["Name"] = varGroupName,
					["Variants"] = new JArray()
				},
				*/
				["WhenValidAutoGive"] = true,
			};

			suits.Add(suit);
			data["SuitList"]["Suits"] = suits;
			config.ContentSection.Data = data;

			config.AddReference(rewardConfigPath);
			config.AddReference(loadoutConfigPath);
			config.AddReference(_icon);
			config.AddReference(varGroupConfigPath);
			config.AddReference(varGroupLoadoutConfigPath);

			var archivePath = Path.Combine(suitsPath, "suits");
			WriteArchive(archivePath, 0, SYSTEM_PROGRESSION_CONFIG_AID, config);
		}

		private void WriteVarGroupLoadoutConfig(string modPath) {
			var varGroupName = $"suit_{_id}_var_group";

			var configsDir = $"suits/{_id}/configs/";
			var varGroupLoadoutConfigPath = configsDir + $"itemloadout_{_id}_variant_group.config";

			var config = Config.Make(
				"ItemLoadoutConfig",
				new List<string> {}
			);

			config.ContentSection.Data = new JObject {
				["Loadout"] = new JObject {
					["ItemLoadoutLists"] = new JArray {
							new JObject {
								["Items"] = new JArray {}
							}
						},
					["Name"] = varGroupName,
				},
			};

			var archivePath = modPath + "_cfg";
			WriteArchive(archivePath, 0, CRC64.Hash(varGroupLoadoutConfigPath), config);
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

		/*
		private void WriteLanguage(string suitsPath, string id, string name) {
			var languagesPath = Path.Combine(suitsPath, "languages");
			if (!Directory.Exists(languagesPath)) {
				Directory.CreateDirectory(languagesPath);
			}

			const ulong LOCALIZATION_AID = 0xBE55D94F171BF8DE; // localization/localization_all.localization
			byte span = LANGUAGES[_language];
			var l = new Localization(_toc.GetAssetReader(span, LOCALIZATION_AID));
			var h = new LocalizationHelper(l);

			var idUpper = id.ToUpper();
			h.Add($"SUIT_{idUpper}", name);
			h.Add($"SUIT_{idUpper}_SHORT", name);
			h.Apply(l);

			byte[] moddedBytes = l.Save();
			WriteArchive(suitsPath, "languages\\base_" + _language, LOCALIZATION_AID, span, moddedBytes);
		}
		*/
	}
}
