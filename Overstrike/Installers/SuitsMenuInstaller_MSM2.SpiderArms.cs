// Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.
// This program is free software, and can be redistributed and/or modified by you. It is provided 'as-is', without any warranty.
// For more details, terms and conditions, see GNU General Public License.
// A copy of the that license should come with this program (LICENSE.txt). If not, see <http://www.gnu.org/licenses/>.

using DAT1;
using DAT1.Files;
using Newtonsoft.Json.Linq;
using Overstrike.Utils;
using System.Collections.Generic;
using System.IO;

namespace Overstrike.Installers {
	internal partial class SuitsMenuInstaller_MSM2 {
		private sealed class SpiderArmsRequest {
			public string SuitName { get; }
			public string TargetItemPath { get; }
			public string ArmsModel { get; }

			public SpiderArmsRequest(string suitName, string targetItemPath, string armsModel) {
				SuitName = suitName;
				TargetItemPath = targetItemPath;
				ArmsModel = armsModel;
			}
		}

		private List<SuitsMenuArchiveAsset> ApplyForcedSpiderArms(List<SpiderArmsRequest> requests) {
			var result = new List<SuitsMenuArchiveAsset>();

			foreach (var request in requests) {
				if (!SPIDER_ARMS_MODELS.Contains(request.ArmsModel)) {
					throw new InvalidDataException($"Unsupported Spider-Arms model selected for '{request.SuitName}'");
				}

				var targetItemPath = DAT1.Utils.Normalize(request.TargetItemPath ?? "");
				if (string.IsNullOrEmpty(targetItemPath) || ResolveSuitCharacter(targetItemPath) != MSM2SuitCharacter.Peter) {
					throw new InvalidDataException($"Spider-Arms target for '{request.SuitName}' is not a Peter reward loadout");
				}

				var assetIndex = _toc.FindFirstAssetIndexByPath(targetItemPath);
				if (assetIndex < 0) throw new InvalidDataException($"Reward loadout '{targetItemPath}' is not installed");
				var span = _toc.GetSpanIndexByAssetIndex(assetIndex);
				if (span == null) throw new InvalidDataException($"Reward loadout '{targetItemPath}' has no span");
				var assetId = CRC64.Hash(targetItemPath);

				var config = new Config_I30(_toc.GetAssetReader(targetItemPath));
				var root = config.ContentSection.Data;
				var armsValue = new JObject {
					["Dynamic_Enum_Value_Type"] = new JObject {
						["EnumAsset"] = "enums/hero_ironarmsmodel.dynamicenum",
						["EnumValue"] = request.ArmsModel
					}
				};
				root["DefaultIronArmsModel"] = armsValue;
				root["DamagedIronArmsModel"] = armsValue.DeepClone();
				config.ContentSection.Data = root;
				var bytes = config.Save();
				var header = PrepareConfigHeader(assetId, bytes.Length, $"Reward loadout '{targetItemPath}'");
				result.Add(new SuitsMenuArchiveAsset((byte)span, assetId, bytes, header));
			}

			return result;
		}
	}
}
