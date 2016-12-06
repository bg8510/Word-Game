using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
//using AssemblyCSharp;

public class Grid : MonoBehaviour {

	public enum PieceType
	{
		EMPTY,
		LETTER,
		BLACK,
		COUNT
	};

	[System.Serializable]
	public struct PiecePrefab
	{
		public PieceType type;
		public GameObject prefab;
	};

	public int xDim;
	public int yDim;
	public float fillTime;		// Controls the speed of the tile-drop anumation

	public PiecePrefab[] piecePrefabs;
	public GameObject backgroundPrefab;

	private Dictionary<PieceType, GameObject> piecePrefabDict;

	private GamePiece[,] pieces;

	private bool inverse = false;		// A flag to swap diagonal filling between left and right

	private GamePiece pressedPiece;		// This is the piece user clicked
	private GamePiece enteredPiece;		// This is the piece where user unclicked

	public List<string> Dictionary;

	void Awake()
	{
		// Create the dictionary from a .txt file, as a List
		var logFile = File.ReadAllLines(@"..\Unity Project\Assets\Scripts\dictionary.txt");
		Dictionary = new List<string>(logFile);

		print(Dictionary[199]);
		print(Dictionary[200]);
		print(Dictionary[201]);
		print(Dictionary[202]);
		print(Dictionary[203]);
		print(Dictionary[204]);
		print(Dictionary[205]);
	}

