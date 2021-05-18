using UnityEngine;

public class StarInfoComponent : MonoBehaviour {
	public TextMesh mesh;

	public string text { set { mesh.text = value; } }
}
