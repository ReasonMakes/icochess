using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Control : MonoBehaviour
{
    public PlayerController playerController;
    public PieceController pieceController;

    [System.NonSerialized] public TextMeshProUGUI infoTopLeft;
    private TextMeshProUGUI infoTopRight;
    private TextMeshProUGUI infoBottom;

    private readonly int FPS_PRINT_PERIOD = 1;
    private int fps = 0;
    private float previousFPSPrintTime = 0;
    private string tileString;
    private bool fullscreen = false;

    private void Awake()
    {
        infoTopLeft = transform.Find("Canvas").Find("Info Top-Left").GetComponent<TextMeshProUGUI>();
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
        //SETTINGS
        if (Input.GetKeyDown(KeyCode.F11))
        {
            fullscreen = !fullscreen;
        }
        if (fullscreen)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }

        //UI
        if ((Time.time - previousFPSPrintTime) > FPS_PRINT_PERIOD)
        {
            //Calculate fps
            fps = (int)(1f / Time.unscaledDeltaTime);

            //Get ready to print next update
            previousFPSPrintTime = Time.time;
        }

        //Text
        infoTopRight.text = fps + " fps" + tileString;
    }

    public void UpdateTileRelatedText()
    {
        if (playerController.highlightedTile != null)
        {
            int highlightedTileID = playerController.highlightedTile.GetComponent<TileInstance>().id;
            tileString = "\nTile " + highlightedTileID;

            Piece.Allegiance pieceAllegianceOnHighlightedTile = pieceController.GetTilePieceAllegiance(highlightedTileID);
            if (pieceAllegianceOnHighlightedTile != Piece.Allegiance.None)
            {
                tileString += "\n" + pieceAllegianceOnHighlightedTile + " " + pieceController.GetTilePieceType(highlightedTileID);
            }
        }
        else
        {
            tileString = "";
        }
    }

    public void ClearTileRelatedText()
    {
        tileString = "";
    }

    public void UpdateBottomText()
    {
        infoBottom.text = playerController.teamWhoseTurnItIs + " to play";
        if (pieceController.checkState != PieceController.Check.None)
        {
            infoBottom.text += "\n" + pieceController.checkState + " is in check";
        }
    }
}