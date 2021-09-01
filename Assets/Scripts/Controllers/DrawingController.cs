using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawingController : Singleton<DrawingController>
{
    public enum DrawingModes
    {
        EDRAW_CREATE,
        EDRAW_UPDATE,
        EDRAW_CLEAN,
    }

    // UI
    public Button ClearBtn;
    public Dropdown ColorDropDown;

    // Controller
    [SerializeField] private GameObject mLinePrefab;
    [SerializeField] private GameObject mDrawingPanel;

    private bool mCanDraw = false;
    private GameObject mCurrentLine;
    private LineRenderer mLineRenderer;
    private List<Vector2> mCurrentLinePoints = new List<Vector2>();
    private List<GameObject> mCurrentLines = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        this.mCanDraw = (GameplayController.Instance.GetCurrentGameState() == GameplayController.GameStates.EGAME_DRAW);
        ClearBtn.onClick.AddListener(CleanUpLines);
        GameplayController.Instance.GameStateChanged.AddListener(ChangeDrawingState);
    }

    private void ChangeDrawingState(GameplayController.GameStates _state)
    {
        this.mCanDraw = (_state == GameplayController.GameStates.EGAME_DRAW);
        this.mDrawingPanel.SetActive(this.mCanDraw);
    }

    // Update is called once per frame
    void Update()
    {
        if (!this.mCanDraw)
        {
            return;
        }

        // Press the Mouse
        if (Input.GetMouseButtonDown(0))
        {
            this.CreateLine();
        }

        // Move while pressed
        if(Input.GetMouseButton(0))
        {
            if (mCurrentLinePoints.Count > 0)
            {
                Vector2 hitPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if (Vector2.Distance(hitPosition, mCurrentLinePoints[mCurrentLinePoints.Count - 1]) > 0.5f)
                {
                    this.UpdateLine(hitPosition);
                }
            }
            
        }
    }

    private void CreateLine()
    {
        /*this.mCurrentLine = GameObject.Instantiate(mLinePrefab, Vector3.zero, Quaternion.identity);
        this.mLineRenderer = mCurrentLine.GetComponent<LineRenderer>();
        this.mCurrentLinePoints.Clear();
        this.mCurrentLines.Add(mCurrentLine);

        Vector2 currentPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        this.mCurrentLinePoints.Add(currentPoint);
        this.mCurrentLinePoints.Add(currentPoint);
        this.mLineRenderer.SetPosition(0, mCurrentLinePoints[0]);
        this.mLineRenderer.SetPosition(1, mCurrentLinePoints[1]);*/

        // Sync with all client here
        //WSConnectionController.Instance.SyncDrawing(DrawingModes.EDRAW_CREATE, currentPoint);
        WSConnectionController.Instance.SyncDrawing(DrawingModes.EDRAW_CREATE, Camera.main.ScreenToWorldPoint(Input.mousePosition));
    }

    private void UpdateLine(Vector2 _newPoint)
    {
        /*this.mCurrentLinePoints.Add(_newPoint);
        this.mLineRenderer.positionCount++;
        this.mLineRenderer.SetPosition(this.mLineRenderer.positionCount - 1, _newPoint);*/

        // Sync with all client here
        WSConnectionController.Instance.SyncDrawing(DrawingModes.EDRAW_UPDATE, _newPoint);

    }

    private void CleanUpLines()
    {
        foreach (GameObject go in this.mCurrentLines)
        {
            Destroy(go);
        }
        this.mCurrentLines.Clear();

        // Sync with all client here
        WSConnectionController.Instance.SyncDrawing(DrawingModes.EDRAW_CLEAN);
    }

    public void SyncCreateLine(Vector2 _pointToCreate)
    {
        this.mCurrentLine = GameObject.Instantiate(mLinePrefab, Vector3.zero, Quaternion.identity);
        this.mLineRenderer = mCurrentLine.GetComponent<LineRenderer>();
        this.mCurrentLinePoints.Clear();
        this.mCurrentLines.Add(mCurrentLine);

        this.mCurrentLinePoints.Add(_pointToCreate);
        this.mCurrentLinePoints.Add(_pointToCreate);
        this.mLineRenderer.SetPosition(0, mCurrentLinePoints[0]);
        this.mLineRenderer.SetPosition(1, mCurrentLinePoints[1]);
    }

    public void SyncUpdateLine(Vector2 _newPoint)
    {
        if (this.mCurrentLine == null)
        {
            return;
        }
        this.mCurrentLinePoints.Add(_newPoint);
        this.mLineRenderer.positionCount++;
        this.mLineRenderer.SetPosition(this.mLineRenderer.positionCount - 1, _newPoint);
    }

    public void SyncCleanUp()
    {
        if (this.mCurrentLines.Count == 0)
        {
            return;
        }
        foreach (GameObject go in this.mCurrentLines)
        {
            Destroy(go);
        }
        this.mCurrentLines.Clear();
    }
}
