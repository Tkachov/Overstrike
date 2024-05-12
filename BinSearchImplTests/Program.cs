using DAT1;
using DAT1.Sections.TOC;
using System.Diagnostics;

namespace BinSearchImplTests {
	internal class Program {
		static void Main(string[] args) {
			TestDsarExtract();
		}

		static void TestTocOneSearch() {
			Console.WriteLine("starting...");

			var toc = new TOC_I20();
			toc.Load("toc");

			Console.WriteLine($"toc loaded, {toc.AssetIdsSection.Values.Count} assets");

			var impls = new TocOneSearch {
				AssetIdsSection = toc.AssetIdsSection,
				SpansSection = toc.SpansSection
			};

			// make requests list
			// - take all (span, assetId) pairs from toc
			// - add 100k randomly mutated pairs (to test that -1 returns correctly for unexisting assets)
			// - shuffle pairs to imitate random access yet still test all the possible ones

			List<Tuple<byte, ulong>> requests = new();

			for (var spanIndex = 0; spanIndex < toc.SpansSection.Entries.Count; ++spanIndex) {
				var span = toc.SpansSection.Entries[spanIndex];
				var begin = span.AssetIndex;
				var end = begin + span.Count;
				for (int index = (int)begin; index < end; ++index) {
					requests.Add(new Tuple<byte, ulong>((byte)spanIndex, toc.AssetIdsSection.Ids[index]));
				}
			}

			var rnd = new Random();
			for (var i = 0; i < 100000; ++i) {
				var baseIndex = rnd.Next(requests.Count);
				if (rnd.Next(2) == 0) {
					requests.Add(new Tuple<byte, ulong>((byte)rnd.Next(256), requests[baseIndex].Item2));
				} else {
					var newId = requests[baseIndex].Item2;
					if (rnd.Next(2) == 0) {
						--newId;
					} else {
						++newId;
					}

					requests.Add(new Tuple<byte, ulong>(requests[baseIndex].Item1, newId));
				}
			}

			requests = requests.OrderBy(item => rnd.Next()).ToList();

			Console.WriteLine($"{requests.Count} requests prepped");
			Console.WriteLine($"[0] = {requests[0].Item1}/{requests[0].Item2}");
			Console.WriteLine($"[{requests.Count - 1}] = {requests[requests.Count - 1].Item1}/{requests[requests.Count - 1].Item2}");

			// prep lists in advance to avoid measuring resize memory management

			List<int> oldResults = new();
			List<int> newResults = new();

			foreach (var request in requests) {
				oldResults.Add(-2);
				newResults.Add(-2);
			}

			Console.WriteLine($"running impls...");

			var stopWatchOld = new Stopwatch();
			var stopWatchNew = new Stopwatch();
			if (rnd.Next(2) == 0) { // to exclude possibility of warmup, do either old first new second or the other way around (obv. test needs to be run multiple times)
				Console.WriteLine($"(old then new)");

				// measure old impl

				stopWatchOld.Start();
				for (var i = 0; i < requests.Count; ++i) {
					oldResults[i] = impls.FindAssetIndex(requests[i].Item1, requests[i].Item2);
				}
				stopWatchOld.Stop();

				// measure new impl

				stopWatchNew.Start();
				for (var i = 0; i < requests.Count; ++i) {
					newResults[i] = impls.FindAssetIndexNew(requests[i].Item1, requests[i].Item2);
				}
				stopWatchNew.Stop();
			} else {
				Console.WriteLine($"(new then old)");

				// measure new impl

				stopWatchNew.Start();
				for (var i = 0; i < requests.Count; ++i) {
					newResults[i] = impls.FindAssetIndexNew(requests[i].Item1, requests[i].Item2);
				}
				stopWatchNew.Stop();

				// measure old impl

				stopWatchOld.Start();
				for (var i = 0; i < requests.Count; ++i) {
					oldResults[i] = impls.FindAssetIndex(requests[i].Item1, requests[i].Item2);
				}
				stopWatchOld.Stop();
			}

			var timestampOld = stopWatchOld.Elapsed;
			var timestampNew = stopWatchNew.Elapsed;

			// compare results
			var resultsMatch = true;
			for (var i = 0; i < requests.Count; ++i) {
				if (oldResults[i] != newResults[i]) {
					Console.WriteLine($"[{i}] mismatch, expected {oldResults[i]}, got {newResults[i]}");
					resultsMatch = false;
					break;
				}
			}

			Console.WriteLine("--");
			if (resultsMatch) {
				Console.WriteLine("OK");
				Console.WriteLine($"old time: {timestampOld}");
				Console.WriteLine($"new time: {timestampNew}");
			} else {
				Console.WriteLine("FAIL");
			}
		}

