using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
    //Assigned when instantiated
    [System.NonSerialized] public Generation generation;
    [System.NonSerialized] public PieceController pieceController;

    public Mesh meshPawn;
    public Mesh meshKnight;
    public Mesh meshBishop;
    public Mesh meshRook;
    public Mesh meshQueen;
    public Mesh meshKing;

    public enum Allegiance
    {
        None,
        White,
        Black
    }
    public Allegiance allegiance = Allegiance.White;

    public enum Type
    {
        None,
        Pawn, //Yes, I know pawns are not pieces, however "Material" has a namespace conflict!
        Knight,
        Bishop,
        Rook,
        Queen,
        King
    }
    public Type type = Type.Pawn;

    //Tile this piece is occupying
    public int tileID = -1;

    //For use with pawns (for 2 moves on the first move) and the king (for castling)
    public bool hasMoved = false;

    public void SetModel()
    {
        if (type == Type.Pawn)      { GetComponent<MeshFilter>().mesh = meshPawn; }
        if (type == Type.Knight)    { GetComponent<MeshFilter>().mesh = meshKnight; }
        if (type == Type.Bishop)    { GetComponent<MeshFilter>().mesh = meshBishop; }
        if (type == Type.Rook)      { GetComponent<MeshFilter>().mesh = meshRook; }
        if (type == Type.Queen)     { GetComponent<MeshFilter>().mesh = meshQueen; }
        if (type == Type.King)      { GetComponent<MeshFilter>().mesh = meshKing; }

        if (allegiance == Allegiance.White) { GetComponent<MeshRenderer>().material = pieceController.selectedMaterialForWhite; }
        if (allegiance == Allegiance.Black) { GetComponent<MeshRenderer>().material = pieceController.selectedMaterialForBlack; }
    }

    public void SetTile(int tileID)
    {
        //Move the piece to a tile
        if (this.tileID != -1)
        {
            generation.tiles[this.tileID].instancePieceGameObject = null;   //reset old tile's piece reference
        }
        this.tileID = tileID;
        generation.tiles[tileID].instancePieceGameObject = gameObject;      //set new tile's piece reference

        transform.position = generation.tiles[tileID].centroidAndNormal;// * 0.92f;
        transform.rotation = Quaternion.LookRotation(generation.tiles[tileID].centroidAndNormal);
        transform.Rotate(90, 0, 0);
    }
}