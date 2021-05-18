using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorridorComponent : MonoBehaviour {
	public KeyValuePair<StarComponent, StarComponent> connection;

	public void UpdatePosition() {
		transform.localPosition = (connection.Key.transform.localPosition + connection.Value.transform.localPosition) / 2f;
		transform.localRotation = Quaternion.LookRotation(connection.Value.transform.localPosition - connection.Key.transform.localPosition, Vector3.up);
		transform.localScale = new Vector3(.001f, .001f, (connection.Key.transform.localPosition - connection.Value.transform.localPosition).magnitude);
	}
}