	// Use this for initialization
	void Start () {
		piecePrefabDict = new Dictionary<PieceType, GameObject> ();

		for (int i = 0; i < piecePrefabs.Length; i++)
		{
			// If the key is not already in the dictionary, add a new key-value pair
			if (!piecePrefabDict.ContainsKey (piecePrefabs [i].type)) {
				piecePrefabDict.Add (piecePrefabs [i].type, piecePrefabs [i].prefab);
			}
		}

		// Fill in the background prefabs for each grid cell
		for (int x = 0; x < xDim; x++)
		{
			for (int y = 0; y < yDim; y++) 
			{
				GameObject background = (GameObject)Instantiate (backgroundPrefab, GetWorldPosition(x, y), Quaternion.identity);

				// Make this background a child of the grid
				background.transform.parent = transform;
			}
		}

		pieces = new GamePiece[xDim, yDim];

		// Fill the board with tiles
		for (int x = 0; x < xDim; x++)
		{
			for (int y = 0; y < yDim; y++)
			{
				// Create a tile using 'SpawnNewPiece()'
				SpawnNewPiece (x, y, PieceType.EMPTY);
			}
		}

		Destroy (pieces [1, 4].gameObject);
		SpawnNewPiece (1, 4, PieceType.BLACK);

		Destroy (pieces [2, 4].gameObject);
		SpawnNewPiece (2, 4, PieceType.BLACK);

		Destroy (pieces [5, 3].gameObject);
		SpawnNewPiece (5, 3, PieceType.BLACK);

		Destroy (pieces [5, 4].gameObject);
		SpawnNewPiece (5, 4, PieceType.BLACK);

		Destroy (pieces [6, 6].gameObject);
		SpawnNewPiece (6, 6, PieceType.BLACK);

		Destroy (pieces [7, 4].gameObject);
		SpawnNewPiece (7, 4, PieceType.BLACK);

		Destroy (pieces [4, 0].gameObject);
		SpawnNewPiece (4, 0, PieceType.BLACK);

		StartCoroutine (Fill ());
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	public IEnumerator Fill()
	{
		bool needsRefill = true;

		while (needsRefill) {
			yield return new WaitForSeconds (fillTime * 5);
		
			// Call FillStep until it's false - which means no more empty spaces
			while (FillStep ()) {
				inverse = !inverse;
				yield return new WaitForSeconds (fillTime);		// This is where we set the timing for the tile-drop animation
			}

			needsRefill = ClearAllValidMatches ();
		}
	}

	public bool FillStep()
	{
		bool movedPiece = false;

		// Loop through all the pieces except the bottom row
		// (we're checking if pieces can move down - so we
		// don't need to check the bottom row)
		for (int y = yDim-2; y >= 0; y--)
		{
			for (int loopX = 0; loopX < xDim; loopX++)
			{
				int x = loopX;

				// This is where we switch direction, if 'inverse' is true
				if (inverse) {
					x = xDim - 1 - loopX;
				}

				GamePiece piece = pieces [x, y];

				if (piece.IsMoveable ())
				{
					GamePiece pieceBelow = pieces [x, y + 1];

					// If the space is empty, move the above piece down
					if (pieceBelow.Type == PieceType.EMPTY)
					{
						Destroy (pieceBelow.gameObject);	// Destroy the piece that was in the space before moving a new piece down
						piece.MoveableComponent.Move (x, y + 1, fillTime);
						pieces [x, y + 1] = piece;					// Add 1 to the current piece's 'y' to move it down
						SpawnNewPiece (x, y, PieceType.EMPTY);		// And spawn a new piece
						movedPiece = true;
					} else
						// This 'else' is for tiles that need to drop down diagonally around an obstacle
					{
						for (int diag = -1; diag <= 1; diag++)
						{
							if (diag != 0)
							{
								int diagX = x + diag;

								if (inverse)
								{
									diagX = x - diag;
								}

								if (diagX >= 0 && diagX < xDim)
								{
									GamePiece diagonalPiece = pieces [diagX, y + 1];

									if (diagonalPiece.Type == PieceType.EMPTY)
									{
										bool hasPieceAbove = true;

										for (int aboveY = y; aboveY >= 0; aboveY--)
										{
											GamePiece pieceAbove = pieces [diagX, aboveY];

											if (pieceAbove.IsMoveable ())	// If the above piece is moveable, no need for diagonal dropping
											{
												break;
											}
											else if(!pieceAbove.IsMoveable() && pieceAbove.Type != PieceType.EMPTY)
											{
												hasPieceAbove = false;
												break;
											}
										}

										// If there's no piece above, fill in the space diagonally
										if (!hasPieceAbove)
										{
											Destroy (diagonalPiece.gameObject);
											piece.MoveableComponent.Move (diagX, y + 1, fillTime);
											pieces [diagX, y + 1] = piece;
											SpawnNewPiece (x, y, PieceType.EMPTY);
											movedPiece = true;
											break;
										}
									} 
								}
							}
						}
					}
				}
			}
		}

		// Now check the top row and fill any gaps with new pieces
		for (int x = 0; x < xDim; x++)
		{
			GamePiece pieceBelow = pieces [x, 0];

			if (pieceBelow.Type == PieceType.EMPTY)
			{
				// Destroy the EMPTY piece before replacing it with a letter piece
				Destroy (pieceBelow.gameObject);

				// Create a new piece in the '-1' row
				GameObject newPiece = (GameObject)Instantiate(piecePrefabDict[PieceType.LETTER], GetWorldPosition(x, -2), Quaternion.identity);
				newPiece.transform.parent = transform;

				pieces [x, 0] = newPiece.GetComponent<GamePiece> ();
				pieces [x, 0].Init (x, -2, this, PieceType.LETTER);			// Here's where we set the position in the '-1' row
				pieces [x, 0].MoveableComponent.Move (x, 0, fillTime);		// Move it to its position in row 0
				// pieces [x, 0].LetterComponent.SetLetter ((LetterPiece.WhichLetter)Random.Range (0, pieces [x, 0].LetterComponent.NumLetters));
				pieces [x, 0].LetterComponent.SetLetter(GetRandomLetter());

				movedPiece = true;
			}
		}

		return movedPiece;
	}

	public Vector2 GetWorldPosition(int x, int y)
	{
		return new Vector2 (transform.position.x - xDim / 2.0f + x,
			transform.position.y + yDim / 2.0f - y);
	}

	public GamePiece SpawnNewPiece(int x, int y, PieceType type)
	{
		// Create a new piece of using the passed-in x, y and type
		GameObject newPiece = (GameObject)Instantiate (piecePrefabDict [type], GetWorldPosition (x, y), Quaternion.identity);

		// Make the new piece a child of the grid
		newPiece.transform.parent = transform;

		// Add it to the 'pieces' array and run 'Init()'
		pieces [x, y] = newPiece.GetComponent<GamePiece> ();
		pieces [x, y].Init (x, y, this, type);

		// Return the new piece
		return pieces [x, y];
	}

	public bool IsAdjacent(GamePiece piece1, GamePiece piece2)
	{
		return (piece1.X == piece2.X && (int)Mathf.Abs (piece1.Y - piece2.Y) == 1)
			|| (piece1.Y == piece2.Y && (int)Mathf.Abs (piece1.X - piece2.X) == 1);
	}

	public void SwapPieces(GamePiece piece1, GamePiece piece2)
	{
		if (piece1.IsMoveable() && piece2.IsMoveable()) {
			pieces [piece1.X, piece1.Y] = piece2;
			pieces [piece2.X, piece2.Y] = piece1;

			int piece1X = piece1.X;
			int piece1Y = piece1.Y;

			piece1.MoveableComponent.Move (piece2.X, piece2.Y, fillTime);
			piece2.MoveableComponent.Move (piece1X, piece1Y, fillTime);

			ClearAllValidMatches ();

			StartCoroutine (Fill ());
		}
	}

	public void PressedPiece(GamePiece piece)
	{
		pressedPiece = piece;
	}

	public void EnterPiece(GamePiece piece)
	{
		enteredPiece = piece;
	}

	public void ReleasePiece()
	{
		if (IsAdjacent(pressedPiece, enteredPiece))
		{
			SwapPieces(pressedPiece, enteredPiece);
		}
	}

	public List<GamePiece> GetMatch(GamePiece piece, int newX, int newY)
	{
		if (piece.IsLetter ()) {
			LetterPiece.WhichLetter letter = piece.LetterComponent.Letter;
			List<GamePiece> horizontalPieces = new List<GamePiece> ();
			List<GamePiece> verticalPieces = new List<GamePiece> ();
			List<GamePiece> matchingPieces = new List<GamePiece> ();

			// First check horizontal
			horizontalPieces.Add(piece);

			for (int dir = 0; dir <= 1; dir++) {
				for (int xOffset = 1; xOffset < xDim; xOffset++) {
					int x;

					if (dir == 0) { 			// Left
						x = newX - xOffset;
					} else { 					// Right
						x = newX + xOffset;
					}

					// If we've moved outside the grid, break
					if (x < 0 || x >= xDim) {
						break;
					}

					// If the piece matches, add it to 'horizontalPieces' list
					if (pieces [x, newY].IsLetter () && pieces [x, newY].LetterComponent.Letter == letter) {
						horizontalPieces.Add (pieces [x, newY]);
					} else {
						// If no match, we're finished searching in this direction
						break;
					}
				}
			}

			if (horizontalPieces.Count >= 3) {
				for (int i = 0; i < horizontalPieces.Count; i++) {
					matchingPieces.Add (horizontalPieces [i]);
				}
			}

			// Traverse vertically if we found a match (for L and T shapes)
			if (horizontalPieces.Count >= 3) {
				for (int i = 0; i < horizontalPieces.Count; i++) {
					for (int dir = 0; dir <= 1; dir++) {
						for (int yOffset = 1; yOffset < yDim; yOffset++) {
							int y;

							if (dir == 0) { 			// Up
								y = newY - yOffset;
							} else { 					// Down
								y = newY + yOffset;
							}

							if (y < 0 || y >= yDim) {
								break;
							}

							if (pieces [horizontalPieces [i].X, y].IsLetter () && pieces [horizontalPieces [i].X, y].LetterComponent.Letter == letter) {
								verticalPieces.Add (pieces [horizontalPieces [i].X, y]);
							} else {
								break;
							}
						}
					}

					if (verticalPieces.Count < 2) {
						verticalPieces.Clear ();
					} else {
						for (int j = 0; j < verticalPieces.Count; j++) {
							matchingPieces.Add (verticalPieces [j]);
						}

						break;
					}
				}
			}

			// if there were 3 or more matching pieces horizontally, return the list
			if (matchingPieces.Count >= 3) {
				return matchingPieces;
			}

			// Didn't find anything going horizontally first,
			// so now check vertically
			verticalPieces.Add(piece);

			for (int dir = 0; dir <= 1; dir++) 
			{
				for (int yOffset = 1; yOffset < yDim; yOffset++) {
					int y;

					if (dir == 0) { 			// Up
						y = newY - yOffset;
					} else { 					// Down
						y = newY + yOffset;
					}

					// If we've moved outside the grid, break
					if (y < 0 || y >= yDim) {
						break;
					}

					// If the piece matches, add it to 'verticalPieces' list
					if (pieces [newX, y].IsLetter () && pieces [newX, y].LetterComponent.Letter == letter) {
						verticalPieces.Add (pieces [newX, y]);
					} else {
						// If no match, we're finished searching in this direction
						break;
					}
				}
			}

			if (verticalPieces.Count >= 3) {
				for (int i = 0; i < verticalPieces.Count; i++) {
					matchingPieces.Add (verticalPieces [i]);
				}
			}

			// Traverse horizontally if we found a match (for L and T shapes)
			if (verticalPieces.Count >= 3) {
				for (int i = 0; i < verticalPieces.Count; i++) {
					for (int dir = 0; dir <= 1; dir++) {
						for (int xOffset = 1; xOffset < xDim; xOffset++) {
							int x;

							if (dir == 0) { 			// Left
								x = newX - xOffset;
							} else { 					// Right
								x = newX + xOffset;
							}

							if (x < 0 || x >= xDim) {
								break;
							}

							if (pieces [x, verticalPieces[i].Y].IsLetter () && pieces [x, verticalPieces[i].Y].LetterComponent.Letter == letter) {
								horizontalPieces.Add (pieces [x, verticalPieces[i].Y]);
							} else {
								break;
							}
						}
					}

					if (horizontalPieces.Count < 2) {
						horizontalPieces.Clear ();
					} else {
						for (int j = 0; j < horizontalPieces.Count; j++) {
							matchingPieces.Add (horizontalPieces [j]);
						}

						break;
					}
				}
			}

			// if there were 3 or more matching pieces vertically, return the list
			if (matchingPieces.Count >= 3) {
				return matchingPieces;
			}
		}

		// If no match was found
		return null;
	}

	public bool ClearAllValidMatches()
	{
		bool needsRefill = false;

		for (int y = 0; y < yDim; y++) {
			for (int x = 0; x < xDim; x++) {
				if (pieces [x, y].IsClearable ()) {
					List<GamePiece> match = GetMatch (pieces [x, y], x, y);

					if (match != null) {
						for (int i = 0; i < match.Count; i++) {
							if (ClearPiece (match [i].X, match [i].Y)) {
								needsRefill = true;				// If it was able to clear the piece, then it needs to refill
							}
						}
					}
				}
			}
		}

		return needsRefill;
	}

	public bool ClearPiece(int x, int y)
	{
		if (pieces [x, y].IsClearable () && !pieces [x, y].ClearableComponent.IsBeingCleared) {
			pieces [x, y].ClearableComponent.Clear ();
			SpawnNewPiece (x, y, PieceType.EMPTY);

			return true;
		}

		return false;
	}

	public LetterPiece.WhichLetter GetRandomLetter()
	{
		// Choose a number between 0 and 97
		int randomKey = UnityEngine.Random.Range (0, 97);
		LetterPiece.WhichLetter randomLetter;

		// Chosse a letter based on weighted chances
		switch(randomKey)
		{
		case 0:
			randomLetter = LetterPiece.WhichLetter.J;
			break;
		case 1:
			randomLetter = LetterPiece.WhichLetter.K;
			break;
		case 2:
			randomLetter = LetterPiece.WhichLetter.Q;
			break;
		case 3:
			randomLetter = LetterPiece.WhichLetter.X;
			break;
		case 4:
			randomLetter = LetterPiece.WhichLetter.Z;
			break;
		case 5:
		case 6:
			randomLetter = LetterPiece.WhichLetter.B;
			break;
		case 7:
		case 8:
			randomLetter = LetterPiece.WhichLetter.C;
			break;
		case 9:
		case 10:
			randomLetter = LetterPiece.WhichLetter.F;
			break;
		case 11:
		case 12:
			randomLetter = LetterPiece.WhichLetter.M;
			break;
		case 13:
		case 14:
			randomLetter = LetterPiece.WhichLetter.P;
			break;
		case 15:
		case 16:
			randomLetter = LetterPiece.WhichLetter.V;
			break;
		case 17:
		case 18:
			randomLetter = LetterPiece.WhichLetter.W;
			break;
		case 19:
		case 20:
			randomLetter = LetterPiece.WhichLetter.Y;
			break;
		case 21:
		case 22:
			randomLetter = LetterPiece.WhichLetter.H;
			break;
		case 23:
		case 24:
		case 25:
			randomLetter = LetterPiece.WhichLetter.G;
			break;
		case 26:
		case 27:
		case 28:
		case 29:
			randomLetter = LetterPiece.WhichLetter.D;
			break;
		case 30:
		case 31:
		case 32:
		case 33:
			randomLetter = LetterPiece.WhichLetter.L;
			break;
		case 34:
		case 35:
		case 36:
		case 37:
			randomLetter = LetterPiece.WhichLetter.S;
			break;
		case 38:
		case 39:
		case 40:
		case 41:
			randomLetter = LetterPiece.WhichLetter.U;
			break;
		case 42:
		case 43:
		case 44:
		case 45:
		case 46:
		case 47:
			randomLetter = LetterPiece.WhichLetter.N;
			break;
		case 48:
		case 49:
		case 50:
		case 51:
		case 52:
		case 53:
			randomLetter = LetterPiece.WhichLetter.R;
			break;
		case 54:
		case 55:
		case 56:
		case 57:
		case 58:
		case 59:
			randomLetter = LetterPiece.WhichLetter.T;
			break;
		case 60:
		case 61:
		case 62:
		case 63:
		case 64:
		case 65:
		case 66:
		case 67:
			randomLetter = LetterPiece.WhichLetter.O;
			break;
		case 68:
		case 69:
		case 70:
		case 71:
		case 72:
		case 73:
		case 74:
		case 75:
		case 76:
			randomLetter = LetterPiece.WhichLetter.A;
			break;
		case 77:
		case 78:
		case 79:
		case 80:
		case 81:
		case 82:
		case 83:
		case 84:
		case 85:
			randomLetter = LetterPiece.WhichLetter.I;
			break;
		default:
			randomLetter = LetterPiece.WhichLetter.E;
			break;
		}

		return randomLetter;
	}
}