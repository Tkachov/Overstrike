﻿using System.IO.Compression;
using System.IO;
using System;
using System.Collections.Generic;

namespace Overstrike.Detectors {
	internal class SuitModDetector: DetectorBase {
		public SuitModDetector() : base() {}

		public override string[] GetExtensions() {
			return new string[] {"suit"};
		}

		public override void Detect(Stream file, string path, List<ModEntry> mods) {
			try {
				ModEntry.ModType detectedModType = ModEntry.ModType.UNKNOWN;

				using (ZipArchive zip = new ZipArchive(file)) {
					// find important files

					ZipArchiveEntry idTxt = null;
					ZipArchiveEntry infoTxt = null;

					foreach (ZipArchiveEntry entry in zip.Entries) {
						if (entry.Name.Equals("id.txt", StringComparison.OrdinalIgnoreCase)) {
							idTxt = entry;
						} else if (entry.Name.Equals("info.txt", StringComparison.OrdinalIgnoreCase)) {
							infoTxt = entry;
						}
					}

					if (idTxt == null || infoTxt == null) {
						return;
					}

					// read id.txt

					string id = null;
					using (var stream = idTxt.Open()) {
						using (StreamReader reader = new StreamReader(stream)) {
							var str = reader.ReadToEnd();
							var lines = str.Split("\n");
							if (lines.Length > 0) {
								id = lines[0].Trim();
							}
						}
					}

					if (id == null) {
						return;
					}

					// check assets file (<id>) exists

					bool hasAssets = false;
					foreach (ZipArchiveEntry entry in zip.Entries) {
						if (entry.FullName.Equals(id + "/" + id, StringComparison.OrdinalIgnoreCase)) {
							hasAssets = true;
							break;
						}
					}

					if (!hasAssets) {
						return;
					}

					// read info.txt

					using (var stream = infoTxt.Open()) {
						var firstByte = stream.ReadByte();

						if (infoTxt.Length % 21 == 1) {
							// version 0
							detectedModType = ModEntry.ModType.SUIT_MSMR;
						} else if (infoTxt.Length % 21 == 2) {
							if (firstByte == 0) {
								// version 1 / MSMR
								detectedModType = ModEntry.ModType.SUIT_MSMR;
							} else if (firstByte == 1) {
								// version 1 / MM
								detectedModType = ModEntry.ModType.SUIT_MM;
							}
						} else if (infoTxt.Length % 17 == 2) {
							if (firstByte == 2) {
								// version 2 / MM
								detectedModType = ModEntry.ModType.SUIT_MM_V2;
							}
						}
					}
				}

				if (detectedModType != ModEntry.ModType.UNKNOWN) {
					var shortPath = GetShortPath(path);
					var name = Path.GetFileName(shortPath);
					mods.Add(new ModEntry(name, path, detectedModType));
				}
			} catch (Exception) { }
		}
	}
}