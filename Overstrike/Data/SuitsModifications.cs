using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Overstrike.Data {
	public class SuitsModifications {
		public List<string> DeletedSuits { get; }
		public List<string> SuitsOrder { get; }
		public Dictionary<string, JObject> Modifications { get; }

		public SuitsModifications(JObject suits) {
			DeletedSuits = new();
			SuitsOrder = new();
			Modifications = new();

			if (suits == null) return;

			if (suits.ContainsKey("delete")) {
				foreach (var suit in suits["delete"]) {
					DeletedSuits.Add((string)suit);
				}
			}

			if (suits.ContainsKey("order")) {
				foreach (var suit in (JArray)suits["order"]) {
					SuitsOrder.Add((string)suit);
				}
			}

			if (suits.ContainsKey("modify")) {
				var modify = (JObject)suits["modify"];
				foreach (var pair in modify.OfType<JProperty>()) {
					if (pair.Value is JObject) {
						Modifications.Add(pair.Name, (JObject)pair.Value);
					}
				}
			}
		}

		public SuitsModifications(List<string> deleted, List<string> order, Dictionary<string, JObject> modify) {
			DeletedSuits = deleted;
			SuitsOrder = order;
			Modifications = modify;
		}

		public JObject Save() {
			var deleted = new JArray();
			var order = new JArray();
			var modify = new JObject();

			foreach (var suit in DeletedSuits) deleted.Add(suit);
			foreach (var suit in SuitsOrder) order.Add(suit);
			foreach (var suit in Modifications) {
				modify[suit.Key] = suit.Value;
			}

			var result = new JObject() {
				["delete"] = deleted,
				["order"] = order,
				["modify"] = modify
			};

			return result;
		}
	}
}
