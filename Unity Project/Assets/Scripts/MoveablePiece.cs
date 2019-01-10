﻿using UnityEngine;
using System.Collections;

public class MoveablePiece : MonoBehaviour {

	private GamePiece piece;
	public IEnumerator moveCoroutine;

	void Awake() {
		piece = GetComponent<GamePiece> ();
	}

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {

	}

	// Move a piece with animation
	public void Move(int newX, int newY, float time)
	{
        // Stop moveCoroutine if it's still running from previous call
		if (moveCoroutine != null) {
			StopCoroutine (moveCoroutine);
		}

        // Start moveCoroutine with the new values
		moveCoroutine = MoveCoroutine (newX, newY, time);
		StartCoroutine (moveCoroutine);

	}

	// Animate the move by spreading it over the appropriate number of frames
	public IEnumerator MoveCoroutine(int newX, int newY, float time)
	{
		piece.X = newX;
		piece.Y = newY;

		Vector3 startPos = transform.position;
		Vector3 endPos = piece.GridRef.GetWorldPosition (newX, newY);

		for (float t = 0; t <= 1 * time; t += Time.deltaTime) {        // Time.deltaTime is the amount of time the last frame took

            // Set the position to the current interpolation
            //print("DEBUG Startpos --------- " + startPos + " / " + endPos + " / " + t/time);			/////////// DEBUG statement
			piece.transform.position = Vector3.Lerp(startPos, endPos, t/time);

			yield return 0; 			// This statement waits for one frame
		}

        // Set the piece at endPos in case it wasn't already completely there
        piece.transform.position = endPos;		
	}
}