		static void TestTocAllSearch() {
			Console.WriteLine("starting...");

			var toc = new TOC_I20();
			toc.Load("toc");

			Console.WriteLine($"toc loaded, {toc.AssetIdsSection.Values.Count} assets");

			var impls = new TocAllSearch {
				AssetIdsSection = toc.AssetIdsSection,
				SpansSection = toc.SpansSection
			};

			// make requests list
			// - take all (span, assetId) pairs from toc
			// - add 100k randomly mutated pairs (to test that -1 returns correctly for unexisting assets)
			// - shuffle pairs to imitate random access yet still test all the possible ones

			List<Tuple<byte, ulong>> requests = new();

			for (var spanIndex = 0; spanIndex < toc.SpansSection.Entries.Count; ++spanIndex) {
				var span = toc.SpansSection.Entries[spanIndex];
				var begin = span.AssetIndex;
				var end = begin + span.Count;
				for (int index = (int)begin; index < end; ++index) {
					requests.Add(new Tuple<byte, ulong>((byte)spanIndex, toc.AssetIdsSection.Ids[index]));
				}
			}

			var rnd = new Random();
			for (var i = 0; i < 100000; ++i) {
				var baseIndex = rnd.Next(requests.Count);
				if (rnd.Next(2) == 0) {
					requests.Add(new Tuple<byte, ulong>((byte)rnd.Next(256), requests[baseIndex].Item2));
				} else {
					var newId = requests[baseIndex].Item2;
					if (rnd.Next(2) == 0) {
						--newId;
					} else {
						++newId;
					}

					requests.Add(new Tuple<byte, ulong>(requests[baseIndex].Item1, newId));
				}
			}

			requests = requests.OrderBy(item => rnd.Next()).ToList();

			Console.WriteLine($"{requests.Count} requests prepped");
			Console.WriteLine($"[0] = {requests[0].Item1}/{requests[0].Item2}");
			Console.WriteLine($"[{requests.Count - 1}] = {requests[requests.Count - 1].Item1}/{requests[requests.Count - 1].Item2}");

			// prep lists in advance to avoid measuring resize memory management

			List<int[]> oldResults = new();
			List<int[]> newResults = new();

			foreach (var request in requests) {
				oldResults.Add(new int[] { });
				newResults.Add(new int[] { });
			}

			Console.WriteLine($"running impls...");

			var stopWatchOld = new Stopwatch();
			var stopWatchNew = new Stopwatch();
			if (rnd.Next(2) == 0) { // to exclude possibility of warmup, do either old first new second or the other way around (obv. test needs to be run multiple times)
				Console.WriteLine($"(old then new)");

				// measure old impl

				stopWatchOld.Start();
				for (var i = 0; i < requests.Count; ++i) {
					oldResults[i] = impls.FindAssetIndexesById(requests[i].Item2);
				}
				stopWatchOld.Stop();

				// measure new impl

				stopWatchNew.Start();
				for (var i = 0; i < requests.Count; ++i) {
					newResults[i] = impls.FindAssetIndexesByIdNew(requests[i].Item2);
				}
				stopWatchNew.Stop();
			} else {
				Console.WriteLine($"(new then old)");

				// measure new impl

				stopWatchNew.Start();
				for (var i = 0; i < requests.Count; ++i) {
					newResults[i] = impls.FindAssetIndexesByIdNew(requests[i].Item2);
				}
				stopWatchNew.Stop();

				// measure old impl

				stopWatchOld.Start();
				for (var i = 0; i < requests.Count; ++i) {
					oldResults[i] = impls.FindAssetIndexesById(requests[i].Item2);
				}
				stopWatchOld.Stop();
			}

			var timestampOld = stopWatchOld.Elapsed;
			var timestampNew = stopWatchNew.Elapsed;

			// compare results
			var resultsMatch = true;
			for (var i = 0; i < requests.Count; ++i) {
				if (oldResults[i].Length != newResults[i].Length) {
					Console.WriteLine($"[{i}] mismatch, expected length = {oldResults[i].Length}, got {newResults[i].Length}");
					resultsMatch = false;
					break;
				} else {
					for (var j = 0; j < oldResults[i].Length; ++j) {
						if (oldResults[i][j] != newResults[i][j]) {
							Console.WriteLine($"[{i}][{j}] mismatch, expected {oldResults[i][j]}, got {newResults[i][j]}");
							resultsMatch = false;
							break;
						}
					}
				}
			}

			Console.WriteLine("--");
			if (resultsMatch) {
				Console.WriteLine("OK");
				Console.WriteLine($"old time: {timestampOld}");
				Console.WriteLine($"new time: {timestampNew}");
			} else {
				Console.WriteLine("FAIL");
			}
		}

