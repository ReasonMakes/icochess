using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Control control;
    public Generation generation;
    public CameraController cameraController;
    public PieceController pieceController;

    //private float yawDegreesAtStartOfInput = 0f;
    //private float pitchDegreesAtStartOfInput = 0f;
    private Vector3 cameraPositionAtStartOfInput = Vector3.zero;

    [System.NonSerialized] public Transform highlightedTile;
    private int selectedTileID = -1;

    [System.NonSerialized] public Piece.Allegiance teamWhoseTurnItIs = Piece.Allegiance.White;
    List<PieceController.Move> validMoves = new List<PieceController.Move> { };

    private bool showTileNamesOnTiles = false;

    private void Update()
    {
        //Cheats
        //teamWhoseTurnItIs = Piece.Allegiance.White;
        //control.UpdateBottomText();
        //control.UpdateTopRightText();

        //Tile selection
        ManipulateTileHighlightAndSelect();
        ManipulateTileMovePiece();

        //Show/hide tile names
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (showTileNamesOnTiles)
            {
                //Destroy all names
                for (int i = 0; i < generation.names.transform.childCount; i++)
                {
                    Destroy(generation.names.transform.GetChild(i).gameObject);
                }
            }
            else
            {
                //Spawn names
                for (int i = 0; i < generation.tiles.Count; i++)
                {
                    GameObject instanceName = Instantiate(
                        generation.namePrefab,
                        generation.tiles[i].centroidAndNormal * 1.006f, //1.003f,
                        Quaternion.identity
                    );
                    instanceName.transform.parent = generation.names.transform;
                    instanceName.transform.rotation = Quaternion.LookRotation(-generation.tiles[i].centroidAndNormal);
                    instanceName.GetComponent<TextMeshPro>().text = generation.tiles[i].humanReadableID;
                }
            }

            showTileNamesOnTiles = !showTileNamesOnTiles;
        }
    }

    private void ManipulateTileHighlightAndSelect()
    {
        //HIGHLIGHT
        bool hitAMesh = true;
        RaycastHit hit;
        if (!Physics.Raycast(GetComponent<Camera>().ScreenPointToRay(Input.mousePosition), out hit))
        {
            hitAMesh = false;
        }

        MeshCollider meshCollider = hit.collider as MeshCollider;
        if (meshCollider == null || meshCollider.sharedMesh == null)
        {
            hitAMesh = false;
        }

        if (hitAMesh)
        {
            //Reset previous highlighted tile
            SetHighlightedTileAndNeighbors(highlightedTile, true);
            //New highlighted tile
            highlightedTile = hit.transform;

            //Self
            SetHighlightedTileAndNeighbors(highlightedTile, false);

            //Update tile related UI text
            control.UpdateTileRelatedText();
        }
        else
        {
            //Reset highlightedTile
            SetHighlightedTileAndNeighbors(highlightedTile, true);
            highlightedTile = null;

            //Update tile related UI text
            control.ClearTileRelatedText();
        }

        //SELECT
        if (Input.GetMouseButtonDown(0))
        {
            //Prep to find out if moving the camera or trying to select
            cameraPositionAtStartOfInput = cameraController.transform.position;
            //yawDegreesAtStartOfInput = cameraController.yawDegrees;
            //pitchDegreesAtStartOfInput = cameraController.pitchDegrees;
        }
        if (Input.GetMouseButtonUp(0))
        {
            //Select
            float panThresholdToAllowSelecting = 0.075f; //2f;
            float cameraMoveDistance = (cameraController.transform.position - cameraPositionAtStartOfInput).magnitude;
            if
            (
                cameraMoveDistance <= panThresholdToAllowSelecting
                //     cameraController.yawDegrees <= yawDegreesAtStartOfInput   + panThresholdToAllowSelecting
                //&& cameraController.yawDegrees   >= yawDegreesAtStartOfInput   - panThresholdToAllowSelecting
                //&& cameraController.pitchDegrees <= pitchDegreesAtStartOfInput + panThresholdToAllowSelecting
                //&& cameraController.pitchDegrees >= pitchDegreesAtStartOfInput - panThresholdToAllowSelecting
            )
            {
                if (highlightedTile != null)
                {
                    //Overwrite/create new selection
                    if (selectedTileID != -1)
                    {
                        //Reset old selection
                        generation.tiles[selectedTileID].instanceTileGameObject.GetComponent<MeshRenderer>().material.color = generation.tiles[selectedTileID].color;
                        pieceController.DestroyAllValidMovePoints();
                    }

                    if (selectedTileID == highlightedTile.GetComponent<TileInstance>().id)
                    {
                        //If selecting the tile already selected, deselect
                        selectedTileID = -1;
                        pieceController.DestroyAllValidMovePoints();
                    }
                    else
                    {
                        //*****Select*****
                        selectedTileID = highlightedTile.GetComponent<TileInstance>().id;
                        generation.tiles[selectedTileID].instanceTileGameObject.GetComponent<MeshRenderer>().material.color = Color.yellow;

                        //Get valid moves for the piece on that tile (this will return None if the tile is unoccopied)
                        if (pieceController.GetTilePieceAllegiance(selectedTileID) == teamWhoseTurnItIs)
                        {
                            validMoves.Clear();
                            validMoves = pieceController.GetValidTilesToMoveTo(
                                generation.tiles[selectedTileID].instancePieceGameObject.GetComponent<Piece>(),
                                true
                            );
                        }
                    }
                }

                //Deselect
                if
                (
                    highlightedTile == null
                    && selectedTileID != -1
                )
                {
                    DeselectTile();
                }
            }
        }
    }

    private void DeselectTile()
    {
        generation.tiles[selectedTileID].instanceTileGameObject.GetComponent<MeshRenderer>().material.color = generation.tiles[selectedTileID].color;
        selectedTileID = -1;
        pieceController.DestroyAllValidMovePoints();
    }

    private void SetHighlightedTileAndNeighbors(Transform highlightedTile, bool reset)
    {
        if (highlightedTile != null)
        {
            //Self
            int highlightedID = highlightedTile.GetComponent<TileInstance>().id;
            Color tileColor = Color.white;
            if (reset)
            {
                tileColor = generation.tiles[highlightedID].color;
            }
            if (highlightedID != selectedTileID)
            {
                highlightedTile.GetComponent<MeshRenderer>().material.color = tileColor;
            }

            //Edges
            for (int i = 0; i < generation.tiles[highlightedID].adjacentNeighborIDsEdge.Count; i++)
            {
                int indexNeighborEdgeID = generation.tiles[highlightedID].adjacentNeighborIDsEdge[i];

                tileColor = new Color(
                    Mathf.Clamp01((generation.tiles[indexNeighborEdgeID].color.r + Color.blue.r) / 2f),
                    Mathf.Clamp01((generation.tiles[indexNeighborEdgeID].color.g + Color.blue.g) / 2f),
                    Mathf.Clamp01((generation.tiles[indexNeighborEdgeID].color.b + Color.blue.b) / 2f),
                    1f
                );
                if (reset)
                {
                    tileColor = generation.tiles[indexNeighborEdgeID].color;
                }
                if (indexNeighborEdgeID != selectedTileID)
                {
                    generation.tiles[indexNeighborEdgeID].instanceTileGameObject.GetComponent<MeshRenderer>().material.color = tileColor;
                }
            }

            //Side corners
            for (int i = 0; i < generation.tiles[highlightedID].adjacentNeighborIDsCornerSide.Count; i++)
            {
                int sideID = generation.tiles[highlightedID].adjacentNeighborIDsCornerSide[i];

                tileColor = new Color(
                    Mathf.Clamp01((generation.tiles[sideID].color.r + Color.red.r) / 2f),
                    Mathf.Clamp01((generation.tiles[sideID].color.g + Color.red.g) / 2f),
                    Mathf.Clamp01((generation.tiles[sideID].color.b + Color.red.b) / 2f),
                    1f
                );
                if (reset)
                {
                    tileColor = generation.tiles[sideID].color;
                }
                if (sideID != selectedTileID)
                {
                    generation.tiles[sideID].instanceTileGameObject.GetComponent<MeshRenderer>().material.color = tileColor;
                }
            }

            //Direct corners
            for (int i = 0; i < generation.tiles[highlightedID].adjacentNeighborIDsCornerDirect.Count; i++)
            {
                int directID = generation.tiles[highlightedID].adjacentNeighborIDsCornerDirect[i];

                tileColor = new Color(
                    Mathf.Clamp01((generation.tiles[directID].color.r + Color.green.r) / 2f),
                    Mathf.Clamp01((generation.tiles[directID].color.g + Color.green.g) / 2f),
                    Mathf.Clamp01((generation.tiles[directID].color.b + Color.green.b) / 2f),
                    1f
                );
                if (reset)
                {
                    tileColor = generation.tiles[directID].color;
                }
                if (directID != selectedTileID)
                {
                    generation.tiles[directID].instanceTileGameObject.GetComponent<MeshRenderer>().material.color = tileColor;
                }
            }
        }
    }

    private void ManipulateTileMovePiece()
    {
        //Releasing right click, and no null references
        if
        (
            Input.GetMouseButtonUp(1)
            && selectedTileID != -1
            && highlightedTile != null
        )
        {
            int highlightedTileID = highlightedTile.GetComponent<TileInstance>().id;

            //Not trying to move in place or move nothing
            if
            (
                selectedTileID != highlightedTileID
                && generation.tiles[selectedTileID].instancePieceGameObject != null
            )
            {
                //Get valid move index for this tile
                int validMoveIndex = GetValidMoveIndexFromTileID(highlightedTileID);

                //Is a valid move?
                if (validMoveIndex != -1)
                {
                    //Piece ref
                    Piece piece = generation.tiles[selectedTileID].instancePieceGameObject.GetComponent<Piece>();

                    //Capture piece?
                    if (validMoves[validMoveIndex].MOVE_TYPE == PieceController.Move.MoveType.Capture)
                    {
                        //Destroy enemy piece
                        Destroy(generation.tiles[highlightedTileID].instancePieceGameObject);
                    }

                    //Move there
                    piece.SetTile(highlightedTileID);

                    //Has moved
                    piece.hasMoved = true;

                    //Deselect, clear valid moves, and end turn
                    DeselectTile();
                    validMoves.Clear();
                    EndTurn();
                }
                else
                {
                    Debug.Log("Invalid move!");
                }
            }
        }
    }

    private int GetValidMoveIndexFromTileID(int tileID)
    {
        for (int i = 0; i < validMoves.Count; i++)
        {
            if (validMoves[i].tileID == tileID)
            {
                return i;
            }
        }

        //Return -1 to indicate the tile is not a valid move
        return -1;
    }

    private void EndTurn()
    {
        //Check for checks, then switch whose turn it is
        control.infoTopLeft.text = "Checks:";
        control.infoTopLeft.gameObject.SetActive(false);
        pieceController.checkState = PieceController.Check.None;
        if (teamWhoseTurnItIs == Piece.Allegiance.White)
        {
            if (pieceController.CheckForChecksAgainst(Piece.Allegiance.Black))
            {
                pieceController.checkState = PieceController.Check.Black;
                control.infoTopLeft.gameObject.SetActive(true);
            }
            teamWhoseTurnItIs = Piece.Allegiance.Black;
        }
        else if (teamWhoseTurnItIs == Piece.Allegiance.Black)
        {
            if (pieceController.CheckForChecksAgainst(Piece.Allegiance.White))
            {
                pieceController.checkState = PieceController.Check.White;
                control.infoTopLeft.gameObject.SetActive(true);
            }
            teamWhoseTurnItIs = Piece.Allegiance.White;
        }

        //Update HUD to show on-screen info like whose turn it is
        control.UpdateBottomText();
    }
}