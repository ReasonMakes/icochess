using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Generation generation;

    //private float moveSpeed = 0.06f;
    private float mouseSensitivity = 2f;
    private float yawDegrees = 0f;
    private float pitchDegrees = 0f;
    private float cameraDistanceToCenter = 2.5f;

    private float yawDegreesAtStartOfInput = 0f;
    private float pitchDegreesAtStartOfInput = 0f;

    private Transform highlightedTile;
    private int selectedTileID = -1;

    public enum MoveRule
    {
        Forbidden,
        Move,
        Capture
    }

    private void Start()
    {
        PanCamera();
    }

    private void Update()
    {
        //Camera controller
        PanCamera();
        cameraDistanceToCenter = Mathf.Max(1.5f, cameraDistanceToCenter + (Input.mouseScrollDelta.y * -0.1f));
        //MoveCamera();

        //Tile selection
        ManipulateTileHighlightAndSelect();
        ManipulateTileMovePiece();
    }

    private void PanCamera()
    {
        //Rotate camera around world origin
        if (Input.GetKey(KeyCode.Mouse0))
        {
            yawDegrees += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            yawDegrees %= 360f;
            pitchDegrees = Mathf.Clamp(pitchDegrees + (Input.GetAxisRaw("Mouse Y") * mouseSensitivity), -89f, 89f);
        }

        Quaternion rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0);
        transform.position = rotation * Vector3.forward * cameraDistanceToCenter;

        //Always look at world origin
        Quaternion lookRotation = Quaternion.LookRotation(Vector3.zero - transform.position);
        if (lookRotation != Quaternion.identity)
        {
            transform.localRotation = lookRotation;
        }
    }

    private void MoveCamera()
    {
        //Cursor locking/visibility
        if (Input.GetKey(KeyCode.Escape))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        if (Input.GetKey(KeyCode.Mouse0))
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        //Move
        Vector3 moveDirection = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W))            { moveDirection += transform.forward; }
        if (Input.GetKey(KeyCode.A))            { moveDirection += -transform.right; }
        if (Input.GetKey(KeyCode.S))            { moveDirection += -transform.forward; }
        if (Input.GetKey(KeyCode.D))            { moveDirection += transform.right; }
        if (Input.GetKey(KeyCode.Space))        { moveDirection += transform.up; }
        if (Input.GetKey(KeyCode.LeftControl))  { moveDirection += -transform.up; }

        float moveSpeed = 0.05f;
        transform.position += moveDirection.normalized * moveSpeed;
        
        //Look
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yawDegrees += Input.GetAxisRaw("Mouse X") * mouseSensitivity; yawDegrees %= 360f;
            pitchDegrees -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity; pitchDegrees %= 360f;
            transform.localRotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
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
        }
        else
        {
            //Reset highlightedTile
            SetHighlightedTileAndNeighbors(highlightedTile, true);
            highlightedTile = null;
        }

        //SELECT
        if (Input.GetMouseButtonDown(0))
        {
            //Prep to find out if moving the camera or trying to select
            yawDegreesAtStartOfInput = yawDegrees;
            pitchDegreesAtStartOfInput = pitchDegrees;
        }
        if (Input.GetMouseButtonUp(0))
        {
            //Select
            float panThresholdToAllowSelecting = 2f;
            if
            (
                     yawDegrees <= yawDegreesAtStartOfInput + panThresholdToAllowSelecting
                && yawDegrees >= yawDegreesAtStartOfInput - panThresholdToAllowSelecting
                && pitchDegrees <= pitchDegreesAtStartOfInput + panThresholdToAllowSelecting
                && pitchDegrees >= pitchDegreesAtStartOfInput - panThresholdToAllowSelecting
            )
            {
                if (highlightedTile != null)
                {
                    //Overwrite/create new selection
                    if (selectedTileID != -1)
                    {
                        //Reset old selection
                        generation.tiles[selectedTileID].instanceTileGameObject.GetComponent<MeshRenderer>().material.color = generation.tiles[selectedTileID].color;
                    }
                    if (selectedTileID == highlightedTile.GetComponent<TileInstance>().id)
                    {
                        selectedTileID = -1;
                    }
                    else
                    {
                        //If selecting the tile already selected, deselect
                        selectedTileID = highlightedTile.GetComponent<TileInstance>().id;
                        generation.tiles[selectedTileID].instanceTileGameObject.GetComponent<MeshRenderer>().material.color = Color.yellow;
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

    private void ManipulateTileMovePiece()
    {
        if (selectedTileID != -1)
        {
            if
            (
                Input.GetMouseButtonUp(1)
                && highlightedTile != null
                && selectedTileID != highlightedTile.GetComponent<TileInstance>().id
                && generation.tiles[selectedTileID].instancePieceGameObject != null
            )
            {
                MoveRule moveCheck = IsValidMove(
                    generation.tiles[selectedTileID].instancePieceGameObject.GetComponent<Piece>(),
                    highlightedTile.GetComponent<TileInstance>().id,
                    generation.tiles[selectedTileID].instancePieceGameObject.GetComponent<Piece>().tileID
                );
                if (moveCheck != MoveRule.Forbidden)
                {
                    //Capture piece
                    if (moveCheck == MoveRule.Capture)
                    {
                        Destroy(generation.tiles[highlightedTile.GetComponent<TileInstance>().id].instancePieceGameObject);
                    }

                    //Move there
                    generation.tiles[selectedTileID].instancePieceGameObject.GetComponent<Piece>().SetTile(highlightedTile.GetComponent<TileInstance>().id);

                    //Deselect
                    DeselectTile();
                }
                else
                {
                    Debug.Log("Invalid move!");
                }
            }
        }
    }

    private void DeselectTile()
    {
        generation.tiles[selectedTileID].instanceTileGameObject.GetComponent<MeshRenderer>().material.color = generation.tiles[selectedTileID].color;
        selectedTileID = -1;
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

    private MoveRule IsValidMove(Piece piece, int tileIDToMoveTo, int tileIDMovingFrom)
    {
        if (generation.tiles[tileIDToMoveTo].instancePieceGameObject == null)
        {
            //MOVING - Empty tile
            if (piece.type == Piece.Type.Pawn)
            {
                //Pawns move one edge away
                if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.Edge)
                {
                    return MoveRule.Move;
                }
                else
                {
                    return MoveRule.Forbidden;
                }
            }
            else if (piece.type == Piece.Type.Knight)
            {
                //Knights move one side corner away
                if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.CornerSide)
                {
                    return MoveRule.Move;
                }
                else
                {
                    return MoveRule.Forbidden;
                }
            }
            else if (piece.type == Piece.Type.Bishop)
            {
                //Bishops move via direct corners
                if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.CornerDirect)
                {
                    return MoveRule.Move;
                }
                else
                {
                    return MoveRule.Forbidden;
                }
            }
            else if (piece.type == Piece.Type.Rook)
            {
                //Rooks move via edges
                if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.Edge)
                {
                    return MoveRule.Move;
                }
                else
                {
                    return MoveRule.Forbidden;
                }
            }
            else if (piece.type == Piece.Type.Queen)
            {
                //Queens move via edges or direct corners
                Generation.TileNeighborType neighborType = generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom);
                if (neighborType == Generation.TileNeighborType.Edge || neighborType == Generation.TileNeighborType.CornerDirect)
                {
                    return MoveRule.Move;
                }
                else
                {
                    return MoveRule.Forbidden;
                }
            }
            else if (piece.type == Piece.Type.King)
            {
                //Kings move one edge away
                if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.Edge)
                {
                    return MoveRule.Move;
                }
                else
                {
                    return MoveRule.Forbidden;
                }
            }
            else
            {
                Debug.LogError("Uncaught move error");
                return MoveRule.Forbidden;
            }
        }
        else
        {
            //CAPTURING - Occupied tile
            if (generation.tiles[tileIDToMoveTo].instancePieceGameObject.GetComponent<Piece>().allegiance == piece.allegiance)
            {
                //Friendly piece
                //Debug.Log("Can't move ontop of or capture own pieces");
                return MoveRule.Forbidden;
            }
            else
            {
                //Hostile piece
                if (piece.type == Piece.Type.Pawn)
                {
                    //Pawns attack one edge away
                    if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.Edge)
                    {
                        return MoveRule.Capture;
                    }
                    else
                    {
                        return MoveRule.Forbidden;
                    }
                }
                else if (piece.type == Piece.Type.Knight)
                {
                    //Knights attack one side corner away
                    if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.CornerSide)
                    {
                        return MoveRule.Capture;
                    }
                    else
                    {
                        return MoveRule.Forbidden;
                    }
                }
                else if (piece.type == Piece.Type.Bishop)
                {
                    //Bishops attack via direct corners
                    if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.CornerDirect)
                    {
                        return MoveRule.Capture;
                    }
                    else
                    {
                        return MoveRule.Forbidden;
                    }
                }
                else if (piece.type == Piece.Type.Rook)
                {
                    //Rooks attack via edges
                    if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.Edge)
                    {
                        return MoveRule.Capture;
                    }
                    else
                    {
                        return MoveRule.Forbidden;
                    }
                }
                else if (piece.type == Piece.Type.Queen)
                {
                    //Queens attack via edges or direct corners
                    Generation.TileNeighborType neighborType = generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom);
                    if (neighborType == Generation.TileNeighborType.Edge || neighborType == Generation.TileNeighborType.CornerDirect)
                    {
                        return MoveRule.Capture;
                    }
                    else
                    {
                        return MoveRule.Forbidden;
                    }
                }
                else if (piece.type == Piece.Type.King)
                {
                    //Kings attack one edge away
                    if (generation.IsTileNeighbor(tileIDToMoveTo, tileIDMovingFrom) == Generation.TileNeighborType.Edge)
                    {
                        return MoveRule.Capture;
                    }
                    else
                    {
                        return MoveRule.Forbidden;
                    }
                }
                else
                {
                    Debug.LogError("Uncaught move error");
                    return MoveRule.Forbidden;
                }
            }
        }
    }
}