		static List<BlockHeader> MakeTestBlocks(string fn) {
			var archive = File.OpenRead(fn);
			var r = new BinaryReader(archive);
			archive.Seek(12, SeekOrigin.Begin);
			uint blocks_header_end = r.ReadUInt32();

			archive.Seek(32, SeekOrigin.Begin);
			List<BlockHeader> blocks = new();
			while (archive.Position < blocks_header_end) {
				BlockHeader header = new();
				header.realOffset = r.ReadUInt32();
				r.ReadUInt32();
				header.compOffset = r.ReadUInt32();
				r.ReadUInt32();
				header.realSize = r.ReadUInt32();
				header.compSize = r.ReadUInt32();
				header.compressionType = r.ReadByte();
				r.ReadBytes(7);
				blocks.Add(header);
			}

			return blocks;
		}

		static List<Tuple<uint, uint>> MakeTestRequests(string archiveName) {
			List<Tuple<uint, uint>> result = new();

			var toc = new TOC_I29();
			toc.Load("toc_rcra");

			var archiveIndex = -1;
			for (var i = 0; i < toc.ArchivesSection.Entries.Count; ++i) {
				if (toc.ArchivesSection.Entries[i].GetFilename() == archiveName) {
					archiveIndex = i;
					break;
				}
			}

			for (var i = 0; i < toc.SizesSection.Entries.Count; ++i) {
				if (toc.SizesSection.Entries[i].ArchiveIndex == archiveIndex) {
					result.Add(new(toc.SizesSection.Entries[i].Offset, toc.SizesSection.Entries[i].Size));
				}
			}

			return result;
		}

		static void TestDsarExtract() {
			Console.WriteLine("starting...");

			var blocks = MakeTestBlocks("model_environment");
			var requests = MakeTestRequests("d\\model_environment");
			var reps = 100;

			var impls = new DsarExtract {
				blocks = blocks
			};

			var rnd = new Random();
			requests = requests.OrderBy(item => rnd.Next()).ToList();

			Console.WriteLine($"{requests.Count} requests prepped");
			Console.WriteLine($"[0] = {requests[0].Item1}, {requests[0].Item2}");
			Console.WriteLine($"[{requests.Count - 1}] = {requests[requests.Count - 1].Item1}, {requests[requests.Count - 1].Item2}");

			Console.WriteLine($"running impls ({reps} times each)...");

			List<uint> oldResults = new();
			List<uint> newResults = new();

			var stopWatchOld = new Stopwatch();
			var stopWatchNew = new Stopwatch();
			if (rnd.Next(2) == 0) { // to exclude possibility of warmup, do either old first new second or the other way around (obv. test needs to be run multiple times)
				Console.WriteLine($"(old then new)");

				// measure old impl

				stopWatchOld.Start();
				impls.Trace = oldResults;
				for (var i = 0; i < reps; ++i) {
					foreach (var request in requests) {
						impls.ExtractAsset(request.Item1, request.Item1 + request.Item2);
					}
				}
				stopWatchOld.Stop();

				// measure new impl

				stopWatchNew.Start();
				impls.Trace = newResults;
				for (var i = 0; i < reps; ++i) {
					foreach (var request in requests) {
						impls.ExtractAssetNew(request.Item1, request.Item1 + request.Item2);
					}
				}
				stopWatchNew.Stop();
			} else {
				Console.WriteLine($"(new then old)");

				// measure new impl

				stopWatchNew.Start();
				impls.Trace = newResults;
				for (var i = 0; i < reps; ++i) {
					foreach (var request in requests) {
						impls.ExtractAssetNew(request.Item1, request.Item1 + request.Item2);
					}
				}
				stopWatchNew.Stop();

				// measure old impl

				stopWatchOld.Start();
				impls.Trace = oldResults;
				for (var i = 0; i < reps; ++i) {
					foreach (var request in requests) {
						impls.ExtractAsset(request.Item1, request.Item1 + request.Item2);
					}
				}
				stopWatchOld.Stop();
			}

			var timestampOld = stopWatchOld.Elapsed;
			var timestampNew = stopWatchNew.Elapsed;

			// compare results
			Console.WriteLine($"{oldResults.Count} vs {newResults.Count}");

			var resultsMatch = true;
			for (var i = 0; i < oldResults.Count; ++i) {
				if (oldResults[i] != newResults[i]) {
					Console.WriteLine($"[{i}] mismatch, expected {oldResults[i]}, got {newResults[i]}");
					resultsMatch = false;
					break;
				}
			}

			Console.WriteLine("--");
			if (resultsMatch) {
				Console.WriteLine("OK");
				Console.WriteLine($"old time: {timestampOld}");
				Console.WriteLine($"new time: {timestampNew}");
			} else {
				Console.WriteLine("FAIL");
			}
		}
	}

