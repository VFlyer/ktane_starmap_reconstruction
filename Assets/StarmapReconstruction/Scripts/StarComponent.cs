using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class StarComponent : MonoBehaviour {
	private const float PASSABILITY = 0f;
	private const float MAX_SPEED = 1f;
	private const float INFO_MAX_X_OFFSET = .08f - .06f;

	private static readonly Color[] starColors = new Color[] {
		new Color32(0x9b, 0xb2, 0xff, 0xff),
		new Color32(0xd3, 0xdd, 0xff, 0xff),
		new Color32(0xfe, 0xf9, 0xff, 0xff),
		new Color32(0xff, 0xeb, 0xd6, 0xff),
		new Color32(0xff, 0xdf, 0xb8, 0xff),
		new Color32(0xff, 0xd0, 0x96, 0xff),
		new Color32(0xff, 0x52, 0x00, 0xff),
	};

	private static Color GetRandomStarColor() {
		int index = Random.Range(0, starColors.Length - 1);
		float lerp = Random.Range(0f, 1f);
		Color from = starColors[index];
		Color to = starColors[index + 1];
		float[] colorComponents = new float[3].Select((_, i) => {
			float min = Mathf.Min(from[i], to[i]);
			float max = Mathf.Max(from[i], to[i]);
			return min + lerp * (max - min);
		}).ToArray();
		return new Color(colorComponents[0], colorComponents[1], colorComponents[2]);
	}

	public KMSelectable Selectable;
	public Renderer StarRenderer;
	public Light StarHalo;
	public StarInfoComponent StarInfoPrefab;

	public bool Selected;
	public Vector3 Speed;
	public HashSet<StarComponent> connectedStars = new HashSet<StarComponent>();
	public int Id;
	public string Name;
	public string Race;
	public string Regime;

	private bool highlighted = false;
	private GameObject highlight;
	private StarInfoComponent info;

	private void Start() {
		StarRenderer.material.color = GetRandomStarColor();
		StarHalo.color = GetRandomStarColor();
		Selectable.OnSelect += OnSelect;
		Selectable.OnDeselect += OnDeselect;
	}

	private void Update() {
		if (highlight == null) {
			if (Selectable.Highlight.transform.childCount < 1) return;
			highlight = Selectable.Highlight.transform.GetChild(0).gameObject;
		}
		highlight.SetActive(highlighted || Selected);
	}

	private void OnSelect() {
		if (Name == "") return;
		highlighted = true;
		info = Instantiate(StarInfoPrefab);
		info.transform.parent = transform.parent;
		info.transform.localScale = Vector3.one;
		info.transform.localRotation = Quaternion.identity;
		Vector3 pos = transform.localPosition;
		pos.y = .13f;
		pos.z -= .028f * Mathf.Sign(pos.z);
		if (pos.x < -INFO_MAX_X_OFFSET) pos.x = -INFO_MAX_X_OFFSET;
		else if (pos.x > INFO_MAX_X_OFFSET) pos.x = INFO_MAX_X_OFFSET;
		info.transform.localPosition = pos;
		info.text = string.Format("{0}\n{1}\n{2}", Name, Race, Regime);
	}

	private void OnDeselect() {
		if (info == null) return;
		highlighted = false;
		Destroy(info.gameObject);
		info = null;
	}

	public void ApplyForce(Vector3 force) {
		if (highlighted) {
			Speed = Vector3.zero;
			return;
		}
		Speed += force;
		if (float.IsNaN(Speed.magnitude) || float.IsInfinity(Speed.magnitude)) Speed = Vector3.zero;
	}

	public void UpdateCoordinates() {
		transform.localPosition += Speed;
		Speed *= PASSABILITY;
	}
}
