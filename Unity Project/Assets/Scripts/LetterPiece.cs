using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LetterPiece : MonoBehaviour {

	public enum WhichLetter
	{
		A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S,
		T, U, V, W, X, Y, Z,
		ANY,
		COUNT
	};

	[System.Serializable]
	public struct LetterSprite
	{
		public WhichLetter letter;
		public Sprite sprite;
	};

	// An array of the LetterSprite structs
	public LetterSprite[] letterSprites;

	private WhichLetter letter;

	public WhichLetter Letter
	{
		get { return letter; }
		set { SetLetter (value); }
	}

	public int NumLetters
	{
		get { return letterSprites.Length; }
	}
		
	private SpriteRenderer sprite;
	private Dictionary<WhichLetter, Sprite> letterSpriteDict;

	void Awake()
	{
		sprite = transform.Find("piece").GetComponent<SpriteRenderer>();
		letterSpriteDict = new Dictionary<WhichLetter, Sprite> ();

		for (int i = 0; i < letterSprites.Length; i++) {
			if (!letterSpriteDict.ContainsKey (letterSprites [i].letter)) {
				letterSpriteDict.Add (letterSprites [i].letter, letterSprites [i].sprite);
			}
		}
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void SetLetter(WhichLetter newLetter)
	{
		letter = newLetter;
		if (letterSpriteDict.ContainsKey (newLetter)) {
			sprite.sprite = letterSpriteDict [newLetter];
		}
	}
}