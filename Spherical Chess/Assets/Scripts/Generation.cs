using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generation : MonoBehaviour
{
    //Constants
    //private readonly float TAU = 6.2831853071f;
    private static readonly float PHI = 1.6180339887f; //(1.0f + Mathf.Sqrt(5.0f)) / 2.0f

    //References
    public PieceController pieceController;
    public GameObject pointBillboardPrefab;
    public GameObject points;
    public GameObject prefabTile;
    public GameObject names;
    public GameObject namePrefab;

    //Procedural mesh
    public Material materialLit;

    public struct Triangle
    {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;

        public Triangle
        (
            int vertexIndexA,
            int vertexIndexB,
            int vertexIndexC
        )
        {
            this.vertexIndexA = vertexIndexA;
            this.vertexIndexB = vertexIndexB;
            this.vertexIndexC = vertexIndexC;
        }
    }

    private List<Vector3> vertices;
    private List<Triangle> triangles;

    [System.NonSerialized] public List<Tile> tiles;

    public enum TileNeighborType
    {
        Edge,
        CornerDirect,
        CornerSide,
        Stranger
    }

    private void Start()
    {
        //Procedurally generate "board"
        tiles = new List<Tile>();
        triangles = new List<Triangle>();
        vertices = new List<Vector3>();
        InitShapeIcosahedron();
        Subdivide(1);
        AssembleMeshAndGetTileData();
        GenerateAllTiles();

        //Set up material - we do this in this script here because this needs to happen after the tiles are generated
        pieceController.SpawnPiecesDefault();
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.F))
        //{
        //    TilesDebugPrint();
        //}
    }

    private void InitShapeIcosahedron()
    {
        //Core icosahedron geometry (smallest icosahedron)
        //12 vertices
        vertices.Add(new Vector3(-1,    PHI,  0).normalized);
        vertices.Add(new Vector3( 1,    PHI,  0).normalized);
        vertices.Add(new Vector3(-1,   -PHI,  0).normalized);

        vertices.Add(new Vector3( 1,   -PHI,  0).normalized);
        vertices.Add(new Vector3( 0,   -1,    PHI).normalized);
        vertices.Add(new Vector3( 0,    1,    PHI).normalized);

        vertices.Add(new Vector3( 0,   -1,   -PHI).normalized);
        vertices.Add(new Vector3( 0,    1,   -PHI).normalized);
        vertices.Add(new Vector3( PHI,  0,   -1).normalized);

        vertices.Add(new Vector3( PHI,  0,    1).normalized);
        vertices.Add(new Vector3(-PHI,  0,   -1).normalized);
        vertices.Add(new Vector3(-PHI,  0,    1).normalized);

        //20 faces (triangles)
        triangles.Add(new Triangle(0, 11, 5));
        triangles.Add(new Triangle(0, 5, 1));
        triangles.Add(new Triangle(0, 1, 7));
        triangles.Add(new Triangle(0, 7, 10));
        triangles.Add(new Triangle(0, 10, 11));

        triangles.Add(new Triangle(1, 5, 9));
        triangles.Add(new Triangle(5, 11, 4));
        triangles.Add(new Triangle(11, 10, 2));
        triangles.Add(new Triangle(10, 7, 6));
        triangles.Add(new Triangle(7, 1, 8));

        triangles.Add(new Triangle(3, 9, 4));
        triangles.Add(new Triangle(3, 4, 2));
        triangles.Add(new Triangle(3, 2, 6));
        triangles.Add(new Triangle(3, 6, 8));
        triangles.Add(new Triangle(3, 8, 9));

        triangles.Add(new Triangle(4, 9, 5));
        triangles.Add(new Triangle(2, 4, 11));
        triangles.Add(new Triangle(6, 2, 10));
        triangles.Add(new Triangle(8, 6, 7));
        triangles.Add(new Triangle(9, 8, 1));
    }

    private void Subdivide(int recursions)
    {
        Dictionary<int, int> midPointCache = new Dictionary<int, int>();

        for (int i = 0; i < recursions; i++)
        {
            List<Triangle> trianglesOverwrite = new List<Triangle>();
            foreach (Triangle triangle in triangles)
            {
                int a = triangle.vertexIndexA;
                int b = triangle.vertexIndexB;
                int c = triangle.vertexIndexC;

                // Use GetMidPointIndex to either create a
                // new vertex between two old vertices, or
                // find the one that was already created.
                int ab = GetMidPointIndex(midPointCache, a, b);
                int bc = GetMidPointIndex(midPointCache, b, c);
                int ca = GetMidPointIndex(midPointCache, c, a);

                // Create the four new polygons using our original
                // three vertices, and the three new midpoints.
                trianglesOverwrite.Add(new Triangle(a, ab, ca));
                trianglesOverwrite.Add(new Triangle(b, bc, ab));
                trianglesOverwrite.Add(new Triangle(c, ca, bc));
                trianglesOverwrite.Add(new Triangle(ab, bc, ca));
            }

            // Replace all our old polygons with the new set of
            // subdivided ones.
            triangles = trianglesOverwrite;
        }
    }

    private int GetMidPointIndex(Dictionary<int, int> cache, int indexA, int indexB)
    {
        // We create a key out of the two original indices
        // by storing the smaller index in the upper two bytes
        // of an integer, and the larger index in the lower two
        // bytes. By sorting them according to whichever is smaller
        // we ensure that this function returns the same result
        // whether you call
        // GetMidPointIndex(cache, 5, 9)
        // or...
        // GetMidPointIndex(cache, 9, 5)
        int smallerIndex = Mathf.Min (indexA, indexB);
        int greaterIndex = Mathf.Max(indexA, indexB);
        int key = (smallerIndex << 16) + greaterIndex;

        // If a midpoint is already defined, just return it.
        int ret;
        if (cache.TryGetValue(key, out ret))
        {
            return ret;
        }
        
        // If we're here, it's because a midpoint for these two
        // vertices hasn't been created yet. Let's do that now!
        Vector3 p1 = vertices[indexA];
        Vector3 p2 = vertices[indexB];
        Vector3 middle = Vector3.Lerp(p1, p2, 0.5f).normalized;

        ret = vertices.Count;
        vertices.Add(middle);

        cache.Add(key, ret);
        return ret;
    }

    private void AssembleMeshAndGetTileData()
    {
        Mesh mesh = new Mesh();

        //Parse lists to arrays
        int verticesCount = triangles.Count * 3;
        int[] trianglesArray = new int[verticesCount];
        Vector3[] verticesArray = new Vector3[verticesCount];
        Vector3[] normalsArray = new Vector3[verticesCount];

        for (int i = 0; i < triangles.Count; i++)
        {
            Triangle triangleAtIndex = triangles[i];

            //Parse list to array (and organize in a way Unity likes)
            trianglesArray[i * 3 + 0] = i * 3 + 0;
            trianglesArray[i * 3 + 1] = i * 3 + 1;
            trianglesArray[i * 3 + 2] = i * 3 + 2;

            verticesArray[i * 3 + 0] = vertices[triangleAtIndex.vertexIndexA];
            verticesArray[i * 3 + 1] = vertices[triangleAtIndex.vertexIndexB];
            verticesArray[i * 3 + 2] = vertices[triangleAtIndex.vertexIndexC];

            //Centroids/normals for tiles and normals (since this is a unit icosahedron, this is both the position of the triangle/face's centroid *and* the direction of its surface normal)
            Vector3 triangleCenter = new Vector3(
                (vertices[triangleAtIndex.vertexIndexA].x + vertices[triangleAtIndex.vertexIndexB].x + vertices[triangleAtIndex.vertexIndexC].x) / 3f,
                (vertices[triangleAtIndex.vertexIndexA].y + vertices[triangleAtIndex.vertexIndexB].y + vertices[triangleAtIndex.vertexIndexC].y) / 3f,
                (vertices[triangleAtIndex.vertexIndexA].z + vertices[triangleAtIndex.vertexIndexB].z + vertices[triangleAtIndex.vertexIndexC].z) / 3f
            );

            ////Calculate colour relative to four significant tiles - white king spawn, black king spawn, and their two frontlines 90 degrees away
            //Vector3 topOfBoard = new Vector3(-0.5f, 0.7f, 0.3f);
            //Vector3 bottomOfBoard = new Vector3(0.5f, -0.7f, -0.3f);
            //Vector3 whiteFrontOfBoard = new Vector3(-0.2f, -0.3f, 0.9f);
            //Vector3 blackFrontOfBoard = new Vector3(0.2f, 0.3f, -0.9f);
            //float distanceToTopOfBoard = (triangleCenter - topOfBoard).magnitude / 2f;
            //float distanceToBottomOfBoard = (triangleCenter - bottomOfBoard).magnitude  /2f;
            //float distanceToWhiteFrontOfBoard = (triangleCenter - whiteFrontOfBoard).magnitude / 2f;
            //float distanceToBlackFrontOfBoard = (triangleCenter - blackFrontOfBoard).magnitude / 2f;
            //Vector3 colorVectorTop = new Vector3(Color.blue.r, 1f, Color.blue.b) * (1f - distanceToTopOfBoard);
            //Vector3 colorVectorBottom = new Vector3(1f, Color.red.g, Color.red.b) * (1f - distanceToBottomOfBoard);
            //Vector3 colorVectorWhiteFront = new Vector3(0.024f, 0.655f, 0.49f) * (1f - distanceToWhiteFrontOfBoard);
            //Vector3 colorVectorBlackFront = new Vector3(0.898f, 0.584f, 0f) * (1f - distanceToBlackFrontOfBoard);
            //Vector3 colorVectorTile = ((colorVectorTop * 1.5f) + (colorVectorBottom * 1.5f) + colorVectorWhiteFront + colorVectorBlackFront).normalized;
            //Color tileColor = new Color(colorVectorTile.x, colorVectorTile.y, colorVectorTile.z, 1f);

            Vector3 normalizedTriangleCenter = new Vector3((triangleCenter.x + 1f) / 2f, (triangleCenter.y + 1f) / 2f, (triangleCenter.z + 1f) / 2f);
            Color tileColor = new Color(normalizedTriangleCenter.x, normalizedTriangleCenter.z, normalizedTriangleCenter.y, 1f);

            char tileHumanReadableIDSymbol1 = (char)((i / 4) + 65); //65 is the ASCII code for A
            int tileHumanReadableIDSymbol2 = ((i + 1) % 4) + 1;
            string tileHumanReadableID = "" + tileHumanReadableIDSymbol1 + tileHumanReadableIDSymbol2;

            tiles.Add(
                new Tile(
                    i,
                    tileHumanReadableID,
                    vertices[triangleAtIndex.vertexIndexA],
                    vertices[triangleAtIndex.vertexIndexB],
                    vertices[triangleAtIndex.vertexIndexC],
                    triangleCenter,
                    tileColor
                )
            );

            normalsArray[i * 3 + 0] = triangleCenter;
            normalsArray[i * 3 + 1] = triangleCenter;
            normalsArray[i * 3 + 2] = triangleCenter;
        }

        mesh.vertices = verticesArray;
        mesh.normals = normalsArray;
        mesh.SetTriangles(trianglesArray, 0);
        mesh.name = "Procedural Mesh";

        GetComponent<MeshFilter>().mesh = mesh;
    }

    private void GenerateAllTiles()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            GenerateTile(tiles[i], i);
        }

        TilesGetBaseNeighbourData();
        TilesGetSideNeighborData();
        TileGetDirectNeighborData();

        //Assign colour
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i].adjacentNeighborIDsCornerDirect.Count == 3)
            {
                tiles[i].color = new Color(
                    Mathf.Clamp01(tiles[i].color.r * 1.5f),
                    Mathf.Clamp01(tiles[i].color.g * 1.5f),
                    Mathf.Clamp01(tiles[i].color.b * 1.5f)
                );
            }

            tiles[i].instanceTileGameObject.GetComponent<MeshRenderer>().material.color = tiles[i].color;
        }
    }

    private void GenerateTile(Tile tile, int index)
    {
        //Object
        GameObject instanceTile = Instantiate(prefabTile);
        instanceTile.transform.parent = transform.Find("Tiles");
        tile.instanceTileGameObject = instanceTile;
        tile.instanceTileGameObject.name = "Tile[" + index + "]";
        instanceTile.GetComponent<TileInstance>().id = tile.id;

        //Mesh
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[3] { tile.vertexA, tile.vertexB, tile.vertexC };
        mesh.normals = new Vector3[3] { tile.centroidAndNormal, tile.centroidAndNormal, tile.centroidAndNormal };
        mesh.SetTriangles(new int[3] { 0, 1, 2 }, 0);
        mesh.name = "Procedural Mesh";
        MeshFilter meshFilter = instanceTile.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        //Material
        MeshRenderer meshRenderer = instanceTile.AddComponent<MeshRenderer>();
        meshRenderer.material = materialLit;

        ////Colour
        //MeshRenderer meshRenderer = instanceTile.AddComponent<MeshRenderer>();
        //meshRenderer.material = materialLit;
        //meshRenderer.material.color = tile.color;

        //Collider (for selecting with raycasting)
        MeshCollider meshCollider = instanceTile.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    private void TilesGetBaseNeighbourData()
    {
        //Neighbour data - each tile created with an int storing data about neighbours, to be used for colouring
        for (int i = 0; i < tiles.Count; i++)
        {
            for (int j = 0; j < tiles.Count; j++)
            {
                //If any vertices are shared
                int sharedVertices = GetTileSharedVertices(i, j);

                //Debug.Log("Tile[" + i + "] + Tile[" + j + "] shared verts: " + sharedVertices);
                if (sharedVertices == 2)
                {
                    //Edge
                    tiles[i].adjacentNeighborIDsEdge.Add(tiles[j].id);
                }
                else if (sharedVertices == 1)
                {
                    //Corner (we make separate lists for directs and sides later)
                    tiles[i].adjacentNeighborIDsCorner.Add(tiles[j].id);
                }
            }
        }
    }

    public int GetTileSharedVertices(int tileID1, int tileID2)
    {
        int sharedVertices = 0;

        if (tiles[tileID1].vertexA == tiles[tileID2].vertexA) sharedVertices++;
        if (tiles[tileID1].vertexA == tiles[tileID2].vertexB) sharedVertices++;
        if (tiles[tileID1].vertexA == tiles[tileID2].vertexC) sharedVertices++;

        if (tiles[tileID1].vertexB == tiles[tileID2].vertexA) sharedVertices++;
        if (tiles[tileID1].vertexB == tiles[tileID2].vertexB) sharedVertices++;
        if (tiles[tileID1].vertexB == tiles[tileID2].vertexC) sharedVertices++;

        if (tiles[tileID1].vertexC == tiles[tileID2].vertexA) sharedVertices++;
        if (tiles[tileID1].vertexC == tiles[tileID2].vertexB) sharedVertices++;
        if (tiles[tileID1].vertexC == tiles[tileID2].vertexC) sharedVertices++;

        return sharedVertices;
    }

    private void TilesGetSideNeighborData()
    {
        //Side corners share exactly one vertex and are once removed from sharing an edge
        for (int i = 0; i < tiles.Count; i++)
        {
            //Each tile
            for (int j = 0; j < tiles[i].adjacentNeighborIDsEdge.Count; j++)
            {
                int immediateEdgeID = tiles[i].adjacentNeighborIDsEdge[j];
                //Each edge neighbor per tile
                for (int k = 0; k < tiles[immediateEdgeID].adjacentNeighborIDsEdge.Count; k++)
                {
                    int onceRemovedEdgeID = tiles[immediateEdgeID].adjacentNeighborIDsEdge[k];
                    //Each edge once removed per immediate edge per tile
                    if (tiles[i].id != tiles[onceRemovedEdgeID].id)
                    {
                        //As long as not self, this is a side corner
                        tiles[i].adjacentNeighborIDsCornerSide.Add(tiles[onceRemovedEdgeID].id);
                    }
                }
            }
        }
    }

    private void TileGetDirectNeighborData()
    {
        //All corners that are not side corners are direct corners
        for (int i = 0; i < tiles.Count; i++)
        {
            //Each tile
            for (int j = 0; j < tiles[i].adjacentNeighborIDsCorner.Count; j++)
            {
                //Each corner
                int cornerID = tiles[i].adjacentNeighborIDsCorner[j];

                //Check if corner is a side corner
                bool cornerIsASideCorner = false;
                for (int k = 0; k < tiles[i].adjacentNeighborIDsCornerSide.Count; k++)
                {
                    //Check if side corner ID is the same as this corner
                    if (tiles[i].adjacentNeighborIDsCornerSide[k] == cornerID)
                    {
                        cornerIsASideCorner = true;
                    }
                }

                //All corners that are not side corners are direct corners
                if (!cornerIsASideCorner)
                {
                    //Add to direct corners list
                    tiles[i].adjacentNeighborIDsCornerDirect.Add(tiles[cornerID].id);
                }
            }
        }
    }

    public TileNeighborType IsTileNeighbor(int tileIDToCheck, int tileIDRelativeTo)
    {
        //Edge neighbour?
        for (int i = 0; i < tiles[tileIDRelativeTo].adjacentNeighborIDsEdge.Count; i++)
        {
            if (tileIDToCheck == tiles[tileIDRelativeTo].adjacentNeighborIDsEdge[i])
            {
                return TileNeighborType.Edge;
            }
        }

        //Direct corner neighbour?
        for (int j = 0; j < tiles[tileIDRelativeTo].adjacentNeighborIDsCornerDirect.Count; j++)
        {
            if (tileIDToCheck == tiles[tileIDRelativeTo].adjacentNeighborIDsCornerDirect[j])
            {
                return TileNeighborType.CornerDirect;
            }
        }

        //Sideways corner neighbour?
        for (int k = 0; k < tiles[tileIDRelativeTo].adjacentNeighborIDsCornerSide.Count; k++)
        {
            if (tileIDToCheck == tiles[tileIDRelativeTo].adjacentNeighborIDsCornerSide[k])
            {
                return TileNeighborType.CornerSide;
            }
        }

        //Unrelated
        return TileNeighborType.Stranger;
    }

    private void TilesDebugPrint()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            //Debug.Log("(Tile [" + i + "].instanceGameObject == null) = " + (tiles[i].instanceGameObject == null));

            Debug.Log("-------------");
            Debug.Log("Tile [" + i + "] - "
                + tiles[i].adjacentNeighborIDsEdge.Count + " edges, "
                + tiles[i].adjacentNeighborIDsCornerSide.Count + " side corners, "
                + tiles[i].adjacentNeighborIDsCornerDirect.Count + " direct corners"
            );
            
            for (int j = 0; j < tiles[i].adjacentNeighborIDsEdge.Count; j++)
            {
                Debug.Log("Edge[" + i + "," + j + "] = " + tiles[i].adjacentNeighborIDsEdge[j]);
            }
            for (int j = 0; j < tiles[i].adjacentNeighborIDsCornerSide.Count; j++)
            {
                Debug.Log("CornerSide[" + i + "," + j + "] = " + tiles[i].adjacentNeighborIDsCornerSide[j]);
            }
            for (int k = 0; k < tiles[i].adjacentNeighborIDsCornerDirect.Count; k++)
            {
                Debug.Log("CornerDirect[" + i + "," + k + "] = " + tiles[i].adjacentNeighborIDsCornerDirect[k]);
            }
        }
    }
}

public class Tile
{
    public int id;
    public string humanReadableID;
    public GameObject instanceTileGameObject;
    public List<int> adjacentNeighborIDsEdge = new List<int> { };
    public List<int> adjacentNeighborIDsCorner = new List<int> { };
    public List<int> adjacentNeighborIDsCornerDirect = new List<int> { };
    public List<int> adjacentNeighborIDsCornerSide = new List<int> { };
    public Vector3 vertexA;
    public Vector3 vertexB;
    public Vector3 vertexC;
    public Vector3 centroidAndNormal;
    public Color color;
    public GameObject instancePieceGameObject;

    public Tile
    (
        int id,
        string humanReadableID,
        Vector3 vertexA,
        Vector3 vertexB,
        Vector3 vertexC,
        Vector3 centroidAndNormal,
        Color color
    )
    {
        this.id = id;
        this.humanReadableID = humanReadableID;
        this.vertexA = vertexA;
        this.vertexB = vertexB;
        this.vertexC = vertexC;
        this.centroidAndNormal = centroidAndNormal;
        this.color = color;
    }
}