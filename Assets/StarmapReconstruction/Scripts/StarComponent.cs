using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class StarComponent : MonoBehaviour {
	private const float INFO_MAX_X_OFFSET = .08f - .06f;
	private const float HALO_RANGE = .015f;

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

	public bool Selected;
	public bool TwitchPlaysActive;
	public int Id;
	public Vector3 Speed;
	public string Name;
	public string Race;
	public string Regime;
	public TextMesh StarInfo;
	public HashSet<StarComponent> connectedStars = new HashSet<StarComponent>();

	private bool highlighted = false;
	private GameObject highlight;

	private void Start() {
		StarRenderer.material.color = GetRandomStarColor();
		StarHalo.color = GetRandomStarColor();
		Selectable.OnHighlight += OnSelect;
		Selectable.OnHighlightEnded += OnDeselect;
	}

	public void UpdateHaloSize() {
		StarHalo.range = transform.lossyScale.magnitude * HALO_RANGE;
	}

	private void Update() {
		if (highlight == null) {
			if (Selectable.Highlight.transform.childCount < 1) return;
			highlight = Selectable.Highlight.transform.GetChild(0).gameObject;
		}
		highlight.SetActive(highlighted || Selected);
	}

	public void OnSelect() {
		if (Name == "") return;
		highlighted = true;
		StarInfo.text = string.Format("{0}\n{1}\n{2}", Name, Race, Regime);
		if (TwitchPlaysActive) StarInfo.text = string.Format("#{0} {1}", Id, StarInfo.text);
	}

	public void ApplyForce() {
		if (highlighted) Speed = Vector3.zero;
		else {
			transform.localPosition += Speed * Time.deltaTime;
			Speed *= .1f;
		}
	}

	public void OnDeselect() {
		highlighted = false;
		StarInfo.text = "";
	}
}
