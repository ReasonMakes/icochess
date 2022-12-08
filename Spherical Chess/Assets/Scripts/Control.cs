using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Control : MonoBehaviour
{
    public PlayerController playerController;

    [System.NonSerialized] private TextMeshProUGUI infoTopRight;
    [System.NonSerialized] private TextMeshProUGUI infoBottom;

    private readonly int FPS_PRINT_PERIOD = 1;
    private int fps = 0;
    private float previousPrintTime = 0;

    private void Awake()
    {
        infoTopRight = transform.Find("Canvas").Find("Info Top-Right").GetComponent<TextMeshProUGUI>();
        infoBottom = transform.Find("Canvas").Find("Info Bottom").GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 300;
        UpdateBottomText();
    }

    private void Update()
    {
        if ((Time.time - previousPrintTime) > FPS_PRINT_PERIOD)
        {
            UpdateFPS();

            //Get ready to print next update
            previousPrintTime = Time.time;
        }

        //Get data
        int highlightedTileID;
        string tileString;
        if (playerController.highlightedTile != null)
        {
            highlightedTileID = playerController.highlightedTile.GetComponent<TileInstance>().id;
            tileString = "\nTile " + highlightedTileID;
        }
        else
        {
            tileString = "";
        }

        //Text
        infoTopRight.text = fps + " fps" + tileString;
    }

    public void UpdateFPS()
    {

        fps = (int)(1f / Time.unscaledDeltaTime);

    }

    public void UpdateBottomText()
    {
        infoBottom.text = playerController.teamWhoseTurnItIs + " to play";
    }
}