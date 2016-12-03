using UnityEngine;
using System.Collections;

public class GamePiece : MonoBehaviour {

	private int x;
	private int y;

	public int X
	{
		get { return x; }
		set {
			if (IsMoveable ()) {
				x = value;
			}
		}
	}

	public int Y
	{
		get { return y; }
		set {
			if (IsMoveable ()) {
				y = value;
			}
		}
	}

	private Grid.PieceType type;

	public Grid.PieceType Type
	{
		get { return type; }
	}

	private Grid grid;

	public Grid GridRef
	{
		get { return grid; }
	}

	private MoveablePiece moveableComponent;

	public MoveablePiece MoveableComponent
	{
		get { return moveableComponent; }
	}

	private LetterPiece letterComponent;

	public LetterPiece LetterComponent
	{
		get { return letterComponent; }
	}

	private ClearablePiece clearableComponent;

	public ClearablePiece ClearableComponent
	{
		get { return clearableComponent; }
	}

	void Awake()
	{
		moveableComponent = GetComponent<MoveablePiece> ();
		letterComponent = GetComponent<LetterPiece> ();
		clearableComponent = GetComponent<ClearablePiece> ();
	}

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {

	}

	public void Init(int _x, int _y, Grid _grid, Grid.PieceType _type)
	{
		x = _x;
		y = _y;
		grid = _grid;
		type = _type;
	}

	void OnMouseEnter()
	{
		grid.EnterPiece (this);
	}

	void OnMouseDown()
	{
		grid.PressedPiece (this);
	}

	void OnMouseUp()
	{
		grid.ReleasePiece ();
	}

	public bool IsMoveable()
	{
		return moveableComponent != null;
	}

	public bool IsLetter()
	{
		return letterComponent != null;
	}

	public bool IsClearable()
	{
		return clearableComponent != null;
	}
}