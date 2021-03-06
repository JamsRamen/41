﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using gs = GameSettings;

// Responsible for the mechanics of a block

public class Block : MonoBehaviour {
	public GameObject shadow;

	public List<GameObject> cubes;
	public List<IntegerVector3> cubePositions;
	public IntegerVector3 dimensions;
	public int id;

	bool grabbed = false;
	bool delayedUngrabbed = false;
	bool planting = false;
	bool prevGrabbed;

	CubeVFX cubeVFX;

	static int nextId = 1;
	static int GetId () {
		if (nextId == 1000000)
			nextId = 1; 
		return nextId++;
	}

	void Start () {
		id = GetId ();
		prevGrabbed = false;

		BlockManager.AddActive (this);
		shadow.SetActive (false);

		cubeVFX = GetComponent<CubeVFX> ();

		cubePositions = new List<IntegerVector3> ();
		dimensions = new IntegerVector3 ();

		foreach (BoxCollider cube in GetComponents<BoxCollider>()) {
			cubePositions.Add (new IntegerVector3(cube.center));

			dimensions.x = Mathf.Max (dimensions.x, Mathf.RoundToInt(cube.center.x));
			dimensions.y = Mathf.Max (dimensions.y, Mathf.RoundToInt(cube.center.y));
			dimensions.z = Mathf.Max (dimensions.z, Mathf.RoundToInt(cube.center.z));
		}
		for (int i = 0; i < transform.childCount; i++) {
			GameObject cube = transform.GetChild(i).gameObject;

			if (cube != shadow)
				cubes.Add(cube);
		}

		transform.SetPositionAndRotation (transform.position, Random.rotation);

		Snap (transform);
	}

	void Update () {
		if (grabbed) {
			shadow.transform.SetPositionAndRotation (transform.position, transform.rotation);
			Snap (shadow.transform);
		} else if (!planting) {
			transform.position += BlockManager.BlockFallingSpeed (transform.position.y) * Time.deltaTime * Vector3.down;

			//if (BlockArea.IsOccupied (BlockArea.WorldSpaceToIndex (transform.position), cubePositions, transform.rotation)) {
			//	Snap (transform);
			//	Lay ();
			//}
			if (Snap (transform)) {
				Lay ();
			}
		}
		if (planting) {
			IntegerVector3 underIndex = BlockArea.WorldSpaceToIndex (transform.position) + new IntegerVector3(0, -1, 0);

			if (!BlockArea.IsOccupied (underIndex, cubePositions, transform.rotation, id)) {
				Unlay ();
			}
		}

		// Debug
		/*cubeVFX.ApplyColor(Color.blue);
		if (delayedUngrabbed) {
			cubeVFX.ApplyColor(Color.yellow);
		}
		if (grabbed) {
			cubeVFX.ApplyColor(Color.red);
		}
		if (planting) {
			cubeVFX.ApplyColor(Color.green);
		}*/
	}

	public bool IsAutoGrabbable () {
		return !prevGrabbed;
	}



	// Snapping dynamics:

	// snaps the height if necessary
	// @return: did a snap actually occur?
	bool SnapHeight (Transform t) {
		IntegerVector3 index = BlockArea.WorldSpaceToIndex (t.position);

		if (BlockArea.IsOccupied (index, cubePositions, t.rotation, id)) {
			do {
				index.y++;
			} while (BlockArea.IsOccupied (index, cubePositions, t.rotation, id));

			t.SetPositionAndRotation (BlockArea.IndexToWorldSpace (index), t.rotation);

			return true;
		}
		return false;
	}

	Vector3 SnapColumn (Vector3 position, Quaternion rotation) {
		IntegerVector3 transformedDim = rotation * dimensions;

		position.x = BlockArea.SnapDimension (BlockArea.ClampDimension(position.x, transformedDim.x));
		position.z = BlockArea.SnapDimension (BlockArea.ClampDimension(position.z, transformedDim.z));

		return position;
	}

	bool Snap (Transform t) {
		Vector3 position = t.position;

		Quaternion snappedRotation = Utility.Rotation.SnapOrthogonal(t);
		t.SetPositionAndRotation(position, snappedRotation);

		position = SnapColumn (position, snappedRotation);

		t.SetPositionAndRotation(position, snappedRotation);
		return SnapHeight (t);
	}


	// Planting dynamics:

	public void Grab () {
		prevGrabbed = true;
		shadow.SetActive (true);
		grabbed = true;
		delayedUngrabbed = false;

		Unlay ();
	}

	void Ungrab () {
		if (!delayedUngrabbed)
			return;
		delayedUngrabbed = false;
		grabbed = false;

		shadow.SetActive (false);
		if (Snap (transform)) {
			Lay ();
		}
	}

	public void Unlay () {
		if (planting) {
			planting = false;
			BlockArea.Unplace (this);
			StopCoroutine("Plant");
		}
	}

	public void Lay () {
		prevGrabbed = true;
		planting = true;
		BlockArea.Place (this);
		StartCoroutine ("Plant");
	}

	public void DelayedUngrab () {
		delayedUngrabbed = true;

		Invoke ("Ungrab", 0);
	}

	IEnumerator Plant () {
		// Debug
		//cubeVFX.ApplyColor(Color.gray);

		yield return new WaitForSeconds (gs.plantTime);

		// Snap (transform);
		BlockArea.Plant (this);
	}
}
