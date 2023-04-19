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

    [System.NonSerialized] public PieceData pieceData = new PieceData();

    public void SetModel()
    {
        if (pieceData.type == Type.Pawn)      { GetComponent<MeshFilter>().mesh = meshPawn; }
        if (pieceData.type == Type.Knight)    { GetComponent<MeshFilter>().mesh = meshKnight; }
        if (pieceData.type == Type.Bishop)    { GetComponent<MeshFilter>().mesh = meshBishop; }
        if (pieceData.type == Type.Rook)      { GetComponent<MeshFilter>().mesh = meshRook; }
        if (pieceData.type == Type.Queen)     { GetComponent<MeshFilter>().mesh = meshQueen; }
        if (pieceData.type == Type.King)      { GetComponent<MeshFilter>().mesh = meshKing; }

        if (pieceData.allegiance == Allegiance.White) { GetComponent<MeshRenderer>().material = pieceController.selectedMaterialForWhite; }
        if (pieceData.allegiance == Allegiance.Black) { GetComponent<MeshRenderer>().material = pieceController.selectedMaterialForBlack; }
    }

    public void SetTile(int tileID)
    {
        //Move the piece to a tile
        if (pieceData.tileID != -1)
        {
            generation.tiles[pieceData.tileID].instancePieceGameObject = null;   //reset old tile's piece reference
        }
        pieceData.tileID = tileID;
        generation.tiles[tileID].instancePieceGameObject = gameObject;      //set new tile's piece reference

        transform.position = generation.tiles[tileID].centroidAndNormal;// * 0.92f;
        transform.rotation = Quaternion.LookRotation(generation.tiles[tileID].centroidAndNormal);
        transform.Rotate(90, 0, 0);
    }
}

public class PieceData
{
    //Default data
    public Piece.Allegiance allegiance = Piece.Allegiance.White;
    public Piece.Type type = Piece.Type.Pawn;
    public int tileID = -1;
    public bool hasMoved = false; //For use with pawns (for 2 moves on the first move) and the king (for castling)
}