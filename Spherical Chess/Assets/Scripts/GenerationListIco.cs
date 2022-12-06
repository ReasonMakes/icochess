using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerationListIco : MonoBehaviour
{
    //Constants
    //private readonly float TAU = 6.2831853071f;
    private readonly float PHI = 1.6180339887f; //(1.0f + Mathf.Sqrt(5.0f)) / 2.0f

    //References
    public GameObject point;
    public GameObject plots;

    //Procedural mesh
    private List<Triangle> m_Triangles;
    private List<Vector3> m_Vertices;

    private void Start()
    {
        //Procedurally generate playing field
        InitShapeIcosahedron();
        Subdivide(2);
        GenerateMesh();
    }

    private void InitShapeIcosahedron()
    {
        m_Triangles = new List<Triangle>();
        m_Vertices = new List<Vector3>();

        // An icosahedron has 12 vertices, and
        // since it's completely symmetrical the
        // formula for calculating them is kind of
        // symmetrical too:

        float t = PHI;

        //Positions of each triangle's vertices
        m_Vertices.Add(new Vector3(-1, t, 0).normalized);
        m_Vertices.Add(new Vector3(1, t, 0).normalized);
        m_Vertices.Add(new Vector3(-1, -t, 0).normalized);
        m_Vertices.Add(new Vector3(1, -t, 0).normalized);
        m_Vertices.Add(new Vector3(0, -1, t).normalized);
        m_Vertices.Add(new Vector3(0, 1, t).normalized);
        m_Vertices.Add(new Vector3(0, -1, -t).normalized);
        m_Vertices.Add(new Vector3(0, 1, -t).normalized);
        m_Vertices.Add(new Vector3(t, 0, -1).normalized);
        m_Vertices.Add(new Vector3(t, 0, 1).normalized);
        m_Vertices.Add(new Vector3(-t, 0, -1).normalized);
        m_Vertices.Add(new Vector3(-t, 0, 1).normalized);

        // And here's the formula for the 20 sides,
        // referencing the 12 vertices we just created.
        m_Triangles.Add(new Triangle(0, 11, 5));
        m_Triangles.Add(new Triangle(0, 5, 1));
        m_Triangles.Add(new Triangle(0, 1, 7));
        m_Triangles.Add(new Triangle(0, 7, 10));
        m_Triangles.Add(new Triangle(0, 10, 11));
        m_Triangles.Add(new Triangle(1, 5, 9));
        m_Triangles.Add(new Triangle(5, 11, 4));
        m_Triangles.Add(new Triangle(11, 10, 2));
        m_Triangles.Add(new Triangle(10, 7, 6));
        m_Triangles.Add(new Triangle(7, 1, 8));
        m_Triangles.Add(new Triangle(3, 9, 4));
        m_Triangles.Add(new Triangle(3, 4, 2));
        m_Triangles.Add(new Triangle(3, 2, 6));
        m_Triangles.Add(new Triangle(3, 6, 8));
        m_Triangles.Add(new Triangle(3, 8, 9));
        m_Triangles.Add(new Triangle(4, 9, 5));
        m_Triangles.Add(new Triangle(2, 4, 11));
        m_Triangles.Add(new Triangle(6, 2, 10));
        m_Triangles.Add(new Triangle(8, 6, 7));
        m_Triangles.Add(new Triangle(9, 8, 1));
    }

    private void Subdivide(int recursions)
    {
        var midPointCache = new Dictionary<int, int>();

        for (int i = 0; i < recursions; i++)
        {
            var newPolys = new List<Triangle>();
            foreach (var poly in m_Triangles)
            {
                int a = poly.vertices[0];
                int b = poly.vertices[1];
                int c = poly.vertices[2];

                // Use GetMidPointIndex to either create a
                // new vertex between two old vertices, or
                // find the one that was already created.
                int ab = GetMidPointIndex(midPointCache, a, b);
                int bc = GetMidPointIndex(midPointCache, b, c);
                int ca = GetMidPointIndex(midPointCache, c, a);

                // Create the four new polygons using our original
                // three vertices, and the three new midpoints.
                newPolys.Add(new Triangle(a, ab, ca));
                newPolys.Add(new Triangle(b, bc, ab));
                newPolys.Add(new Triangle(c, ca, bc));
                newPolys.Add(new Triangle(ab, bc, ca));
            }

            // Replace all our old polygons with the new set of
            // subdivided ones.
            m_Triangles = newPolys;
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
        Vector3 p1 = m_Vertices[indexA];
        Vector3 p2 = m_Vertices[indexB];
        Vector3 middle = Vector3.Lerp(p1, p2, 0.5f).normalized;

        ret = m_Vertices.Count;
        m_Vertices.Add(middle);

        cache.Add(key, ret);
        return ret;
    }

    private void GenerateMesh()
    {
        Mesh mesh = new Mesh();

        int verticesCount = m_Triangles.Count * 3;
        int[] triangles = new int[verticesCount];
        Vector3[] vertices = new Vector3[verticesCount];
        Vector3[] normals = new Vector3[verticesCount];

        Color32[] colors = new Color32[verticesCount];
        //Color32 green = new Color32(20, 255, 30, 255);
        //Color32 brown = new Color32(220, 150, 70, 255);

        //Parse lists to arrays
        for (int i = 0; i < m_Triangles.Count; i++)
        {
            Triangle triangleAtIndex = m_Triangles[i];

            triangles[i * 3 + 0] = i * 3 + 0;
            triangles[i * 3 + 1] = i * 3 + 1;
            triangles[i * 3 + 2] = i * 3 + 2;
            vertices[i * 3 + 0] = m_Vertices[triangleAtIndex.vertices[0]];
            vertices[i * 3 + 1] = m_Vertices[triangleAtIndex.vertices[1]];
            vertices[i * 3 + 2] = m_Vertices[triangleAtIndex.vertices[2]];
        
            //Random colour per triangle
            Color32 colorLerp = new Color32( //Color32.Lerp(green, brown, Random.Range(0.0f, 1.0f));
                (byte)(Random.Range(0.0f, 1.0f) * 255),
                (byte)(Random.Range(0.0f, 1.0f) * 255),
                (byte)(Random.Range(0.0f, 1.0f) * 255),
                255
            );
            colors[i * 3 + 0] = colorLerp;
            colors[i * 3 + 1] = colorLerp;
            colors[i * 3 + 2] = colorLerp;
        
            // For now our planet is still perfectly spherical, so
            // so the normal of each vertex is just like the vertex
            // itself: pointing away from the origin.
            normals[i * 3 + 0] = m_Vertices[triangleAtIndex.vertices[0]];
            normals[i * 3 + 1] = m_Vertices[triangleAtIndex.vertices[0]];
            normals[i * 3 + 2] = m_Vertices[triangleAtIndex.vertices[0]];
        }

        mesh.vertices = vertices;
        //mesh.normals = normals;
        mesh.colors32 = colors;
        mesh.SetTriangles(triangles, 0);

        GetComponent<MeshFilter>().mesh = mesh;
    }
}

public class Triangle
{
    public List<int> vertices;

    public Triangle(int a, int b, int c)
    {
        vertices = new List<int>() { a, b, c };
    }
}