// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using Newtonsoft.Json.Linq;
using System.IO;

namespace SuitTool.Data {
	public class Project {
		public string ModName;
		public string Author;
		public string SuitName;
		public string Id;
		public string Hero;

		public const string HERO_PETER = "Peter";
		public const string HERO_MILES = "Miles";

		private bool _New;
		public string MainIcon;
		public string MainModel;
		public string MaskModel;

		public string IronLegs;
		public bool BlackWebs;
		public bool TentacleTraversal;

		public const string LEGS_UNSPECIFIED = "unspecified";

		public List<string> Assets;

		public List<string> Models { get => _models; }
		public List<string> Textures { get => _textures; }
		public List<string> Materials { get => _materials; }
		public List<string> OtherAssets { get => _other; }

		private List<string> _models;
		private List<string> _textures;
		private List<string> _materials;
		private List<string> _other;

		public class Style {
			public string Id = "";
			public string Name = "";
			public string Icon = "";
			public Dictionary<string, string> Overrides = new();
		}

		public List<Style> Styles { get => _styles; }
		private List<Style> _styles;

		public Project() {
			ModName = "";
			Author = "";
			SuitName = "";
			Id = "";
			Hero = HERO_PETER;

			_New = true;
			MainIcon = "";
			MainModel = "";
			MaskModel = "";

			IronLegs = LEGS_UNSPECIFIED;
			BlackWebs = false;
			TentacleTraversal = false;

			Assets = new();
			_models = new();
			_textures = new();
			_materials = new();
			_other = new();

			_styles = new();
		}

		public Project(string filename): this() {
			JObject json = JObject.Parse(File.ReadAllText(filename));

			if (json.ContainsKey("name")) {
				ModName = (string)json["name"];
				SuitName = (string)json["name"];
			} else {
				ModName = (string)json["mod_name"];
				if (ModName == null) throw new Exception("bad project file");

				SuitName = (string)json["suit_name"];
				if (SuitName == null) throw new Exception("bad project file");
			}

			Author = (string)json["author"];
			if (Author == null) throw new Exception("bad project file");

			Id = (string)json["id"];
			if (Id == null) throw new Exception("bad project file");

			Hero = (string)json["hero"];
			if (Hero == null) throw new Exception("bad project file");

			//

			_New = (json.ContainsKey("new"));

			MainIcon = (string)json["main_icon"];
			if (MainIcon == null) throw new Exception("bad project file");

			MainModel = (string)json["main_model"];
			if (MainModel == null) throw new Exception("bad project file");

			MaskModel = (string)json["mask_model"];
			if (MaskModel == null) throw new Exception("bad project file");

			if (json.ContainsKey("iron_legs")) {
				IronLegs = (string)json["iron_legs"];
			}

			if (json.ContainsKey("black_webs")) {
				BlackWebs = (bool)json["black_webs"];
			}

			if (json.ContainsKey("tentacle_traversal")) {
				TentacleTraversal = (bool)json["tentacle_traversal"];
			}

			//

			if (json.ContainsKey("styles")) {
				var styles = (JArray)json["styles"];
				foreach (JObject style in styles) {
					var newStyle = new Style();
					newStyle.Id = (string)style["id"];
					newStyle.Name = (string)style["name"];
					newStyle.Icon = (string)style["icon"];

					var overrides = (JObject)style["overrides"];
					foreach (var prop in overrides.Properties()) {
						if (prop.Value == null) continue;
						
						var value = (string)prop.Value;
						if (value == null || value.Trim() == "") continue;

						newStyle.Overrides[prop.Name] = value;
					}

					_styles.Add(newStyle);
				}
			}

			//

			RefreshAssets(filename);

			if (_New) {
				TryDeducingValues(filename);
				_New = false;
			}
		}

		public bool Save(string filename) {
			try {
				var json = new JObject {
					["mod_name"] = ModName,
					["author"] = Author,
					["suit_name"] = SuitName,
					["id"] = Id,
					["hero"] = Hero,

					["main_icon"] = MainIcon,
					["main_model"] = MainModel,
					["mask_model"] = MaskModel,

					["iron_legs"] = IronLegs,
					["black_webs"] = BlackWebs,
					["tentacle_traversal"] = TentacleTraversal,
				};

				if (_New) {
					json["new"] = true;
				}

				var styles = new JArray();
				foreach (var style in Styles) {
					var overrides = new JObject();
					foreach (var pair in style.Overrides) {
						overrides[pair.Key] = pair.Value;
					}

					styles.Add(new JObject {
						["id"] = style.Id,
						["name"] = style.Name,
						["icon"] = style.Icon,
						["overrides"] = overrides
					});
				}
				json["styles"] = styles;

				File.WriteAllText(filename, json.ToString());
				return true;
			} catch {}

			return false;
		}

