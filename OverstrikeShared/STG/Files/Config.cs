// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1.Files;
using DAT1.Sections.Config;
using Newtonsoft.Json.Linq;
using System.IO;

namespace OverstrikeShared.STG.Files {
	public class Config: STG {
		private Config_I30 _config;

		#region sections

		public ConfigTypeSection_I30 TypeSection => _config.TypeSection;
		public ConfigBuiltSection_I30 ContentSection => _config.ContentSection;
		public ConfigReferencesSection ReferencesSection => _config.ReferencesSection;

		public void AddTypeSection() {
			_config.AddSection(ConfigTypeSection_I30.TAG, new ConfigTypeSection_I30());
			TypeSection.SetDat1(_config);
		}

		public void AddBuiltSection() {
			_config.AddSection(ConfigBuiltSection_I30.TAG, new ConfigBuiltSection_I30());
			ContentSection.SetDat1(_config);
		}

		public void AddReferencesSection() { _config.AddSection(ConfigReferencesSection.TAG, new ConfigReferencesSection()); }

		#endregion

		#region STG

		protected override DAT1.DAT1 ReadDat1(BinaryReader br) {
			var header = Header;
			if (header != null) {
				DAT1.Utils.Assert(header.Magic == Config_I30.MAGIC);
			}

			_config = new Config_I30(br);
			DAT1.Utils.Assert(_config.TypeMagic == Config_I30.MAGIC);
			return _config;
		}

		public override byte[] Save(bool packRawIfNoExtras = true) {
			byte[] data = _config.Save();

			var header = Header;
			header.Pairs[0].A = (uint)data.Length;
			Header = header;

			// prevent having to .Save() again
			SetRaw(data);
			SetDat1(null);

			return base.Save(packRawIfNoExtras);
		}

		#endregion

		#region API

		public void AddReference(string path, bool backwardSlashes = true) {
			if (backwardSlashes) {
				path = path.Replace('/', '\\');
			}

			ReferencesSection.Values.Add(new DAT1.Sections.Generic.ReferencesSection.ReferenceEntry() { AssetPathStringOffset = _config.AddString(path) });
		}

		#endregion

		#region creating new

		public static Config Make() {
			var result = new Config {
				_config = Config_I30.Make()
			};

			var header = new AssetHeaderHelper();
			header.Magic = Config_I30.MAGIC;
			header.Pairs.Add(new AssetHeaderHelper.Pair { A = 0, B = 0 });
			result.Header = header;
			result.SetFlag(Flags.INSTALL_HEADER, true);

			result.SetDat1(result._config);

			return result;
		}

		public static Config Make(string type, bool withReferenceSection = false) {
			var config = Make();

			config.AddTypeSection();
			config.TypeSection.Data = new JObject {
				["Type"] = type
			};

			config.AddBuiltSection();
			if (withReferenceSection) {
				config.AddReferencesSection();
			}

			return config;
		}

		public static Config Make(string type, List<string> refs) {
			var config = Make(type, true);

			foreach (var path in refs) {
				config.AddReference(path);
			}

			return config;
		}

		#endregion
	}
}
