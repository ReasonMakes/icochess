using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerationFibonacci : MonoBehaviour
{
    //Constants
    private readonly float TAU = 6.2831853071f;
    private readonly float PHI = 1.6180339887f;

    //References
    public GameObject point;
    public GameObject plots;

    private void Start()
    {
        GenerateVerticies();
    }

    private void Update()
    {
        //UpdateVerticies();

        //if (Input.GetKeyDown(KeyCode.E))
        //{
        //    UpdateVerticies();
        //}
    }

    private void UpdateVerticies()
    {
        ClearPoints();
        GenerateVerticies();
    }

    private void GenerateVerticies()
    {
        int nPoints = 1000;
        float turnFraction = PHI;
        for (int i = 0; i < nPoints; i++)
        {
            float distance = Mathf.Pow(i / (nPoints - 1f), 0.5f);
            float angle = TAU * turnFraction * i;

            float x = distance * Mathf.Cos(angle);
            float y = distance * Mathf.Sin(angle);

            PlotPoint(x, y, Color.yellow);
        }
    }

    private void PlotPoint(float x, float y, Color color)
    {
        GameObject instancedPoint = Instantiate(point, new Vector3(x, y, 0f), Quaternion.identity);
        instancedPoint.transform.parent = plots.transform;
        //instancedPoint.GetComponent<point>().color = color;
    }

    private void ClearPoints()
    {
        for (int i = 0; i < plots.transform.childCount; i++)
        {
            Destroy(plots.transform.GetChild(i).gameObject);
        }
    }
}
