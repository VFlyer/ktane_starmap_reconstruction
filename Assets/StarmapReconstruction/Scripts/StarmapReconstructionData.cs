using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public static class StarmapReconstructionData {
	private struct Distance {
		public string from;
		public string to;
		public int distance;
		public Distance(string from, int distance, string to) {
			this.from = from;
			this.to = to;
			this.distance = distance;
		}
	}

	private static int[][] CorridorsCountVariants = new int[][] {
		new int[] { 1, 1, 1, 1, 1, 1, 2, 6 },
		new int[] { 1, 1, 1, 1, 2, 2, 2, 6 },
		new int[] { 1, 1, 2, 2, 2, 2, 2, 6 },
		new int[] { 2, 2, 2, 2, 2, 2, 2, 6 },
		new int[] { 1, 1, 1, 1, 1, 1, 3, 5 },
		new int[] { 1, 1, 1, 1, 2, 2, 3, 5 },
		new int[] { 1, 1, 2, 2, 2, 2, 3, 5 },
		new int[] { 2, 2, 2, 2, 2, 2, 3, 5 },
		new int[] { 1, 1, 1, 1, 1, 2, 2, 5 },
		new int[] { 1, 1, 1, 2, 2, 2, 2, 5 },
		new int[] { 1, 2, 2, 2, 2, 2, 2, 5 },
		new int[] { 1, 1, 1, 1, 1, 2, 3, 4 },
		new int[] { 1, 1, 1, 2, 2, 2, 3, 4 },
		new int[] { 1, 2, 2, 2, 2, 2, 3, 4 },
		new int[] { 1, 1, 1, 1, 2, 2, 2, 4 },
		new int[] { 1, 1, 2, 2, 2, 2, 2, 4 },
		new int[] { 2, 2, 2, 2, 2, 2, 2, 4 },
		new int[] { 1, 1, 1, 1, 2, 2, 3, 3 },
		new int[] { 1, 1, 2, 2, 2, 2, 3, 3 },
		new int[] { 2, 2, 2, 2, 2, 2, 3, 3 },
		new int[] { 1, 1, 1, 2, 2, 2, 2, 3 },
		new int[] { 1, 2, 2, 2, 2, 2, 2, 3 },
		new int[] { 1, 1, 2, 2, 2, 2, 2, 2 },
		new int[] { 2, 2, 2, 2, 2, 2, 2, 2 },
	};

	private static Distance[] Distances = new Distance[] {
		new Distance("Achernar", 3, "Spica"),
		new Distance("Acrux", 4, "Toliman"),
		new Distance("Adhara", 4, "Deneb"),
		new Distance("Aldebaran", 3, "Betelgeuse"),
		new Distance("Alioth", 6, "KausAustralis"),
		new Distance("Alnitak", 5, "Elnath"),
		new Distance("Antares", 3, "Altair"),
		new Distance("Bellatrix", 5, "Alnair"),
		new Distance("Canopus", 2, "Rigel"),
		new Distance("Capella", 2, "Sirius"),
		new Distance("Miaplacidus", 5, "Alnilam"),
		new Distance("Mimosa", 4, "Gacrux"),
		new Distance("Mirfak", 6, "Dubhe"),
		new Distance("Pollux", 3, "Hadar"),
		new Distance("Procyon", 2, "Arcturus"),
		new Distance("Regulus", 4, "Fomalhaut"),
		new Distance("RigilKentaurus", 2, "Vega"),
		new Distance("Wezen", 7, "Alkaid"),
	};

	public static KeyValuePair<string, int>? GetRequiredDistanceFrom(string from) {
		int i = Distances.IndexOf((d) => d.from == from);
		if (i < 0) return null;
		return new KeyValuePair<string, int>(Distances[i].to, Distances[i].distance);
	}

	private static HashSet<string> _randomStarNames = new HashSet<string> { "Avior", "Sargas", "Menkalinan", "Atria", "Alhena", "Peacock", "Castor", "Mirzam" };
	public static HashSet<string> RandomStarNames { get { return new HashSet<string>(_randomStarNames); } }

	private static string[] _races = new string[] { "Faeyans", "Humans", "Gaals", "Pelengs", "Maloqs" };
	public static string[] Races { get { return _races.Select(a => a).ToArray(); } }

	private static string[] _regimes = new string[] { "Democracy", "Aristocracy", "Monarchy", "Dictatorship", "Anarchy" };
	public static string[] Regimes { get { return _regimes.Select(a => a).ToArray(); } }

	public struct StarInfo {
		public int id;
		public string name;
		public string race;
		public string regime;
		public StarInfo(int id, string name, string race, string regime) {
			this.id = id;
			this.name = name;
			this.race = race;
			this.regime = regime;
		}
	}

	public static int GetAdjacentStarsCount(string race, string regime, KMBombInfo bomb) {
		if (race == "Faeyans") {
			if (regime == "Dictatorship") return bomb.GetOffIndicators().Count() % 6 + 1;
			if (regime == "Democracy") return 6;
			if (regime == "Aristocracy") return 5;
			if (regime == "Monarchy") return 3;
			return 2;
		}
		if (race == "Humans") {
			if (regime == "Democracy") return 4;
			if (regime == "Anarchy") return 1;
			return 2;
		}
		if (race == "Gaals") {
			if (regime == "Monarchy") return bomb.GetBatteryHolderCount() % 6 + 1;
			if (regime == "Democracy" || regime == "Aristocracy") return 2;
			return 1;
		}
		if (race == "Pelengs") {
			if (regime == "Democracy") return bomb.GetSerialNumberNumbers().ToArray()[0] % 6 + 1;
			if (regime == "Aristocracy") return bomb.GetOnIndicators().Count() % 6 + 1;
			if (regime == "Monarchy") return bomb.GetBatteryCount(Battery.D) % 6 + 1;
			if (regime == "Dictatorship") return bomb.GetPortPlateCount() % 6 + 1;
			return 1;
		}
		if (regime == "Democracy") return bomb.GetSerialNumberNumbers().Min() % 6 + 1;
		if (regime == "Anarchy") return bomb.GetPortCount() % 6 + 1;
		return 1;
	}

	public static void GenerateStarmap(KMBombInfo bomb, out StarInfo[] stars, out Starmap answerExample) {
		KeyValuePair<int, int>[] corridorsCount = CorridorsCountVariants.PickRandom().Select((a, i) => new KeyValuePair<int, int>(a, i)).ToArray();
		Debug.LogFormat("<Starmap Reconstruction> Generator: corridors counts: {0}", corridorsCount.Select(kv => kv.Key).Join(""));
		Starmap map = new Starmap(corridorsCount.Length);
		int n = corridorsCount.Length - 1;
		Action<int, int> add = (int i, int j) => {
			map.Add(corridorsCount[i].Value, corridorsCount[j].Value);
			corridorsCount[i] = new KeyValuePair<int, int>(corridorsCount[i].Key - 1, corridorsCount[i].Value);
			corridorsCount[j] = new KeyValuePair<int, int>(corridorsCount[j].Key - 1, corridorsCount[j].Value);
		};
		while (true) {
			Array.Sort(corridorsCount, (a, b) => b.Key - a.Key);
			while (n > 0 && corridorsCount[n].Key == 0) n--;
			if (n == 0) break;
			if (corridorsCount[n].Key == 1) {
				int pos = 0;
				for (int i = 0; i < n; i++) {
					if ((corridorsCount[i].Key % 2 == 1) && (corridorsCount[i].Key > 1)) {
						pos = i;
						break;
					}
				}
				if (corridorsCount[pos].Key == 1 && n > 1) throw new Exception("Unable to create starmap: left corridors eq 1");
				add(pos, n);
				continue;
			}
			if (corridorsCount[n].Key != 2) throw new Exception("Unable to create starmap: corridors counts gt 2");
			if (n < 2) throw new Exception("Unable to create starmap: lt 2 corridors left");
			if (corridorsCount[0].Key == 2) {
				for (int i = 0; i < n - 1; i++) add(i, i + 1);
				add(0, n);
				continue;
			}
			if (corridorsCount[n - 1].Key != 2) throw new Exception("Unable to create starmap: only one corridor eq 2");
			add(n - 1, n);
			add(n, 0);
			add(0, n - 1);
		}
		Dictionary<int, HashSet<KeyValuePair<string, string>>> infos = new Dictionary<int, HashSet<KeyValuePair<string, string>>>();
		foreach (string race in Races) {
			foreach (string regime in Regimes) {
				int adj = GetAdjacentStarsCount(race, regime, bomb);
				if (!infos.ContainsKey(adj)) infos[adj] = new HashSet<KeyValuePair<string, string>>();
				infos[adj].Add(new KeyValuePair<string, string>(race, regime));
			}
		}
		HashSet<int> unnamedStars = new HashSet<int>(Enumerable.Range(0, 8));
		HashSet<string> usedNames = new HashSet<string>();
		string[] starNames = Enumerable.Range(0, 8).Select(_ => "").ToArray();
		while (unnamedStars.Count > 0) {
			int starId = unnamedStars.PickRandom();
			unnamedStars.Remove(starId);
			HashSet<int> otherStars = new HashSet<int>(unnamedStars);
			while (otherStars.Count > 0) {
				int otherStarId = otherStars.PickRandom();
				otherStars.Remove(otherStarId);
				int dist = map.GetDistance(starId, otherStarId);
				IEnumerable<Distance> pairs = Distances.Where(d => d.distance == dist && !usedNames.Contains(d.from) && !usedNames.Contains(d.to));
				if (pairs.Count() == 0) continue;
				Distance pair = pairs.PickRandom();
				starNames[starId] = pair.from;
				starNames[otherStarId] = pair.to;
				usedNames.Add(pair.from);
				usedNames.Add(pair.to);
				unnamedStars.Remove(otherStarId);
				break;
			}
		}
		for (int i = 0; i < 8; i++) {
			if (starNames[i] != "") continue;
			starNames[i] = _randomStarNames.Where(s => !usedNames.Contains(s)).PickRandom();
			usedNames.Add(starNames[i]);
		}
		stars = Enumerable.Range(0, 8).Select(id => {
			int adj = map.GetAdjacentNodesCount(id);
			KeyValuePair<string, string> info = infos[adj].PickRandom();;
			return new StarInfo(id, starNames[id], info.Key, info.Value);
		}).ToArray();
		answerExample = map;
	}
}
