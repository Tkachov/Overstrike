// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Newtonsoft.Json.Linq;
using Overstrike.Data;
using Overstrike.Installers;
using Overstrike.Utils;
using OverstrikeShared.Windows;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Overstrike.Windows {
	public partial class ModularWizard: ModularWizardBase {
		ModEntry _mod;

		public ModularWizard(ModEntry mod, Window mainWindow) {
			_mod = mod;

			InitializeComponent();
			Init(mainWindow);
		}

		//

		protected override Grid MainGrid { get => _MainGrid; }
		protected override Label NumberLabel { get => _NumberLabel; }
		protected override TextBox NumberBox { get => _NumberBox; }

		protected override string ModName { get => _mod.Name; }

		private string _iconsStyle;
		protected override string IconsStyle { get => _iconsStyle; }

		protected override JArray LoadLayout() {
			using var modular = ModularInstaller.ReadModularFile(_mod);
			
			var info = ModularInstaller.GetInfo(modular);
			_iconsStyle = (string)info["icons_style"];
			
			var layout = (JArray)info["layout"];
			PrefetchIcons(modular, layout); // needed so we're not reading archive over and over for every single icon
			return layout;
		}

		private void PrefetchIcons(ZipArchive modular, JArray layout) {
			try {
				// maybe move the part that traverses layout to base class, so in case the format changes it's updated in the same place?
				foreach (var layoutEntry in layout) {
					var entryType = (string)layoutEntry[0];
					if (entryType != "module") continue;

					var options = (JArray)layoutEntry[2];
					if (options.Count < 2) continue;

					foreach (var item in options) {
						var path = (string)item[0];
						if (path == "") continue;
						if (_icons.ContainsKey(path)) continue;

						var entry = NestedFiles.GetZipEntryByFullName(modular, path);
						AddIconFromZipEntry(entry, path);
					}
				}
			} catch {}
		}

		protected override ulong LoadSelectedCombinationNumber() {
			return ModularInstaller.GetSelectedCombinationNumber(_mod);
		}

		private Dictionary<string, BitmapSource> _icons = new();
		protected override BitmapSource GetIconByPath(string path) {
			if (path == "") return null;
			if (_icons.TryGetValue(path, out BitmapSource? value)) return value;

			using var modular = ModularInstaller.ReadModularFile(_mod);
			var entry = NestedFiles.GetZipEntryByFullName(modular, path);
			return AddIconFromZipEntry(entry, path);
		}

		private BitmapSource AddIconFromZipEntry(ZipArchiveEntry entry, string path) {
			if (entry == null) return null;

			using var stream = entry.Open();
			var file = new MemoryStream();
			stream.CopyTo(file);
			file.Seek(0, SeekOrigin.Begin);

			BitmapSource icon;
			try {
				icon = OverstrikeShared.Utils.Imaging.LoadImage(file);
			} catch {
				return null; // bad icon => don't add to the list
			}
			_icons[path] = icon;
			return icon;
		}

		protected override void SaveSelection() {
			if (_mod.Extras == null) {
				_mod.Extras = new JObject();
			}

			_mod.Extras["selections"] = GetCurrentCombinationNumber();
			_mod.Extras["description"] = GetCurrentCombinationDescription();
		}
	}
}
