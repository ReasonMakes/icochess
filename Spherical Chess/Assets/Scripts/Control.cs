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
    private float previousPrintTime = 0;

    private void Awake()
    {
        infoTopRight = transform.Find("Canvas").Find("Info Top-Right").GetComponent<TextMeshProUGUI>();
        infoBottom = transform.Find("Canvas").Find("Info Bottom").GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 240;
        UpdateBottomText();
    }

    private void Update()
    {
        if ((Time.time - previousPrintTime) > FPS_PRINT_PERIOD)
        {
            UpdateTopRightText();

            //Get ready to print next update
            previousPrintTime = Time.time;
        }
    }

    public void UpdateTopRightText()
    {
        //Get data
        int highlightedTileID = -1;
        if (playerController.highlightedTile != null)
        {
            highlightedTileID = playerController.highlightedTile.GetComponent<TileInstance>().id;
        }

        int fps = (int)(1f / Time.unscaledDeltaTime);

        //Text
        infoTopRight.text =
            fps + " fps"
            + "\nTile " + highlightedTileID;
    }

    public void UpdateBottomText()
    {
        infoBottom.text = playerController.teamWhoseTurnItIs + " to play";
    }
}