using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceController : MonoBehaviour
{
    public Generation generation;
    public GameObject piecePrefab;
    public GameObject validMovePointPrefab;

    [System.NonSerialized] public Material selectedMaterialForWhite;
    [System.NonSerialized] public Material selectedMaterialForBlack;

    public Material flatMetalBlue;
    public Material flatMetalRed;
    public Material smoothMetalWhite;
    public Material smoothMetalBlack;
    public Material smoothMetalBlue;
    public Material smoothMetalRed;

    [System.NonSerialized] public List<Piece> pieces = new List<Piece> { };

    private readonly int WHITE_KING_SPAWN_TILE_ID = 0;
    private readonly int BLACK_KING_SPAWN_TILE_ID = 52;

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

    private void Awake()
    {
        selectedMaterialForWhite = smoothMetalBlue;
        selectedMaterialForBlack = smoothMetalRed;
    }

    public List<Move> GetValidTilesToMoveTo(Piece piece)
    {
        //List of valid moves we will return
        List<Move> validTilesToMoveTo = new List<Move> { };

        //What are this pieces friends and enemies?
        int pieceKingTileID = -1;
        Piece.Allegiance enemyAllegiance = Piece.Allegiance.None;
        if (piece.allegiance == Piece.Allegiance.White)
        {
            enemyAllegiance = Piece.Allegiance.Black;
            pieceKingTileID = WHITE_KING_SPAWN_TILE_ID;
        }
        else if (piece.allegiance == Piece.Allegiance.Black)
        {
            enemyAllegiance = Piece.Allegiance.White;
            pieceKingTileID = BLACK_KING_SPAWN_TILE_ID;
        }

        //Clear old valid move points
        DestroyAllValidMovePoints();

        //Each piece type has their own set of valid moves
        if (piece.type == Piece.Type.Pawn)
        {
            //Pawns move/capture one edge away
            List<int> tileNeighborsToCheck = generation.tiles[piece.tileID].adjacentNeighborIDsEdge;
            for (int i = 0; i < tileNeighborsToCheck.Count; i++)
            {
                int tileCheckingID = generation.tiles[tileNeighborsToCheck[i]].id;

                SetBasicMoveTileInteractionsAndGetIfTileOccupied(ref validTilesToMoveTo, piece.allegiance, enemyAllegiance, tileCheckingID);

                ////Pawns only being able to "move forward" was unintuitive
                //float distanceToKingCurrent = (generation.tiles[piece.tileID].centroidAndNormal - generation.tiles[pieceKingTileID].centroidAndNormal).magnitude;
                //float distanceToKingPotential = (generation.tiles[tileCheckingID].centroidAndNormal - generation.tiles[pieceKingTileID].centroidAndNormal).magnitude;
                //float distanceToKingDifference = distanceToKingPotential - distanceToKingCurrent;
                //Debug.Log(distanceToKingDifference);
                //if (distanceToKingDifference >= -0.13f)
                //{
                //    //Pawns can onlu move away from their king's spawn tile
                //    SetBasicMoveTileInteractions(ref validTilesToMoveTo, piece.allegiance, enemyAllegiance, tileCheckingID);
                //}
            }
        }
        else if (piece.type == Piece.Type.King)
        {
            //Kings move/capture one edge away
            List<int> tileNeighborsToCheck = generation.tiles[piece.tileID].adjacentNeighborIDsEdge;
            for (int i = 0; i < tileNeighborsToCheck.Count; i++)
            {
                int tileCheckingID = generation.tiles[tileNeighborsToCheck[i]].id;

                SetBasicMoveTileInteractionsAndGetIfTileOccupied(ref validTilesToMoveTo, piece.allegiance, enemyAllegiance, tileCheckingID);
            }
        }
        else if (piece.type == Piece.Type.Knight)
        {
            //Knights move/capture one side corner away, ignoring obstructions in the way
            List<int> tilesToCheck = generation.tiles[piece.tileID].adjacentNeighborIDsCornerSide;
            for (int i = 0; i < tilesToCheck.Count; i++)
            {
                SetBasicMoveTileInteractionsAndGetIfTileOccupied(ref validTilesToMoveTo, piece.allegiance, enemyAllegiance, generation.tiles[tilesToCheck[i]].id);
            }
        }
        else if (piece.type == Piece.Type.Bishop || piece.type == Piece.Type.Rook || piece.type == Piece.Type.Queen)
        {
            if (piece.type == Piece.Type.Bishop || piece.type == Piece.Type.Queen)
            {
                //Bishops move/capture infinitely from edges to direct corners and vice versa along one direction
                //Initial move - check each "direction" for valid moves
                List<int> tilesToCheck;
                bool previousTileWasEdgeInitial;
                for (int i = 0; i < 2; i++)
                {
                    //First "direction" to start with is edges
                    tilesToCheck = generation.tiles[piece.tileID].adjacentNeighborIDsEdge;
                    previousTileWasEdgeInitial = true;
                    if (i == 1)
                    {
                        //Next pass, immedates will be direct corners
                        tilesToCheck = generation.tiles[piece.tileID].adjacentNeighborIDsCornerDirect;
                        previousTileWasEdgeInitial = false;
                    }

                    for (int j = 0; j < tilesToCheck.Count; j++)
                    {
                        //EACH EDGE TILE AROUND THE PIECE
                        int immediateTileID = generation.tiles[tilesToCheck[j]].id;

                        //Base interactions around immediate edges
                        if (!SetBasicMoveTileInteractionsAndGetIfTileOccupied(ref validTilesToMoveTo, piece.allegiance, enemyAllegiance, immediateTileID))
                        {
                            //Only recur if the immediate tile is unoccupied
                            //Recursion
                            //Setup default variables to be used in recursion
                            int currentTileID = immediateTileID; //start recursion from an immediate edge
                            int previous1TileID = immediateTileID;
                            int previous2TileID = piece.tileID;
                            List<int> tilesToCheckInRecursion;
                            bool previousTileWasEdge = previousTileWasEdgeInitial;

                            int tileDistance = 11;
                            for (int k = 0; k < tileDistance; k++)
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
                                for (int w = 0; w < tilesToCheckInRecursion.Count; w++)
                                {
                                    currentTileID = generation.tiles[tilesToCheckInRecursion[w]].id;

                                    //PreviousTileID2 and currentTileID must share no vertices - ensures we aren't veering off to the side and aren't going back somewhere we just came from
                                    int sharedVertices = generation.GetTileSharedVertices(previous2TileID, currentTileID);

                                    if (sharedVertices == 0)
                                    {
                                        //Valid move tile (as long as not occupied by friendly piece)
                                        if (SetBasicMoveTileInteractionsAndGetIfTileOccupied(ref validTilesToMoveTo, piece.allegiance, enemyAllegiance, currentTileID))
                                        {
                                            //If tile occupied, dead end - stop recurring along this direction
                                            deadEndThereforeBreakRecursionInThisDirection = true;
                                        }

                                        //Break so that we "lock in" the currentTileID so we continue on from that tile instead of continuing from an invalid tile
                                        break;
                                    }

                                    if (w == tilesToCheckInRecursion.Count - 1)
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
            }

            if (piece.type == Piece.Type.Rook || piece.type == Piece.Type.Queen)
            {
                //Rooks move/capture infinitely from edge to edge along one direction
                //Immediate edges
                List<int> tilesImmediateEdges = generation.tiles[piece.tileID].adjacentNeighborIDsEdge;
                for (int i = 0; i < tilesImmediateEdges.Count; i++)
                {
                    //Only continue checking along this direction if tile unoccupied
                    if (!SetBasicMoveTileInteractionsAndGetIfTileOccupied(ref validTilesToMoveTo, piece.allegiance, enemyAllegiance, tilesImmediateEdges[i]))
                    {
                        //Edges of those edges
                        List<int> tilesEdges1Removed = generation.tiles[tilesImmediateEdges[i]].adjacentNeighborIDsEdge;
                        for (int j = 0; j < tilesEdges1Removed.Count; j++)
                        {
                            //Not starting tile
                            if (tilesEdges1Removed[j] != piece.tileID)
                            {
                                //Only continue checking along this direction if tile unoccupied
                                if (!SetBasicMoveTileInteractionsAndGetIfTileOccupied(ref validTilesToMoveTo, piece.allegiance, enemyAllegiance, tilesEdges1Removed[j]))
                                {
                                    //Recursion - edges of the previous tiles edges
                                    //Setup default variables to be used in recursion
                                    List<int> tilesEdgesInRecursion = generation.tiles[tilesEdges1Removed[j]].adjacentNeighborIDsEdge;
                                    int currentTileID = tilesEdgesInRecursion[0]; //gets overwritten anyway, but this will be the first assignment
                                    int previous1TileID = tilesEdges1Removed[j];
                                    int previous2TileID = tilesImmediateEdges[i];
                                    int previous3TileID = piece.tileID;

                                    int tileDistance = 16;
                                    for (int k = 0; k < tileDistance; k++)
                                    {
                                        //Protect against dead ends
                                        bool deadEndThereforeBreakRecursionInThisDirection = false;

                                        for (int w = 0; w < tilesEdgesInRecursion.Count; w++)
                                        {
                                            //Update which tile we are on
                                            currentTileID = tilesEdgesInRecursion[w];

                                            //Shares no vertices with tile 3 previous
                                            int sharedVertices = generation.GetTileSharedVertices(previous3TileID, currentTileID);
                                            //Debug.Log("Tile " + currentTileID + " shares " + sharedVertices + " vertices with piece's original tile");
                                            if (sharedVertices == 0)
                                            {
                                                //Valid move tile (as long as not occupied by friendly piece)
                                                if (SetBasicMoveTileInteractionsAndGetIfTileOccupied(ref validTilesToMoveTo, piece.allegiance, enemyAllegiance, currentTileID))
                                                {
                                                    //If tile occupied, dead end - stop recurring along this direction
                                                    deadEndThereforeBreakRecursionInThisDirection = true;
                                                }

                                                //Break so we "lock in" the current tile as the sole valid one
                                                break;
                                            }
                                        }

                                        if (deadEndThereforeBreakRecursionInThisDirection)
                                        {
                                            break;
                                        }

                                        //Update tile history
                                        previous3TileID = previous2TileID;
                                        previous2TileID = previous1TileID;
                                        previous1TileID = currentTileID;

                                        //Setup which tile to check on the next recursion
                                        tilesEdgesInRecursion = generation.tiles[currentTileID].adjacentNeighborIDsEdge;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        //Return valid moves
        return validTilesToMoveTo;
    }

    private bool SetBasicMoveTileInteractionsAndGetIfTileOccupied(ref List<Move> validTilesToMoveTo, Piece.Allegiance thisAllegiance, Piece.Allegiance enemyAllegiance, int tileID)
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

    public void SpawnPiecesDefault()
    {
        SpawnPiece(Piece.Allegiance.White, Piece.Type.King, WHITE_KING_SPAWN_TILE_ID);
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
        
        SpawnPiece(Piece.Allegiance.Black, Piece.Type.King, BLACK_KING_SPAWN_TILE_ID);
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
        instancePieceScript.pieceController = this;
        instancePieceScript.allegiance = allegiance;
        instancePieceScript.type = type;
        instancePieceScript.SetModel();
        instancePieceScript.SetTile(tileID);
    }
}