	class TocOneSearch {
		public SpansSection SpansSection;
		public AssetIdsSection AssetIdsSection;

		// -- https://github.com/Tkachov/Overstrike/blob/3986ab06ae45934264b61d1497318a74ec1d8517/DAT1/TOC.cs#L29-L39
		public virtual int FindAssetIndex(byte span, ulong assetId) {
			var spanEntry = SpansSection.Entries[span];
			var begin = spanEntry.AssetIndex;
			var end = begin + spanEntry.Count;
			for (int index = (int)begin; index < end; ++index) {
				if (AssetIdsSection.Ids[index] == assetId)
					return index;
			}

			return -1;
		}
		// --

		public virtual int FindAssetIndexNew(byte span, ulong assetId) {
			var spanEntry = SpansSection.Entries[span];
			var index = AssetIdsSection.Ids.BinarySearch((int)spanEntry.AssetIndex, (int)spanEntry.Count, assetId, null); // default comparer
			if (index < 0) return -1;
			if (AssetIdsSection.Ids[index] != assetId) return -1;
			return index;
		}
	}

	class TocAllSearch {
		public SpansSection SpansSection;
		public AssetIdsSection AssetIdsSection;
		public bool IsLoaded = true;

		// -- https://github.com/Tkachov/Overstrike/blob/3986ab06ae45934264b61d1497318a74ec1d8517/DAT1/TOC.cs#L45-L59
		public virtual int[] FindAssetIndexesById(ulong assetId, bool stopOnFirst = false) {
			List<int> results = new();

			if (IsLoaded) {
				var ids = AssetIdsSection.Ids;
				for (int i = 0; i < ids.Count; ++i) { // linear search =\
					if (ids[i] == assetId) {
						results.Add(i);
						if (stopOnFirst) break;
					}
				}
			}

			return results.ToArray();
		}
		// --

		public virtual int FindAssetIndex(byte span, ulong assetId) {
			var spanEntry = SpansSection.Entries[span];
			var index = AssetIdsSection.Ids.BinarySearch((int)spanEntry.AssetIndex, (int)spanEntry.Count, assetId, null); // default comparer
			if (index < 0) return -1;
			if (AssetIdsSection.Ids[index] != assetId) return -1;
			return index;
		}

		public virtual int[] FindAssetIndexesByIdNew(ulong assetId, bool stopOnFirst = false) {
			List<int> results = new();

			for (var spanIndex = 0; spanIndex < SpansSection.Entries.Count; ++spanIndex) {
				var index = FindAssetIndex((byte)spanIndex, assetId);
				if (index != -1) {
					results.Add(index);
					if (stopOnFirst) break;
				}
			}

			return results.ToArray();
		}
	}

	// -- https://github.com/Tkachov/Overstrike/blob/3986ab06ae45934264b61d1497318a74ec1d8517/DAT1/DSAR.cs#L21-L30
	class BlockHeader {
		public uint realOffset;
		//public uint unk1;
		public uint compOffset;
		//public uint unk2;
		public uint realSize;
		public uint compSize;
		public byte compressionType;
		//public byte[7] unk3;
	}
	// --

