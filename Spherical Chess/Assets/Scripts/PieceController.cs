using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceController : MonoBehaviour
{
    public Control control;
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

    public enum Check
    {
        None,
        White,
        Black
    }
    [System.NonSerialized] public Check checkState = Check.None;

    public struct Move
    {
        public enum MoveType
        {
            Forbidden,
            Reposition,
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

    public List<Move> GetValidTilesToMoveTo(Piece piece, PieceData pieceData, bool spawnPoints, bool ignorePuttingSelfInCheck)
    {
        //List of valid moves we will return
        List<Move> validTilesToMoveTo = new List<Move> { };

        //What are this piece's friends and enemies?
        Piece.Allegiance enemyAllegiance = Piece.Allegiance.None;
        if (pieceData.allegiance == Piece.Allegiance.White)
        {
            enemyAllegiance = Piece.Allegiance.Black;
        }
        else if (pieceData.allegiance == Piece.Allegiance.Black)
        {
            enemyAllegiance = Piece.Allegiance.White;
        }

        //Clear old valid move points
        //DestroyAllValidMovePoints();

        //Each piece type has their own set of valid moves
        if (pieceData.type == Piece.Type.Pawn)
        {
            //Pawns move/capture one edge away. Can move twice the first time they move.
            List<int> tileNeighborsToCheck = generation.tiles[pieceData.tileID].adjacentNeighborIDsEdge;
            for (int i = 0; i < tileNeighborsToCheck.Count; i++)
            {
                int tileCheckingID = generation.tiles[tileNeighborsToCheck[i]].id;

                bool occupiedTile = SetBasicMoveTileInteractionsAndGetIfTileOccupiedAndSpawnPoints(
                    ref validTilesToMoveTo,
                    piece,
                    pieceData,
                    enemyAllegiance,
                    tileCheckingID,
                    spawnPoints,
                    ignorePuttingSelfInCheck
                );

                //Can move twice the first time they move
                if (!occupiedTile && !pieceData.hasMoved)
                {
                    for (int j = 0; j < generation.tiles[tileCheckingID].adjacentNeighborIDsEdge.Count; j++)
                    {
                        int tileCheckingForDoubleMoveID = generation.tiles[tileCheckingID].adjacentNeighborIDsEdge[j];
                        SetBasicMoveTileInteractionsAndGetIfTileOccupiedAndSpawnPoints(
                            ref validTilesToMoveTo,
                            piece,
                            pieceData,
                            enemyAllegiance,
                            tileCheckingForDoubleMoveID,
                            spawnPoints,
                            ignorePuttingSelfInCheck
                        );
                    }
                }

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
        else if (pieceData.type == Piece.Type.King)
        {
            //Kings move/capture one edge away
            List<int> tileNeighborsToCheck = generation.tiles[pieceData.tileID].adjacentNeighborIDsEdge;
            for (int i = 0; i < tileNeighborsToCheck.Count; i++)
            {
                int tileCheckingID = generation.tiles[tileNeighborsToCheck[i]].id;

                SetBasicMoveTileInteractionsAndGetIfTileOccupiedAndSpawnPoints(
                    ref validTilesToMoveTo,
                    piece,
                    pieceData,
                    enemyAllegiance,
                    tileCheckingID,
                    spawnPoints,
                    ignorePuttingSelfInCheck
                );
            }
        }
        else if (pieceData.type == Piece.Type.Knight)
        {
            //Knights move/capture one side corner away, ignoring obstructions in the way
            List<int> tilesToCheck = generation.tiles[pieceData.tileID].adjacentNeighborIDsCornerSide;
            for (int i = 0; i < tilesToCheck.Count; i++)
            {
                SetBasicMoveTileInteractionsAndGetIfTileOccupiedAndSpawnPoints(
                    ref validTilesToMoveTo,
                    piece,
                    pieceData,
                    enemyAllegiance,
                    generation.tiles[tilesToCheck[i]].id,
                    spawnPoints,
                    ignorePuttingSelfInCheck
                );
            }
        }
        else if (pieceData.type == Piece.Type.Bishop || pieceData.type == Piece.Type.Rook || pieceData.type == Piece.Type.Queen)
        {
            if (pieceData.type == Piece.Type.Bishop || pieceData.type == Piece.Type.Queen)
            {
                //Bishops move/capture infinitely from edges to direct corners and vice versa along one direction
                //Initial move - check each "direction" for valid moves
                List<int> tilesToCheck;
                bool previousTileWasEdgeInitial;
                for (int i = 0; i < 2; i++)
                {
                    //First "direction" to start with is edges
                    tilesToCheck = generation.tiles[pieceData.tileID].adjacentNeighborIDsEdge;
                    previousTileWasEdgeInitial = true;
                    if (i == 1)
                    {
                        //Next pass, immedates will be direct corners
                        tilesToCheck = generation.tiles[pieceData.tileID].adjacentNeighborIDsCornerDirect;
                        previousTileWasEdgeInitial = false;
                    }

                    for (int j = 0; j < tilesToCheck.Count; j++)
                    {
                        //EACH EDGE TILE AROUND THE PIECE
                        int immediateTileID = generation.tiles[tilesToCheck[j]].id;

                        //Base interactions around immediate edges
                        if
                        (
                            !SetBasicMoveTileInteractionsAndGetIfTileOccupiedAndSpawnPoints(
                                ref validTilesToMoveTo,
                                piece,
                                pieceData,
                                enemyAllegiance,
                                immediateTileID,
                                spawnPoints,
                                ignorePuttingSelfInCheck
                            )
                        )
                        {
                            //Only recur if the immediate tile is unoccupied
                            //Recursion
                            //Setup default variables to be used in recursion
                            int currentTileID = immediateTileID; //start recursion from an immediate edge
                            int previous1TileID = immediateTileID;
                            int previous2TileID = pieceData.tileID;
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
                                        if
                                        (
                                            SetBasicMoveTileInteractionsAndGetIfTileOccupiedAndSpawnPoints(
                                                ref validTilesToMoveTo,
                                                piece,
                                                pieceData,
                                                enemyAllegiance,
                                                currentTileID,
                                                spawnPoints,
                                                ignorePuttingSelfInCheck
                                            )
                                        )
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

            if (pieceData.type == Piece.Type.Rook || pieceData.type == Piece.Type.Queen)
            {
                //Rooks move/capture infinitely from edge to edge along one direction
                //Immediate edges
                List<int> tilesImmediateEdges = generation.tiles[pieceData.tileID].adjacentNeighborIDsEdge;
                for (int i = 0; i < tilesImmediateEdges.Count; i++)
                {
                    //Only continue checking along this direction if tile unoccupied
                    if (
                        !SetBasicMoveTileInteractionsAndGetIfTileOccupiedAndSpawnPoints(
                            ref validTilesToMoveTo,
                            piece,
                            pieceData,
                            enemyAllegiance,
                            tilesImmediateEdges[i],
                            spawnPoints,
                            ignorePuttingSelfInCheck
                        )
                    )
                    {
                        //Edges of those edges
                        List<int> tilesEdges1Removed = generation.tiles[tilesImmediateEdges[i]].adjacentNeighborIDsEdge;
                        for (int j = 0; j < tilesEdges1Removed.Count; j++)
                        {
                            //Not starting tile
                            if (tilesEdges1Removed[j] != pieceData.tileID)
                            {
                                //Only continue checking along this direction if tile unoccupied
                                if (
                                    !SetBasicMoveTileInteractionsAndGetIfTileOccupiedAndSpawnPoints(
                                        ref validTilesToMoveTo,
                                        piece,
                                        pieceData,
                                        enemyAllegiance,
                                        tilesEdges1Removed[j],
                                        spawnPoints,
                                        ignorePuttingSelfInCheck
                                    )
                                )
                                {
                                    //Recursion - edges of the previous tiles edges
                                    //Setup default variables to be used in recursion
                                    List<int> tilesEdgesInRecursion = generation.tiles[tilesEdges1Removed[j]].adjacentNeighborIDsEdge;
                                    int currentTileID = tilesEdgesInRecursion[0]; //gets overwritten anyway, but this will be the first assignment
                                    int previous1TileID = tilesEdges1Removed[j];
                                    int previous2TileID = tilesImmediateEdges[i];
                                    int previous3TileID = pieceData.tileID;

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
                                                if (
                                                    SetBasicMoveTileInteractionsAndGetIfTileOccupiedAndSpawnPoints(
                                                        ref validTilesToMoveTo,
                                                        piece,
                                                        pieceData,
                                                        enemyAllegiance,
                                                        currentTileID,
                                                        spawnPoints,
                                                        ignorePuttingSelfInCheck
                                                    )
                                                )
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

    //Returns whether tile is occupied
    //Adds this tile as a valid move for the selected piece if it is unoccupied or has an enemy piece on it
    //Spawns valid move point indicators
    private bool SetBasicMoveTileInteractionsAndGetIfTileOccupiedAndSpawnPoints
    (
        ref List<Move> validTilesToMoveTo,
        Piece piece, PieceData pieceData, Piece.Allegiance enemyAllegiance,
        int tileID,
        bool spawnPoints, bool ignorePuttingSelfInCheck
    )
    {
        //Basic tile interaction:
        //Any tile that is within range of the piece can be moved to as long as it is not occupied by a friendly piece.
        //If an enemy piece occupies it, the enemy piece will be capture and this piece will then occupy the tile the enemy used to.
        //Move points will be spawned on tiles that are valid moves so that the player can see their options. (These are cleared in other scripts.)

        bool isTileOccupied = false;

        Piece.Allegiance pieceAllegienceOnTile = GetTilePieceAllegiance(tileID);
        if (pieceAllegienceOnTile == enemyAllegiance)
        {
            //Valid move - capture
            SetMoveTileInteractions(ref validTilesToMoveTo, Move.MoveType.Capture, ignorePuttingSelfInCheck, piece, pieceData, tileID);

            //Spawn valid move UI point
            if (spawnPoints)
            {
                SpawnValidMovePoint(tileID, true);
            }

            //Tile is occupied by an enemy
            isTileOccupied = true;
        }
        else if (pieceAllegienceOnTile == Piece.Allegiance.None)
        {
            //Valid move - reposition
            SetMoveTileInteractions(ref validTilesToMoveTo, Move.MoveType.Reposition, ignorePuttingSelfInCheck, piece, pieceData, tileID);

            //Spawn valid move UI point
            if (spawnPoints)
            {
                SpawnValidMovePoint(tileID, false);
            }

            //Tile is unoccupied
        }
        else if (pieceAllegienceOnTile == pieceData.allegiance)
        {
            //Not a valid move

            //Tile is occupied by a friend
            isTileOccupied = true;
        }

        return isTileOccupied;
    }

    private void SetMoveTileInteractions(
        ref List<Move> validTilesToMoveTo,
        Move.MoveType moveType,
        bool ignorePuttingSelfInCheck,
        Piece piece, PieceData pieceData,
        int tileID
    )
    {
        if (ignorePuttingSelfInCheck)
        {
            validTilesToMoveTo.Add(new Move(Move.MoveType.Capture, tileID));
        }
        else
        {
            //Only add this move as a valid move if there are no checks against themself in the hypothetical position
            PieceData hypotheticalPieceData = new PieceData();
            hypotheticalPieceData.tileID = tileID;

            if (
                !CheckForChecksAgainst(
                    pieceData.allegiance,
                    piece.transform.GetSiblingIndex(),
                    hypotheticalPieceData
                )
            )
            {
                validTilesToMoveTo.Add(new Move(moveType, tileID));
            }
        }
    }

    private void SpawnValidMovePoint(int tileID, bool isCaptureTile)
    {
        Debug.Log("Move point spawned!");

        //Spawn the indicator
        GameObject instanceValidMovePoint = Instantiate(
            validMovePointPrefab,
            generation.tiles[tileID].centroidAndNormal * 1.006f, //1.003f,
            Quaternion.identity
        );

        //Capture indicators are larger than ove indicators so they're visible underneath pieces
        if (isCaptureTile)
        {
            instanceValidMovePoint.transform.localScale = Vector3.one * 0.115f;
        }

        //Place in hierarchy
        instanceValidMovePoint.transform.parent = generation.points.transform;

        //Rotate to point away from the board
        instanceValidMovePoint.transform.rotation = Quaternion.LookRotation(generation.tiles[tileID].centroidAndNormal);
    }

    public void DestroyAllValidMovePoints()
    {
        Debug.Log("Destroyed all valid move points!");

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
            return generation.tiles[tileID].instancePieceGameObject.GetComponent<Piece>().pieceData.type;
        }

        //Unoccupied by default
        return Piece.Type.None;
    }

    public Piece.Allegiance GetTilePieceAllegiance(int tileID)
    {
        if (generation.tiles[tileID].instancePieceGameObject != null)
        {
            //Occupied
            return generation.tiles[tileID].instancePieceGameObject.GetComponent<Piece>().pieceData.allegiance;
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
        instancePawn.name = "" + allegiance + " " + type;

        Piece instancePieceScript = instancePawn.GetComponent<Piece>();
        instancePieceScript.generation = generation;
        instancePieceScript.pieceController = this;
        instancePieceScript.pieceData.allegiance = allegiance;
        instancePieceScript.pieceData.type = type;
        instancePieceScript.SetModel();
        instancePieceScript.SetTile(tileID);
    }

    public bool CheckForChecksAgainst(Piece.Allegiance defendingTeam, int hypotheticalMovedPieceChildIndex, PieceData hypotheticalPieceData)
    {
        //Checked at the end of a turn, BEFORE whose turn it is actually changes
        //teamToCheckChecks will be the team whose turn is just ending now
        //defendingTeam is the king's team
        for (int i = 0; i < transform.childCount; i++)
        {
            //Every piece on the "board"

            //Get piece data
            Piece piece = null;
            PieceData pieceData;
            if (i == hypotheticalMovedPieceChildIndex)
            {
                //We can leave piece null here as we won't need the reference anyway when ignoring putting self in check
                pieceData = hypotheticalPieceData;
            }
            else
            {
                piece = transform.GetChild(i).GetComponent<Piece>();
                pieceData = piece.pieceData;
            }
            
            //Make sure this piece is opposed to the defending team
            if (pieceData.allegiance != defendingTeam)
            {
                //All pieces that are enemies to the defendingTeam

                //See if any moves could capture the king if neglected
                //Avoid infinite loop: don't check whether the enemy capturing your king would put them in check, because it wouldn't matter at that point
                List<Move> validMoves = GetValidTilesToMoveTo(piece, pieceData, false, true);
                for (int j = 0; j < validMoves.Count; j++)
                {
                    //All valid moves for this enemy piece
                    if (GetTilePieceType(validMoves[j].tileID) == Piece.Type.King)
                    {
                        //This move goes to a king piece
                        if (GetTilePieceAllegiance(validMoves[j].tileID) == defendingTeam)
                        {
                            //This move goes to the defendingTeam's king piece
                            //In check!

                            //Check source
                            control.infoTopLeft.text += "\n" + piece.transform.name + " on " + generation.tiles[piece.pieceData.tileID].humanReadableID;

                            //Return result
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }
}