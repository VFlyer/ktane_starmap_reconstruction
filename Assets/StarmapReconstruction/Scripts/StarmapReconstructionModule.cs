using System.Collections.Generic;
using UnityEngine;

public class StarmapReconstructionModule : MonoBehaviour {
	private const int STARS_COUNT = 8;
	private const float GRAVITATIONAL_CONSTANT = .0000067f;
	private const float PLANK_LENGTH = .001f;
	private const float CORRIDOR_FORCE = 18f;
	private const float ATTRACTOR_RADIUS = .06f;
	private const float SPAWN_RADIUS = ATTRACTOR_RADIUS / 2f;
	private const float MAP_ROTATION_SPEED = 45f;
	private const float MAP_ROTATION_ACCELERATION = 1f;

	private float ATTRACTOR_POWER { get { return 8 * GRAVITATIONAL_CONSTANT / Mathf.Pow(ATTRACTOR_RADIUS, 4); } }

	private static int moduleIdCounter = 1;

	public KMAudio Audio;
	public KMSelectable Selectable;
	public KMSelectable ClearButton;
	public KMSelectable SubmitButton;
	public GameObject MapContainer;
	public TextMesh StarInfo;
	public KMBombModule Module;
	public KMBombInfo BombInfo;
	public StarComponent StarPrefab;
	public CorridorComponent CorridorPrefab;

	private bool solved = false;
	private bool activated = false;
	private bool focused = false;
	private int moduleId;
	private float mapRotationInertion = 1f;
	private Vector3 mapRotation;
	private Vector3 mapRotationSpeed;
	private StarComponent[] stars = new StarComponent[STARS_COUNT];
	private List<CorridorComponent> corridors = new List<CorridorComponent>();
	private StarComponent selectedStar = null;

	private void Start() {
		moduleId = moduleIdCounter++;
		mapRotationSpeed = Random.onUnitSphere;
		List<KMSelectable> children = new List<KMSelectable>();
		for (int i = 0; i < STARS_COUNT; i++) {
			StarComponent star = Instantiate(StarPrefab);
			star.transform.parent = MapContainer.transform;
			star.transform.localPosition = Random.rotation * (Vector3.forward * SPAWN_RADIUS);
			star.transform.localRotation = Quaternion.identity;
			star.transform.localScale = Vector3.one;
			stars[i] = star;
			star.StarInfo = StarInfo;
			star.Selectable.OnInteract += () => { OnStarPressed(star); return false; };
			star.Selectable.Parent = Selectable;
			children.Add(star.Selectable);
		}
		children.Add(ClearButton);
		children.Add(SubmitButton);
		Selectable.Children = children.ToArray();
		Selectable.UpdateChildren();
		Selectable.OnFocus += () => focused = true;
		Selectable.OnDefocus += () => focused = false;
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
		for (int i = 0; i < stars.Length; i++) {
			Vector3 move = Vector3.zero;
			StarComponent star = stars[i];
			Vector3 pos = star.transform.localPosition;
			move += -pos.normalized * ATTRACTOR_POWER * Mathf.Pow(pos.magnitude, 2);
			foreach (StarComponent other in stars) {
				if (star == other) continue;
				Vector3 diff = other.transform.localPosition - pos;
				move -= diff.normalized * GRAVITATIONAL_CONSTANT / Mathf.Pow(Mathf.Max(diff.magnitude, PLANK_LENGTH), 2);
				if (star.connectedStars.Contains(other)) move += diff.normalized * CORRIDOR_FORCE * Mathf.Pow(diff.magnitude, 2);
			}
			star.Speed += move;
		}
		foreach (StarComponent star in stars) star.ApplyForce();
		foreach (CorridorComponent corridor in corridors) corridor.UpdatePosition();
		if (!focused) mapRotationInertion = Mathf.Min(1f, mapRotationInertion + Time.deltaTime);
		else mapRotationInertion = Mathf.Max(0, mapRotationInertion - Time.deltaTime);
		mapRotationSpeed = (mapRotationSpeed + Random.onUnitSphere * MAP_ROTATION_ACCELERATION * Time.deltaTime).normalized;
		mapRotation += mapRotationSpeed * Time.deltaTime * MAP_ROTATION_SPEED * mapRotationInertion;
		MapContainer.transform.localRotation = Quaternion.Euler(mapRotation);
	}

	private void OnClearButtonPressed() {
		if (solved || !activated) return;
		Unselect();
		Audio.PlaySoundAtTransform("StarmapReconstructionClear", transform);
		foreach (CorridorComponent corridor in corridors) Destroy(corridor.gameObject);
		foreach (StarComponent star in stars) star.connectedStars = new HashSet<StarComponent>();
		corridors = new List<CorridorComponent>();
	}

	private void OnStarPressed(StarComponent star) {
		if (solved || !activated) return;
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
			Audio.PlaySoundAtTransform("StarmapReconstructionStarsDisconnected", corridorToDelete.transform);
			corridors.Remove(corridorToDelete);
			Destroy(corridorToDelete.gameObject);
		} else {
			star.connectedStars.Add(selectedStar);
			selectedStar.connectedStars.Add(star);
			CorridorComponent corridor = Instantiate(CorridorPrefab);
			corridor.connection = new KeyValuePair<StarComponent, StarComponent>(star, selectedStar);
			corridor.transform.parent = MapContainer.transform;
			corridor.UpdatePosition();
			Audio.PlaySoundAtTransform("StarmapReconstructionStarsConnected", corridor.transform);
			corridors.Add(corridor);
		}
		Unselect();
	}

	private void OnSubmitButtonPressed() {
		if (solved || !activated) return;
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
		solved = true;
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
		Module.HandlePass();
	}

	private void Unselect() {
		if (selectedStar != null) {
			selectedStar.Selected = false;
			selectedStar = null;
		}
	}
}
