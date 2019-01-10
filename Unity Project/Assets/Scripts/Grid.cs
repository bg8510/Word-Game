using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

//namespace main
//{
    public class Grid : MonoBehaviour
    {
		// Set "bDebug" to false to stop debug statements in the console
		private bool bDebug = true;

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
        public float fillTime;      // Controls the speed of the tile-drop animation

        public PiecePrefab[] piecePrefabs;
        public GameObject backgroundPrefab;
        public Text ScoreWord;
        public Text ScoreAmount;
        public Text TotalBox;
        public Text MovesLeftBox;

        public int MINWORDLENGTH = 4;

		public SortedDictionary<PieceType, GameObject> piecePrefabDict;
        int outValue;

        private GamePiece[,] pieces;

        private bool inverse = false;       // A flag to swap diagonal filling between left and right
        bool needsRefill = false;

        private GamePiece pressedPiece;     // This is the piece user clicked
        private GamePiece enteredPiece;     // This is the piece where user unclicked

        private int CurrentScore;
        private int TotalScore;

        private int NumberOfMoves = 20;

       // public AvlTree<string, int> Dictionary;
        List<GamePiece> match;
        string DictionaryString;
		public StringSearchPod stringSearcher;

        void Awake()
        {
            // Create the dictionary from a .txt file, as an AVL Tree
            //var logFile = File.ReadAllLines(@"..\Unity Project\Assets\Scripts\dictionary.txt");
            //Dictionary = new AvlTree<string, int>();
            int lineCounter = 0;
            string line;

            StreamReader logFile = new StreamReader(@"..\Unity Project\Assets\Scripts\dictionary.txt");
            while ((line = logFile.ReadLine()) != null)
            {
                //Dictionary.Insert(line, line.Length);
                lineCounter++;
                DictionaryString = DictionaryString + " " + line;

            }

            logFile.Close();
        }

        // Use this for initialization
        void Start()
        {
            CurrentScore = 0;

			piecePrefabDict = new SortedDictionary<PieceType, GameObject>();
			stringSearcher = new StringSearchPod(DictionaryString, xDim, yDim);

            for (int i = 0; i < piecePrefabs.Length; i++)
            {
                // If the key is not already in the dictionary, add a new key-value pair
                if (!piecePrefabDict.ContainsKey(piecePrefabs[i].type))
                {
                    piecePrefabDict.Add(piecePrefabs[i].type, piecePrefabs[i].prefab);
                }
            }

            // Fill in the background prefabs for each grid cell
            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    GameObject background = (GameObject)Instantiate(backgroundPrefab, GetWorldPosition(x, y), Quaternion.identity);

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
					pieces[x, y] = SpawnNewPiece(x, y, PieceType.EMPTY);
                }
            }

            // Set some pieces to BLACK
            //Destroy (pieces [1, 4].gameObject);
            //SpawnNewPiece (1, 4, PieceType.BLACK);

            //Destroy (pieces [2, 4].gameObject);
            //SpawnNewPiece (2, 4, PieceType.BLACK);

            //Destroy (pieces [5, 3].gameObject);
            //SpawnNewPiece (5, 3, PieceType.BLACK);

            //Destroy (pieces [5, 4].gameObject);
            //SpawnNewPiece (5, 4, PieceType.BLACK);

            //Destroy (pieces [6, 6].gameObject);
            //SpawnNewPiece (6, 6, PieceType.BLACK);

            //Destroy (pieces [7, 4].gameObject);
            //SpawnNewPiece (7, 4, PieceType.BLACK);

            //Destroy (pieces [4, 0].gameObject);
            //SpawnNewPiece (4, 0, PieceType.BLACK);

			StartCoroutine(FillCoroutine());

			stringSearcher.SetXDim(xDim);
			stringSearcher.SetYDim(yDim);
        }

        // Update is called once per frame
        void Update()
        {

        }

        // The IEnumerator specification means this is a coroutine
        public IEnumerator FillCoroutine()
        {
            bool needsRefill = true;

            while (needsRefill)
            {
                yield return new WaitForSeconds(fillTime * 5);

                // Call FillStep until it's false - which means no more empty spaces
                while (FillStep())
                {
                    inverse = !inverse;
                    yield return new WaitForSeconds(fillTime);      // This is where we set the timing for the tile-drop animation
                }

            //needsRefill = ClearAllValidMatches();
            }

        stringSearcher.CreateStringsFromGrid(pieces);
        }

        public bool FillStep()
        {
            bool movedPiece = false;

            /// Loop through all the pieces except the bottom row
            /// (we're checking if pieces can move down - so we
            /// don't need to check the bottom row)
            for (int y = yDim - 2; y >= 0; y--)
            {
                for (int loopX = 0; loopX < xDim; loopX++)
                {
                    int x = loopX;

                    // This is where we switch direction, if 'inverse' is true
                    if (inverse)
                    {
                        x = xDim - 1 - loopX;
                    }

                    GamePiece piece = pieces[x, y];

                    if (piece.IsMoveable())
                    {
                        GamePiece pieceBelow = pieces[x, y + 1];

                        // If the space is empty, move the above piece down
                        if (pieceBelow.Type == PieceType.EMPTY)
                        {
                            Destroy(pieceBelow.gameObject); // Destroy the piece that was in the space before moving a new piece down
                            piece.MoveableComponent.Move(x, y + 1, fillTime);
                            pieces[x, y + 1] = piece;                   // Add 1 to the current piece's 'y' to move it down
                            SpawnNewPiece(x, y, PieceType.EMPTY);       // And spawn a new piece
                            movedPiece = true;
                        }
                        else
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
                                        GamePiece diagonalPiece = pieces[diagX, y + 1];

                                        if (diagonalPiece.Type == PieceType.EMPTY)
                                        {
                                            bool hasPieceAbove = true;

                                            for (int aboveY = y; aboveY >= 0; aboveY--)
                                            {
                                                GamePiece pieceAbove = pieces[diagX, aboveY];

                                                if (pieceAbove.IsMoveable())    // If the above piece is moveable, no need for diagonal dropping
                                                {
                                                    break;
                                                }
                                                else if (!pieceAbove.IsMoveable() && pieceAbove.Type != PieceType.EMPTY)
                                                {
                                                    hasPieceAbove = false;
                                                    break;
                                                }
                                            }

                                            // If there's no piece above, fill in the space diagonally
                                            if (!hasPieceAbove)
                                            {
                                                Destroy(diagonalPiece.gameObject);
                                                piece.MoveableComponent.Move(diagX, y + 1, fillTime);
                                                pieces[diagX, y + 1] = piece;
                                                SpawnNewPiece(x, y, PieceType.EMPTY);
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
                GamePiece pieceBelow = pieces[x, 0];

                if (pieceBelow.Type == PieceType.EMPTY)
                {
                    // Destroy the EMPTY piece before replacing it with a letter piece
                    Destroy(pieceBelow.gameObject);

                    // Create a new piece in the '-1' row
                    GameObject newPiece = (GameObject)Instantiate(piecePrefabDict[PieceType.LETTER], GetWorldPosition(x, -2), Quaternion.identity);
                    newPiece.transform.parent = transform;

                    pieces[x, 0] = newPiece.GetComponent<GamePiece>();
                    pieces[x, 0].Init(x, -2, this, PieceType.LETTER);           // Here's where we set the position in the '-1' row
                    pieces[x, 0].MoveableComponent.Move(x, 0, fillTime);        // Move it to its position in row 0
                    pieces[x, 0].LetterComponent.SetLetter(GetRandomLetter());

                    movedPiece = true;
                }
            }

            // If movedPiece comes back false, no piece was moved, and the Fill() method knows it's done
            return movedPiece;
        }

        public Vector2 GetWorldPosition(int x, int y)
        {
            return new Vector2(transform.position.x - (xDim / 2.0f) + x,
                transform.position.y + (yDim / 2.0f) - y);
        }

        public GamePiece SpawnNewPiece(int x, int y, PieceType type)
        {
            // Create a new piece using the passed-in x, y and type
            GameObject newPiece = (GameObject)Instantiate(piecePrefabDict[type], GetWorldPosition(x, y), Quaternion.identity);

            // Make the new piece a child of the grid
            newPiece.transform.parent = transform;

            // Add it to the 'pieces' array and run 'Init()'
            pieces[x, y] = newPiece.GetComponent<GamePiece>();
            pieces[x, y].Init(x, y, this, type);

            // Return the new piece
            return pieces[x, y];
        }

        public bool IsAdjacent(GamePiece piece1, GamePiece piece2)
        {
            // If adjacent in the same X...
            if (piece1.X == piece2.X && (int)Mathf.Abs(piece1.Y - piece2.Y) == 1) return true;

            // If adjacent in the same Y...
            else if (piece1.Y == piece2.Y && (int)Mathf.Abs(piece1.X - piece2.X) == 1) return true;

            // If diagonally adjacent...
            else if ((int)Mathf.Abs(piece1.X - piece2.X) == 1 && (int)Mathf.Abs(piece1.X - piece2.X) == 1) return true;

            // If not adjacent...
            return false;
        }

        public void SwapPieces(GamePiece piece1, GamePiece piece2)
        {
            if (piece1.IsMoveable() && piece2.IsMoveable())
            {
                pieces[piece1.X, piece1.Y] = piece2;
                pieces[piece2.X, piece2.Y] = piece1;

                // If any match is found, do the swap
                //if ( Get_ThreeInARow_Match(piece1, piece2.X, piece2.Y) != null 
                //    || Get_ThreeInARow_Match(piece2, piece1.X, piece1.Y) != null
                //    || Get_HorizontalWord_Match(piece1) != null
                //    || Get_HorizontalWord_Match(piece2) != null
                //    || Get_VerticalWord_Match(piece1) != null
                //    || Get_VerticalWord_Match(piece2) != null )
                //{
                int piece1X = piece1.X;
                int piece1Y = piece1.Y;

                // Move piece1 to piece2's spot, and vice-versa
                piece1.MoveableComponent.Move(piece2.X, piece2.Y, fillTime);
                piece2.MoveableComponent.Move(piece1X, piece1Y, fillTime);

                do
                {
                    ClearAllValidMatches();

                    StartCoroutine(FillCoroutine());
                } while (match != null);


                //if (Get_ThreeInARow_Match(piece1, piece2.X, piece2.Y) != null)
                //    clearAMatch(Get_ThreeInARow_Match(piece1, piece2.X, piece2.Y));
                //if (Get_ThreeInARow_Match(piece2, piece1.X, piece1.Y) != null)
                //    clearAMatch(Get_ThreeInARow_Match(piece2, piece1.X, piece1.Y));
                //if (Get_HorizontalWord_Match(piece1) != null)
                //    clearAMatch(Get_HorizontalWord_Match(piece1));
                //if (Get_HorizontalWord_Match(piece2) != null)
                //    clearAMatch(Get_HorizontalWord_Match(piece2));
                //if (Get_VerticalWord_Match(piece1) != null)
                //    clearAMatch(Get_VerticalWord_Match(piece1));
                //if (Get_VerticalWord_Match(piece2) != null)
                //    clearAMatch(Get_VerticalWord_Match(piece2));
                //}
                // If no match found, don't swap
                //else
                //{
                //    pieces[piece1.X, piece1.Y] = piece1;
                //    pieces[piece2.X, piece2.Y] = piece2;
                //}

                /*
                /// Check for horizontal matches on Piece 1's row
                do {
                    match = Get_HorizontalWord_Match (piece1);

                    // Clear the longest horizontal match on Piece 1's row
                    if (match != null)
                    {
                        clearAMatch (match);
                    }

                    StartCoroutine (Fill ());
                } while (match != null);

                /// if piece1 and piece2 are on different rows, then also check piece2's row
                if (piece1.Y != piece2.Y) {
                    do {
                        //Check for horizontal matches for Piece 2
                        match = Get_HorizontalWord_Match (piece2);

                        // Clear the longest match on Piece 2's row
                        clearAMatch (match);

                        StartCoroutine (Fill ());
                    } while (match != null);
                }

                /// Check for vertical matches on Piece 1's row
                do {
                    match = Get_VerticalWord_Match (piece1);

                    // Clear the longest vertical match on Piece 1's column
                    if (match != null) {
                        clearAMatch (match);
                    }

                    StartCoroutine (Fill ());
                } while (match != null);


                /// if piece1 and piece2 are on different columns, also check piece2's column
                if (piece1.X != piece2.X)
                {
                    do {
                        //Check for vertictal matches for Piece 2
                        match = Get_VerticalWord_Match (piece2);

                        // Clear the longest match on Piece 2's row
                        clearAMatch (match);

                        StartCoroutine (Fill ());
                    } while (match != null);
                }
                */
            }
				
			stringSearcher.UpdateStringsOnSwap(piece1, piece2);

            return;
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
            // If there are no more moves, return
            if (NumberOfMoves == 0) return;

            // Count down the moves
            NumberOfMoves--;
            MovesLeftBox.text = "Moves Left:  " + NumberOfMoves.ToString();


            if (IsAdjacent(pressedPiece, enteredPiece))
            {
                SwapPieces(pressedPiece, enteredPiece);
            }
        }
			
        /// This method recieves a single piece and its coordinates, searches
        /// for any words in that piece's row, and returns a List<> of any
        /// tiles that make a word.  Else it returns 'null.'
        //////////////////////////////////////////////////////////////////////
        public List<GamePiece> Get_HorizontalWord_Match(GamePiece piece)
        {
            if (piece.IsLetter())
            {
                //int iteration, currentXPos, startPos;
                int wordLength, startPos;
                string fullString, stringToCheck;
                //string wordString;

                List<GamePiece> horizontalWord = new List<GamePiece>();

                // Clear 'horizontalWord' so we can refill it with new letters
                horizontalWord.Clear();

                // Get all the tiles in the row
                for (startPos = 0; startPos < xDim; startPos++)
                {
                    // Compile a list of tiles to check
                    horizontalWord.Add(pieces[startPos, piece.Y]);
                }

                // Convert list of letter tiles to a string
				fullString = stringSearcher.ConvertListToString(horizontalWord);

                // Check for any words (MINWORDLENGTH or more letters) in the horizontal direction
                for (wordLength = xDim; wordLength >= MINWORDLENGTH; wordLength--)
                {
                    for (startPos = 0; startPos <= xDim - wordLength; startPos++)
                    {
						if (bDebug) print(" == === " + startPos + " == == " + wordLength);
                        
						stringToCheck = fullString.Substring(startPos, wordLength);

						if (bDebug) print(" == === == == " + stringToCheck);

                        // Using 'wordString', look for a match in the dictionary
                        if (DictionaryString.Contains(" " + stringToCheck + " "))
                            //if (Dictionary.Search(stringToCheck, out outValue))
                            return horizontalWord.GetRange(startPos, wordLength);
                    }
                }

                // If no match found, return null
                return null;
            }

            // If no match was found
            return null;
        }

        /// This method recieves a single piece and its coordinates, searches
        /// for any words in that piece's column, and returns a List<> of any
        /// tiles that make a word.  Else it returns 'null.'
        public List<GamePiece> Get_VerticalWord_Match(GamePiece piece)
        {
            if (piece.IsLetter())
            {
                int wordLength, startPos;
                string fullString, stringToCheck;

                List<GamePiece> verticalWord = new List<GamePiece>();

                // Clear 'verticalWord' so we can refill it with new letters
                verticalWord.Clear();

                // Get all the tiles in the column
                for (startPos = 0; startPos <
                yDim; startPos++)
                {
                    // Compile a list of tiles to check
                    verticalWord.Add(pieces[piece.X, startPos]);
                }

                // Convert list of letter tiles to a string
				fullString = stringSearcher.ConvertListToString(verticalWord);

                // Check for any words (MINWORDLENGTH or more letters) in the vertical direction
                //for (wordLength = xDim; wordLength >= MINWORDLENGTH; wordLength--)
                for (wordLength = yDim; wordLength >= MINWORDLENGTH; wordLength--)
                {
                    for (startPos = 0; startPos <= yDim - wordLength; startPos++)
                    {
                        stringToCheck = fullString.Substring(startPos, wordLength);
						if (bDebug) print("Vertical == === == == " + stringToCheck);

                        // Using 'wordString', look for a match in the dictionary
                        if (DictionaryString.Contains(" " + stringToCheck + " "))
                            //if (Dictionary.Search(stringToCheck, out outValue))
                            return verticalWord.GetRange(startPos, wordLength);
                    }
                }

                // If no match found, return null
                return null;
            }

            // If no match was found
            return null;
        }

        /// Takes a list of pieces and returns it as a string
        ////////////////////////////////////////////////////
//        public string ConvertListToString(List<GamePiece> word)
//        {
//            string wordString = null;
//
//            // Step through the List of pieces and add each letter to the output string
//            for (int index = 0; index <= word.Count - 1; index++)
//            {
//                if (word[index].IsLetter())
//                {
//                    wordString = String.Concat(wordString, word[index].LetterComponent.Letter);
//                }
//            }
//
//            return wordString;
//        }

        /// Check for, and clear, any valid matches
        //////////////////////////////////////////
        public bool ClearAllValidMatches()
        {
            // Check for three or more of the same letter
            //for (int y = 0; y < yDim; y++)
            //{
            //	for (int x = 0; x < xDim; x++)
            //	{
            //		if (pieces [x, y].IsClearable ())
            //		{
            //			match = Get_ThreeInARow_Match (pieces [x, y], x, y);

            //                  if (match != null)
            //                  {
            //                      print("Match = " + match.ToString());					// DEBUG statement

            //                      // This clears a match and sets "needsRefill"
            //                      ClearAMatch(match);

            //                      //StartCoroutine(FillCoroutine());
            //                  }
            //		} 
            //          }
            //}

            /// Check for horizontal matches
            for (int y = 0; y < yDim; y++)
            {
                // Clear the longest horizontal match in the row
                if (pieces[0, y].IsClearable())
                {
                    match = Get_HorizontalWord_Match(pieces[0, y]);

                    if (match == null) continue;

					if (bDebug) print("H-Match = " + match.ToString());                 // DEBUG statement
                }

                // This clears a match and sets "needsRefill"
                ClearAMatch(match);


                //StartCoroutine(FillCoroutine());
            }

            /// Check for vertical matches
            for (int x = 0; x < xDim; x++)
            {
            //match = Get_VerticalWord_Match(pieces[x, 0]);

                // Clear the longest vertical match the column
                if (pieces[x, 0].IsClearable())
                {
                    match = Get_VerticalWord_Match(pieces[x, 0]);

                    if (match == null) continue;

                    if (bDebug) print("V-Match = " + match.ToString());                   // DEBUG statement
                }

                // This clears a match and sets "needsRefill"
                ClearAMatch(match);

                //StartCoroutine (FillCoroutine ());
            }

            return needsRefill;
        }

        /// Clear the pieces passed in, and flag the board for a refill
        ///////////////////////////////////////////////////////////////
        public void ClearAMatch(List<GamePiece> matchToClear)
        {
            if (matchToClear != null)
            {
                for (int i = 0; i < matchToClear.Count; i++)
                {          // DEBUG - changed ..Count-1
                    if (ClearPiece(matchToClear[i].X, matchToClear[i].Y))
                    {
                        needsRefill = true;             // If it was able to clear the piece, then it needs to refill
                    }
                }

				string wordString = stringSearcher.ConvertListToString(matchToClear);

                // Send the current word string to the UpdateScore() method
                // and print a message if there's a problem
                if (UpdateScore(wordString) == false)
				{
					if (bDebug) print("Scoring Error on word = " + wordString);
					if (bDebug) print("Current Score = " + CurrentScore);
				}
            }

            return;
        }

        // Clear the piece at the given x and y, and create a new piece
        ///////////////////////////////////////////////////////////////
        public bool ClearPiece(int x, int y)
        {
            if (pieces[x, y].IsClearable() && !pieces[x, y].ClearableComponent.IsBeingCleared)
            {
				if (bDebug) print(" ===== === ======= === ======= ===   Clear " + pieces[x, y].LetterComponent.Letter);     // DEBUG Statement
                pieces[x, y].ClearableComponent.Clear();
                SpawnNewPiece(x, y, PieceType.EMPTY);

                return true;
            }

            return false;
        }

        // Returns a random letter with weighted probabilities based on English
        ///////////////////////////////////////////////////////////////////////
        public LetterPiece.WhichLetter GetRandomLetter()
        {
            // Choose a number between 0 and 97
            int randomKey = UnityEngine.Random.Range(0, 97);
            LetterPiece.WhichLetter randomLetter;

            // Chosse a letter based on weighted chances
            switch (randomKey)
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

        // Update Score
        public bool UpdateScore(string ScoringWord)
        {
            // Calculate scores for this word and the new total
            CurrentScore = (int)Math.Pow(ScoringWord.Length, 3);

            // Extra points if it uses one of the three least common letters
            if (ScoringWord.Contains("Q") && ScoringWord.Contains("Z") && ScoringWord.Contains("J"))
                CurrentScore += ScoringWord.Length * 6;
            else if (ScoringWord.Contains("Q") && ScoringWord.Contains("Z"))
                CurrentScore += ScoringWord.Length * 4;
            else if (ScoringWord.Contains("Q") && ScoringWord.Contains("J"))
                CurrentScore += ScoringWord.Length * 4;
            else if (ScoringWord.Contains("Z") && ScoringWord.Contains("J"))
                CurrentScore += ScoringWord.Length * 4;
            else if (ScoringWord.Contains("Q") || ScoringWord.Contains("Z") || ScoringWord.Contains("J"))
                CurrentScore += ScoringWord.Length * 2;

			if (ScoringWord.Equals("TEXAS"))
				CurrentScore += 10000;
			
            TotalScore = TotalScore + CurrentScore;



            // Set the text on screen
            ScoreWord.text = ScoreWord.text + "\n" + ScoringWord;
            ScoreAmount.text = ScoreAmount.text + "\n" + CurrentScore.ToString();
            TotalBox.text = TotalScore.ToString();

            return true;
        }
    }
//}