		public void RefreshAssets(string filename) {
			Assets.Clear();

			var path = Path.GetDirectoryName(filename);
			for (var spanIndex = 0; spanIndex < 256; ++spanIndex) {
				var spanDir = Path.Combine(path, $"{spanIndex}");
				if (!Directory.Exists(spanDir)) continue;

				var files = Directory.GetFiles(spanDir, "*", SearchOption.AllDirectories);
				foreach (var file in files) {
					var relpath = Path.GetRelativePath(path, file);
					relpath = relpath.Replace("\\", "/");
					Assets.Add(relpath);
				}
			}

			_models.Clear();
			_materials.Clear();
			_textures.Clear();
			_other.Clear();

			foreach (var asset in Assets) {
				if (asset.EndsWith(".model")) {
					_models.Add(asset);
				} else if (asset.EndsWith(".material") || asset.EndsWith(".materialgraph")) {
					_materials.Add(asset);
				} else if (asset.EndsWith(".texture")) {
					_textures.Add(asset);
				} else {
					_other.Add(asset);
				}
			}
		}

		private void TryDeducingValues(string filename) {
			var basename = Path.GetFileNameWithoutExtension(filename);

			if (ModName == null || ModName == "") {
				ModName = basename;
			}

			if (Id == null || Id == "") {
				Id = basename;
			}

			if (basename.Contains("miles", StringComparison.OrdinalIgnoreCase) || Id.Contains("miles", StringComparison.OrdinalIgnoreCase)) {
				Hero = HERO_MILES;
			}

			if (MainIcon == null || MainIcon == "") {
				// TODO: put filter/sort logic here, so it can choose the first icon in the sort (which likely to be right)
				// TODO: also check for "icon" and "main" in the name
				foreach (var texture in _textures) {
					if (texture.Contains("ui", StringComparison.OrdinalIgnoreCase) && texture.Contains("pause", StringComparison.OrdinalIgnoreCase)) {
						MainIcon = texture;
						break;
					}
				}
			}

			if (MaskModel == null || MaskModel == "") {
				foreach (var model in _models) {
					if (model.Contains("mask", StringComparison.OrdinalIgnoreCase)) {
						MaskModel = model;
						break;
					}
				}
			}

			if (MainModel == null || MainModel == "") {
				foreach (var model in _models) {
					if (model == MaskModel) continue;

					if (model.Contains("hero", StringComparison.OrdinalIgnoreCase)) {
						MainModel = model;
						break;
					}
				}
			}
		}

		public void AddStyle(Dictionary<string, string> materials) {
			var style = new Style();
			_styles.Add(style);

			var v = $"var{_styles.Count}";
			style.Id = v;

			// try guessing icon

			foreach (var texture in _textures) {
				var ui_pause = (texture.Contains("ui", StringComparison.OrdinalIgnoreCase) && texture.Contains("pause", StringComparison.OrdinalIgnoreCase));
				var icon = texture.Contains("icon", StringComparison.OrdinalIgnoreCase);
				var variant = texture.Contains(v, StringComparison.OrdinalIgnoreCase);
				if (variant && (ui_pause || icon)) {
					style.Icon = texture;
					break;
				}
			}

			// try guessing overrides based on existing material paths

			string Normalize(string s) {
				return s.Replace('\\', '/');
			}

			var materialsNormalized = new HashSet<string>();
			foreach (var m in _materials) {
				materialsNormalized.Add(Normalize(m));
			}

			string FindMatch(string path, string v) {
				path = Normalize(path);
				
				var guess1 = "0/" + v + path;
				var i = path.LastIndexOf('/');
				if (i != -1) {
					guess1 = "0/" + path[..i] + "/" + v + path[i..];
				}
				if (materialsNormalized.Contains(guess1)) return guess1;

				var guess2 = "0/" + path + v;
				i = path.LastIndexOf('.');
				if (i != -1) {
					guess2 = "0/" + path[..i] + "_" + v + path[i..];
				}
				if (materialsNormalized.Contains(guess2)) return guess2;

				return null;
			}

			foreach (var pair in materials) {
				var slot = pair.Key;
				var path = pair.Value;

				var match = FindMatch(path, v);
				if (match == null) continue;

				style.Overrides[slot] = match;
			}
		}

		public void RefillStyle(Style style, Dictionary<string, string> materials) {
			var indexToUpdate = -1;
			for (var i = 0; i < Styles.Count; ++i) {
				if (Styles[i] == style) {
					indexToUpdate = i;
					break;
				}
			}

			if (indexToUpdate == -1) return;

			var backup = new Style[Styles.Count];
			Styles.CopyTo(backup);

			// remove all styles after the one to update, inclusive
			Styles.RemoveRange(indexToUpdate, Styles.Count - indexToUpdate);
			
			// act as if adding a new style, with correct .Count and thus given name and guesses
			AddStyle(materials);

			// restore everything that was after the style to update
			for (var i = indexToUpdate + 1; i < backup.Length; ++i) {
				Styles.Add(backup[i]);
			}
		}
	}
}
