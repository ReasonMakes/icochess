using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceController : MonoBehaviour
{
    public Generation generation;
    public GameObject piecePrefab;
    public GameObject validMovePointPrefab;

    [System.NonSerialized] public List<Piece> pieces = new List<Piece> { };

    public struct Move
    {
        public enum MoveType
        {
            Forbidden,
            Move,
            Capture
        }
        public readonly MoveType MOVE_TYPE;

        public int tileID;

        public Move
        (
            MoveType moveType,
            int tileID
        )
        {
            this.MOVE_TYPE = moveType;
            this.tileID = tileID;
        }
    }

    public List<Move> GetValidTilesToMoveTo(Piece piece)
    {
        //List of valid moves we will return
        List<Move> validTilesToMoveTo = new List<Move> { };

        //What are this pieces friends and enemies?
        Piece.Allegiance enemyAllegiance = Piece.Allegiance.None;
        if (piece.allegiance == Piece.Allegiance.White)
        {
            enemyAllegiance = Piece.Allegiance.Black;
        }
        else if (piece.allegiance == Piece.Allegiance.Black)
        {
            enemyAllegiance = Piece.Allegiance.White;
        }

        //Clear old valid move points
        DestroyAllValidMovePoints();

        //Each piece type has their own set of valid moves
        if (piece.type == Piece.Type.Pawn)
        {
            //Pawns move/capture one edge away
            List<int> tilesToCheck = generation.tiles[piece.tileID].adjacentNeighborIDsEdge;
            for (int i = 0; i < tilesToCheck.Count; i++)
            {
                SetBasicMoveTileInteractions(ref validTilesToMoveTo, piece.allegiance, enemyAllegiance, generation.tiles[tilesToCheck[i]].id);
            }
        }
        else if (piece.type == Piece.Type.King)
        {
            //Kings move/capture one edge away - todo: castling
            List<int> tilesToCheck = generation.tiles[piece.tileID].adjacentNeighborIDsEdge;
            for (int i = 0; i < tilesToCheck.Count; i++)
            {
                SetBasicMoveTileInteractions(ref validTilesToMoveTo, piece.allegiance, enemyAllegiance, generation.tiles[tilesToCheck[i]].id);
            }
        }
        else if (piece.type == Piece.Type.Knight)
        {
            //Knights move/capture one side corner away
            List<int> tilesToCheck = generation.tiles[piece.tileID].adjacentNeighborIDsCornerSide;
            for (int i = 0; i < tilesToCheck.Count; i++)
            {
                SetBasicMoveTileInteractions(ref validTilesToMoveTo, piece.allegiance, enemyAllegiance, generation.tiles[tilesToCheck[i]].id);
            }
        }
        else if (piece.type == Piece.Type.Bishop)
        {
            //Initial move - check each "direction" for valid moves - edges first
            //Todo: Allow starting direction to be a direct corner
            List<int> tilesToCheck = generation.tiles[piece.tileID].adjacentNeighborIDsEdge;
            for (int i = 0; i < tilesToCheck.Count; i++)
            {
                //EACH EDGE TILE AROUND THE PIECE
                int immediateTileID = generation.tiles[tilesToCheck[i]].id;

                //Base interactions around immediate edges
                if(!SetBasicMoveTileInteractions(ref validTilesToMoveTo, piece.allegiance, enemyAllegiance, immediateTileID))
                {
                    //Only recur if the immediate tile is unoccupied
                    //Recursion
                    //Setup default variables to be used in recursion
                    int currentTileID = immediateTileID; //start recursion from an immediate edge
                    int previous1TileID = immediateTileID;
                    int previous2TileID = piece.tileID;
                    List<int> tilesToCheckInRecursion;
                    bool previousTileWasEdge = true;

                    int tileDistance = 11;
                    for (int j = 0; j < tileDistance; j++)
                    {
                        //Protect against dead ends
                        bool deadEndThereforeBreakRecursionInThisDirection = false;

                        //Recurse
                        //Alternate between checking edges and corners
                        if (previousTileWasEdge)
                        {
                            tilesToCheckInRecursion = generation.tiles[currentTileID].adjacentNeighborIDsCornerDirect;
                        }
                        else
                        {
                            tilesToCheckInRecursion = generation.tiles[currentTileID].adjacentNeighborIDsEdge;
                        }
                        previousTileWasEdge = !previousTileWasEdge;

                        //Find the next tile in this direction (there will only be one) and consider adding it to the list of valid moves
                        for (int k = 0; k < tilesToCheckInRecursion.Count; k++)
                        {
                            currentTileID = generation.tiles[tilesToCheckInRecursion[k]].id;

                            //PreviousTileID2 and currentTileID must share no vertices - ensures we aren't veering off to the side and aren't going back somewhere we just came from
                            int sharedVertices = generation.GetTileSharedVertices(previous2TileID, currentTileID);
                            //Debug.Log(sharedVertices);
                            //Debug.Log(previous1TileID);

                            if (sharedVertices == 0)
                            {
                                //Valid move tile (as long as not occupied by friendly piece)
                                if (SetBasicMoveTileInteractions(ref validTilesToMoveTo, piece.allegiance, enemyAllegiance, currentTileID))
                                {
                                    //If capture, dead end
                                    deadEndThereforeBreakRecursionInThisDirection = true;
                                }

                                //Debug.Log("Added " + currentTileID + " from " + previous1TileID + ", originating at immediate tile " + immediateTileID);

                                //Break so that we "lock in" the currentTileID so we continue on from that tile instead of continuing from an invalid tile
                                break;
                            }

                            if (k == tilesToCheckInRecursion.Count - 1)
                            {
                                //Dead end - no more valid moves along this direction - "double break" out of these two layers of nested for loops
                                deadEndThereforeBreakRecursionInThisDirection = true;
                            }
                        }

                        if (deadEndThereforeBreakRecursionInThisDirection)
                        {
                            break;
                        }

                        //Update tile history
                        previous2TileID = previous1TileID;
                        previous1TileID = currentTileID;
                    }
                }
            }
        }

        //Return valid moves - anything uncaught will result in a lack of valid moves
        return validTilesToMoveTo;
    }

    private bool SetBasicMoveTileInteractions(ref List<Move> validTilesToMoveTo, Piece.Allegiance thisAllegiance, Piece.Allegiance enemyAllegiance, int tileID)
    {
        //Basic tile interaction:
        //Any tile that is within range of the piece can be moved to as long as it is not occupied by a friendly piece.
        //If an enemy piece occupies it, the enemy piece will be capture and this piece will then occupy the tile the enemy used to.
        //Move points will be spawned on tiles that are valid moves so that the player can see their options. (These are cleared in other scripts.)

        bool isTileOccupied = false;

        Piece.Allegiance pieceAllegienceOnTile = GetTilePieceAllegiance(tileID);
        if (pieceAllegienceOnTile == enemyAllegiance)
        {
            validTilesToMoveTo.Add(new Move(Move.MoveType.Capture, tileID));
            SpawnValidMovePoint(tileID, true);
            isTileOccupied = true;
        }
        else if (pieceAllegienceOnTile == Piece.Allegiance.None)
        {
            validTilesToMoveTo.Add(new Move(Move.MoveType.Move, tileID));
            SpawnValidMovePoint(tileID, false);
        }
        else if (pieceAllegienceOnTile == thisAllegiance)
        {
            isTileOccupied = true;
        }

        return isTileOccupied;
    }

    private void SpawnValidMovePoint(int tileID, bool isCaptureTile)
    {
        GameObject instanceValidMovePoint = Instantiate(
            validMovePointPrefab,
            generation.tiles[tileID].centroidAndNormal * 1.006f, //1.003f,
            Quaternion.identity
        );
        if (isCaptureTile)
        {
            instanceValidMovePoint.transform.localScale = Vector3.one * 0.1f;
        }
        instanceValidMovePoint.transform.parent = generation.points.transform;
        instanceValidMovePoint.transform.rotation = Quaternion.LookRotation(generation.tiles[tileID].centroidAndNormal);
    }

    public void DestroyAllValidMovePoints()
    {
        for (int i = 0; i < generation.points.transform.childCount; i++)
        {
            Destroy(generation.points.transform.GetChild(i).gameObject);
        }
    }

    public Piece.Type GetTilePieceType(int tileID)
    {
        if (generation.tiles[tileID].instancePieceGameObject != null)
        {
            //Occupied
            return generation.tiles[tileID].instancePieceGameObject.GetComponent<Piece>().type;
        }

        //Unoccupied by default
        return Piece.Type.None;
    }

    public Piece.Allegiance GetTilePieceAllegiance(int tileID)
    {
        if (generation.tiles[tileID].instancePieceGameObject != null)
        {
            //Occupied
            return generation.tiles[tileID].instancePieceGameObject.GetComponent<Piece>().allegiance;
        }

        //Unoccupied - no allegiance
        return Piece.Allegiance.None;
    }

    //private MoveRule IsValidMove(Piece piece, int tileIDToMoveTo, int tileIDMovingFrom)
    //{
    //    if (generation.tiles[tileIDToMoveTo].instancePieceGameObject == null)
    //    {
    //        //MOVING - Empty tile
    //        if (piece.type == Piece.Type.Pawn)
    //        {
    //            //Pawns move one edge away
    //            if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.Edge)
    //            {
    //                return MoveRule.Move;
    //            }
    //            else
    //            {
    //                return MoveRule.Forbidden;
    //            }
    //        }
    //        else if (piece.type == Piece.Type.Knight)
    //        {
    //            //Knights move one side corner away
    //            if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.CornerSide)
    //            {
    //                return MoveRule.Move;
    //            }
    //            else
    //            {
    //                return MoveRule.Forbidden;
    //            }
    //        }
    //        else if (piece.type == Piece.Type.Bishop)
    //        {
    //            //Bishops move via direct corners
    //            if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.CornerDirect)
    //            {
    //                return MoveRule.Move;
    //            }
    //            else
    //            {
    //                return MoveRule.Forbidden;
    //            }
    //        }
    //        else if (piece.type == Piece.Type.Rook)
    //        {
    //            //Rooks move via edges
    //            if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.Edge)
    //            {
    //                return MoveRule.Move;
    //            }
    //            else
    //            {
    //                return MoveRule.Forbidden;
    //            }
    //        }
    //        else if (piece.type == Piece.Type.Queen)
    //        {
    //            //Queens move via edges or direct corners
    //            Generation.TileNeighborType neighborType = generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom);
    //            if (neighborType == Generation.TileNeighborType.Edge || neighborType == Generation.TileNeighborType.CornerDirect)
    //            {
    //                return MoveRule.Move;
    //            }
    //            else
    //            {
    //                return MoveRule.Forbidden;
    //            }
    //        }
    //        else if (piece.type == Piece.Type.King)
    //        {
    //            //Kings move one edge away
    //            if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.Edge)
    //            {
    //                return MoveRule.Move;
    //            }
    //            else
    //            {
    //                return MoveRule.Forbidden;
    //            }
    //        }
    //        else
    //        {
    //            Debug.LogError("Uncaught move error");
    //            return MoveRule.Forbidden;
    //        }
    //    }
    //    else
    //    {
    //        //CAPTURING - Occupied tile
    //        if (generation.tiles[tileIDToMoveTo].instancePieceGameObject.GetComponent<Piece>().allegiance == piece.allegiance)
    //        {
    //            //Friendly piece
    //            //Debug.Log("Can't move ontop of or capture own pieces");
    //            return MoveRule.Forbidden;
    //        }
    //        else
    //        {
    //            //Hostile piece
    //            if (piece.type == Piece.Type.Pawn)
    //            {
    //                //Pawns attack one edge away
    //                if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.Edge)
    //                {
    //                    return MoveRule.Capture;
    //                }
    //                else
    //                {
    //                    return MoveRule.Forbidden;
    //                }
    //            }
    //            else if (piece.type == Piece.Type.Knight)
    //            {
    //                //Knights attack one side corner away
    //                if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.CornerSide)
    //                {
    //                    return MoveRule.Capture;
    //                }
    //                else
    //                {
    //                    return MoveRule.Forbidden;
    //                }
    //            }
    //            else if (piece.type == Piece.Type.Bishop)
    //            {
    //                //Bishops attack via direct corners
    //                if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.CornerDirect)
    //                {
    //                    return MoveRule.Capture;
    //                }
    //                else
    //                {
    //                    return MoveRule.Forbidden;
    //                }
    //            }
    //            else if (piece.type == Piece.Type.Rook)
    //            {
    //                //Rooks attack via edges
    //                if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.Edge)
    //                {
    //                    return MoveRule.Capture;
    //                }
    //                else
    //                {
    //                    return MoveRule.Forbidden;
    //                }
    //            }
    //            else if (piece.type == Piece.Type.Queen)
    //            {
    //                //Queens attack via edges or direct corners
    //                Generation.TileNeighborType neighborType = generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom);
    //                if (neighborType == Generation.TileNeighborType.Edge || neighborType == Generation.TileNeighborType.CornerDirect)
    //                {
    //                    return MoveRule.Capture;
    //                }
    //                else
    //                {
    //                    return MoveRule.Forbidden;
    //                }
    //            }
    //            else if (piece.type == Piece.Type.King)
    //            {
    //                //Kings attack one edge away
    //                if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.Edge)
    //                {
    //                    return MoveRule.Capture;
    //                }
    //                else
    //                {
    //                    return MoveRule.Forbidden;
    //                }
    //            }
    //            else
    //            {
    //                Debug.LogError("Uncaught move error");
    //                return MoveRule.Forbidden;
    //            }
    //        }
    //    }
    //}

    public void SpawnPiecesDefault()
    {
        SpawnPiece(Piece.Allegiance.White, Piece.Type.King, 0);
        SpawnPiece(Piece.Allegiance.White, Piece.Type.Queen, 3);
        SpawnPiece(Piece.Allegiance.White, Piece.Type.Bishop, 4);
        SpawnPiece(Piece.Allegiance.White, Piece.Type.Bishop, 16);
        SpawnPiece(Piece.Allegiance.White, Piece.Type.Rook, 19);
        SpawnPiece(Piece.Allegiance.White, Piece.Type.Rook, 7);
        SpawnPiece(Piece.Allegiance.White, Piece.Type.Knight, 5);
        SpawnPiece(Piece.Allegiance.White, Piece.Type.Knight, 18);
        SpawnPiece(Piece.Allegiance.White, Piece.Type.Pawn, 2);
        SpawnPiece(Piece.Allegiance.White, Piece.Type.Pawn, 12);
        SpawnPiece(Piece.Allegiance.White, Piece.Type.Pawn, 8);
        SpawnPiece(Piece.Allegiance.White, Piece.Type.Pawn, 1);

        SpawnPiece(Piece.Allegiance.Black, Piece.Type.King, 52);
        SpawnPiece(Piece.Allegiance.Black, Piece.Type.Queen, 55);
        SpawnPiece(Piece.Allegiance.Black, Piece.Type.Bishop, 48);
        SpawnPiece(Piece.Allegiance.Black, Piece.Type.Bishop, 56);
        SpawnPiece(Piece.Allegiance.Black, Piece.Type.Rook, 51);
        SpawnPiece(Piece.Allegiance.Black, Piece.Type.Rook, 59);
        SpawnPiece(Piece.Allegiance.Black, Piece.Type.Knight, 57);
        SpawnPiece(Piece.Allegiance.Black, Piece.Type.Knight, 50);
        SpawnPiece(Piece.Allegiance.Black, Piece.Type.Pawn, 53);
        SpawnPiece(Piece.Allegiance.Black, Piece.Type.Pawn, 54);
        SpawnPiece(Piece.Allegiance.Black, Piece.Type.Pawn, 40);
        SpawnPiece(Piece.Allegiance.Black, Piece.Type.Pawn, 44);
    }

    private void SpawnPiece(Piece.Allegiance allegiance, Piece.Type type, int tileID)
    {
        GameObject instancePawn = Instantiate(piecePrefab);
        instancePawn.transform.parent = transform;

        Piece instancePieceScript = instancePawn.GetComponent<Piece>();
        instancePieceScript.generation = generation;
        instancePieceScript.allegiance = allegiance;
        instancePieceScript.type = type;
        instancePieceScript.SetModel();
        instancePieceScript.SetTile(tileID);
    }
}