using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarmapReconstructionModule : MonoBehaviour {
	private const int STARS_COUNT = 8;
	private const float MAX_X = .1f;
	private const float MIN_Y = .03f;
	private const float MAX_Y = .1f;
	private const float MIN_Z = -0.08f;
	private const float MAX_Z = 0.04f;
	private const float PLANCK_LENGTH = .001f;
	private const float CORRIDOR_FORCE = 1f;
	private const float BORDER_FORCE = .9f;
	private const float STAR_FORCE = .00001f;
	private const float ASYMPTOTIC_MULTIPLIER = .6f;

	private static int moduleIdCounter = 1;

	public KMSelectable Selectable;
	public KMSelectable ClearButton;
	public KMSelectable SubmitButton;
	public KMBombModule Module;
	public KMBombInfo BombInfo;
	public StarComponent StarPrefab;
	public CorridorComponent CorridorPrefab;

	private bool activated = false;
	private int moduleId;
	private Vector3[] starsInitialPositions = new Vector3[] {
		new Vector3(0.05f, 0.044f, -0.047f),
		new Vector3(-0.029f, 0.086f, -0.049f),
		new Vector3(-0.05f, 0.044f, -0.047f),
		new Vector3(0.029f, 0.086f, 0.0086f),
		new Vector3(-0.029f, 0.086f, 0.0086f),
		new Vector3(0.05f, 0.044f, 0.007f),
		new Vector3(-0.05f, 0.044f, 0.007f),
		new Vector3(0.029f, 0.086f, -0.049f),
	};
	private StarComponent[] stars = new StarComponent[STARS_COUNT];
	private List<CorridorComponent> corridors = new List<CorridorComponent>();
	private StarComponent selectedStar = null;

	private void Start() {
		moduleId = moduleIdCounter++;
		List<KMSelectable> children = new List<KMSelectable>();
		for (int i = 0; i < STARS_COUNT; i++) {
			StarComponent star = Instantiate(StarPrefab);
			star.transform.parent = transform;
			star.transform.localPosition = new Vector3(Random.Range(-MAX_X, MAX_X), Random.Range(MIN_Y, MAX_Y), Random.Range(MIN_Z, MAX_Z));
			if (i < starsInitialPositions.Length) star.transform.localPosition = starsInitialPositions[i];
			star.transform.localRotation = Quaternion.identity;
			star.transform.localScale = Vector3.one;
			stars[i] = star;
			star.Selectable.OnInteract += () => { OnStarPressed(star); return false; };
			star.Selectable.Parent = Selectable;
			children.Add(star.Selectable);
		}
		children.Add(ClearButton);
		children.Add(SubmitButton);
		Selectable.Children = children.ToArray();
		Selectable.UpdateChildren();
		Module.OnActivate += Activate;
	}

	private void Activate() {
		StarmapReconstructionData.StarInfo[] starInfos;
		Starmap answerExample;
		StarmapReconstructionData.GenerateStarmap(BombInfo, out starInfos, out answerExample);
		foreach (StarmapReconstructionData.StarInfo star in starInfos) {
			int expectedConnectionsCount = StarmapReconstructionData.GetAdjacentStarsCount(star.race, star.regime, BombInfo);
			Debug.LogFormat("[Starmap Reconstruction #{0}] Star #{1}: {2} {3} {4} ({5})", moduleId, star.id, star.name, star.race, star.regime, expectedConnectionsCount);
		}
		Debug.LogFormat("[Starmap Reconstruction #{0}] Answer example: {1}", moduleId, answerExample.ToShortString());
		starInfos.Shuffle();
		for (int i = 0; i < STARS_COUNT; i++) {
			stars[i].Name = starInfos[i].name;
			stars[i].Race = starInfos[i].race;
			stars[i].Regime = starInfos[i].regime;
			stars[i].Id = starInfos[i].id;
		}
		ClearButton.OnInteract += () => { OnClearButtonPressed(); return false; };
		SubmitButton.OnInteract += () => { OnSubmitButtonPressed(); return false; };
		activated = true;
	}

	private void Update() {
		foreach (StarComponent star in stars) {
			Vector3 force = Vector3.zero;
			float yForceDiff = 1f + ASYMPTOTIC_MULTIPLIER * (star.transform.localPosition.y - MAX_Y) / (MAX_Y - MIN_Y);
			float xDiff = star.transform.localPosition.x;
			force += (xDiff > 0 ? Vector3.left : Vector3.right) * BORDER_FORCE * yForceDiff * Mathf.Pow(xDiff, 2) / (2 * MAX_X);
			float yDiff = star.transform.localPosition.y - (MAX_Y + MIN_Y) / 2;
			force += (yDiff > 0 ? Vector3.down : Vector3.up) * BORDER_FORCE * Mathf.Pow(yDiff, 2) / (MAX_Y - MIN_Y);
			float zDiff = star.transform.localPosition.z - (MAX_Z + MIN_Z) / 2;
			force += (zDiff > 0 ? Vector3.back : Vector3.forward) * BORDER_FORCE * Mathf.Pow(zDiff, 2) / (MAX_Z - MIN_Z);
			foreach (StarComponent otherStar in stars) {
				if (otherStar == star) continue;
				Vector3 dir = star.transform.localPosition - otherStar.transform.localPosition;
				force += dir.normalized * STAR_FORCE / Mathf.Max(PLANCK_LENGTH, Mathf.Pow(dir.magnitude, 2));
				if (star.connectedStars.Contains(otherStar)) force += dir.normalized * -1 * CORRIDOR_FORCE * Mathf.Pow(dir.magnitude, 2);
			}
			star.ApplyForce(force);
			foreach (CorridorComponent corridor in corridors) corridor.UpdatePosition();
		}
		foreach (StarComponent star in stars) {
			star.UpdateCoordinates();
			Vector3 pos = star.transform.localPosition;
			if (pos.x < -MAX_X) {
				pos.x = -MAX_X;
				star.Speed.x = 0;
			} else if (pos.x > MAX_X) {
				pos.x = MAX_X;
				star.Speed.x = 0;
			}
			if (pos.y < MIN_Y) {
				pos.y = MIN_Y;
				star.Speed.y = 0;
			} else if (pos.y > MAX_Y) {
				pos.y = MAX_Y;
				star.Speed.y = 0;
			}
			if (pos.z < MIN_Z) {
				pos.z = MIN_Z;
				star.Speed.z = 0;
			} else if (pos.z > MAX_Z) {
				pos.z = MAX_Z;
				star.Speed.z = 0;
			}
			star.transform.localPosition = pos;
		}
	}

	private void OnClearButtonPressed() {
		Unselect();
		foreach (CorridorComponent corridor in corridors) Destroy(corridor.gameObject);
		foreach (StarComponent star in stars) star.connectedStars = new HashSet<StarComponent>();
		corridors = new List<CorridorComponent>();
	}

	private void OnStarPressed(StarComponent star) {
		if (!activated) return;
		if (selectedStar == null) {
			selectedStar = star;
			star.Selected = true;
			return;
		}
		if (selectedStar == star) { Unselect(); return; }
		if (star.connectedStars.Contains(selectedStar)) {
			selectedStar.connectedStars.Remove(star);
			star.connectedStars.Remove(selectedStar);
			CorridorComponent corridorToDelete = corridors.Find(c =>
				(c.connection.Key == star && c.connection.Value == selectedStar) || (c.connection.Key == selectedStar && c.connection.Value == star)
			);
			corridors.Remove(corridorToDelete);
			Destroy(corridorToDelete.gameObject);
		} else {
			star.connectedStars.Add(selectedStar);
			selectedStar.connectedStars.Add(star);
			CorridorComponent corridor = Instantiate(CorridorPrefab);
			corridor.connection = new KeyValuePair<StarComponent, StarComponent>(star, selectedStar);
			corridor.transform.parent = transform;
			corridor.UpdatePosition();
			corridors.Add(corridor);
		}
		Unselect();
	}

	private void OnSubmitButtonPressed() {
		Unselect();
		Debug.LogFormat("[Starmap Reconstruction #{0}] Submit pressed", moduleId);
		Starmap map = new Starmap(stars.Length);
		foreach (StarComponent star in stars) {
			int expectedAdjacentsCount = StarmapReconstructionData.GetAdjacentStarsCount(star.Race, star.Regime, BombInfo);
			int actualAdjacentsCount = star.connectedStars.Count;
			if (expectedAdjacentsCount != actualAdjacentsCount) {
				Debug.LogFormat("[Starmap Reconstruction #{0}] STRIKE: Star {1} has {2} connected stars. Expected: {3}", moduleId, star.Name, actualAdjacentsCount,
					expectedAdjacentsCount);
				Module.HandleStrike();
				return;
			}
			foreach (StarComponent other in star.connectedStars) map.Add(star.Id, other.Id);
		}
		Debug.LogFormat("[Starmap Reconstruction #{0}] Submitted map: {1}", moduleId, map.ToShortString());
		foreach (StarComponent star in stars) {
			KeyValuePair<string, int>? requiredDistance = StarmapReconstructionData.GetRequiredDistanceFrom(star.Name);
			if (requiredDistance == null) continue;
			string to = requiredDistance.Value.Key;
			int expectedDistance = requiredDistance.Value.Value;
			int otherIndex = stars.IndexOf(s => s.Name == to);
			if (otherIndex < 0) continue;
			StarComponent other = stars[otherIndex];
			int actualDistance = map.GetDistance(star.Id, other.Id);
			if (expectedDistance != actualDistance) {
				Debug.LogFormat("[Starmap Reconstruction #{0}] STRIKE: Distance from {1} to {2} is {3}. Expected: {4}", moduleId, star.Name, other.Name, actualDistance,
					expectedDistance);
				Module.HandleStrike();
				return;
			}
		}
		Debug.LogFormat("[Starmap Reconstruction #{0}] Module solved", moduleId);
		Module.HandlePass();
	}

	private void Unselect() {
		if (selectedStar != null) {
			selectedStar.Selected = false;
			selectedStar = null;
		}
	}
}