	class DsarExtract {
		public List<BlockHeader> blocks;

		// -- https://github.com/Tkachov/Overstrike/blob/3986ab06ae45934264b61d1497318a74ec1d8517/DAT1/DSAR.cs#L69-L90
		/*
				bool started_reading = false;
				foreach (var block in blocks) {
					uint real_end = block.realOffset + block.realSize;
					bool is_first_block = (block.realOffset <= asset_offset && asset_offset < real_end);
					bool is_last_block = (block.realOffset < asset_end && asset_end <= real_end);

					if (is_first_block) started_reading = true;

					if (started_reading) {
						archive.Seek(block.compOffset, SeekOrigin.Begin);
						byte[] compressed = new byte[block.compSize];
						archive.Read(compressed, 0, compressed.Length);
						byte[] decompressed = Decompress(block, compressed);
						uint block_start = Math.Max(block.realOffset, asset_offset) - block.realOffset;
						uint block_end = Math.Min(asset_end, real_end) - block.realOffset;

						for (int i = (int)block_start; i < block_end; ++i)
							bytes[bytes_ptr++] = decompressed[i];
					}

					if (is_last_block) break;
				}
		*/
		// -- rewritten as:
		public List<uint> Trace;

		void Seek(uint pos) {
			Trace.Add(0);
			Trace.Add(pos);
		}

		void Read(uint cnt) {
			Trace.Add(1);
			Trace.Add(cnt);
		}

		void For(uint start, uint end) {
			Trace.Add(2);
			Trace.Add(start);
			Trace.Add(end);
		}

		public void ExtractAsset(uint asset_offset, uint asset_end) {
			bool started_reading = false;
			foreach (var block in blocks) {
				uint real_end = block.realOffset + block.realSize;
				bool is_first_block = (block.realOffset <= asset_offset && asset_offset < real_end);
				bool is_last_block = (block.realOffset < asset_end && asset_end <= real_end);

				if (is_first_block) started_reading = true;

				if (started_reading) {
					Seek(block.compOffset);
					Read(block.compSize);

					uint block_start = Math.Max(block.realOffset, asset_offset) - block.realOffset;
					uint block_end = Math.Min(asset_end, real_end) - block.realOffset;

					For(block_start, block_end);
				}

				if (is_last_block) break;
			}
		}
		// --

		public class BlockHeaderComparer: IComparer<BlockHeader> {
			public int Compare(BlockHeader x, BlockHeader y) {
				if (x == null) {
					if (y == null) {
						return 0;
					} else {
						return -1;
					}
				}

				if (y == null) {
					return 1;
				}
				
				return x.realOffset.CompareTo(y.realOffset);
			}
		}

		public void ExtractAssetNew(uint asset_offset, uint asset_end) {
			var comparer = new BlockHeaderComparer();

			var fakeBlock = new BlockHeader() { realOffset = asset_offset };
			int firstIndex = blocks.BinarySearch(fakeBlock, comparer);
			if (firstIndex < 0) firstIndex = ~firstIndex;			
			if (firstIndex >= blocks.Count || blocks[firstIndex].realOffset > asset_offset) --firstIndex;

			fakeBlock.realOffset = asset_end;
			int lastIndex = blocks.BinarySearch(fakeBlock, comparer);
			if (lastIndex < 0) lastIndex = ~lastIndex;
			if (lastIndex >= blocks.Count || blocks[lastIndex].realOffset == asset_end) --lastIndex;

			bool started_reading = false;
			for (var blockIndex = firstIndex; blockIndex <= lastIndex; ++blockIndex) {
				var block = blocks[blockIndex];
				uint real_end = block.realOffset + block.realSize;
				bool is_first_block = (block.realOffset <= asset_offset && asset_offset < real_end);
				bool is_last_block = (block.realOffset < asset_end && asset_end <= real_end);

				if (is_first_block) started_reading = true;

				if (started_reading) {
					Seek(block.compOffset);
					Read(block.compSize);

					uint block_start = Math.Max(block.realOffset, asset_offset) - block.realOffset;
					uint block_end = Math.Min(asset_end, real_end) - block.realOffset;

					For(block_start, block_end);
				}

				if (is_last_block) break;
			}
		}
	}
}
