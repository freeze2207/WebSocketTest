using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawingController : Singleton<DrawingController>
{
    public enum DrawingModes
    {
        EDRAW_CREATE,
        EDRAW_CLEAN,
    }

    // UI
    public Button ClearBtn;
    public Dropdown ColorDropDown;

    // Controller
    public float Sensitivity = 0.4f;
    [SerializeField] private GameObject mLinePrefab;
    [SerializeField] private GameObject mDrawingPanel;

    // Runtime
    private bool mCanDraw = false;
    private GameObject mCurrentLine;
    private LineRenderer mLineRenderer;
    private List<Vector2> mCurrentLinePoints = new List<Vector2>();
    private List<GameObject> mAllExistingLines = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        this.mCanDraw = (GameplayController.Instance.GetCurrentGameState() == GameplayController.GameStates.EGAME_DRAW) && (WSConnectionController.Instance.GetConnectionStatus());
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
                if (Vector2.Distance(hitPosition, mCurrentLinePoints[mCurrentLinePoints.Count - 1]) > this.Sensitivity)
                {
                    this.UpdateLine(hitPosition);
                }
            }
        }

        // release the left click mouse
        if (Input.GetMouseButtonUp(0))
        {
            WSConnectionController.Instance.SyncDrawing(DrawingModes.EDRAW_CREATE, this.mCurrentLinePoints);
        }
        
    }

    private void CreateLine()
    {
        this.mCurrentLinePoints.Clear();
        this.mCurrentLine = GameObject.Instantiate(mLinePrefab, Vector3.zero, Quaternion.identity);
        this.mLineRenderer = mCurrentLine.GetComponent<LineRenderer>();
        this.mAllExistingLines.Add(mCurrentLine);

        Vector2 currentPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        this.mCurrentLinePoints.Add(currentPoint);
        this.mCurrentLinePoints.Add(currentPoint);
        this.mLineRenderer.SetPosition(0, mCurrentLinePoints[0]);
        this.mLineRenderer.SetPosition(1, mCurrentLinePoints[1]);

        this.mCurrentLinePoints.Add(currentPoint);

    }

    private void UpdateLine(Vector2 _newPoint)
    {
        this.mCurrentLinePoints.Add(_newPoint);
        this.mLineRenderer.positionCount++;
        this.mLineRenderer.SetPosition(this.mLineRenderer.positionCount - 1, _newPoint);
    }

    private void CleanUpLines()
    {
        foreach (GameObject go in this.mAllExistingLines)
        {
            Destroy(go);
        }
        this.mAllExistingLines.Clear();

        // Sync with all client here
        WSConnectionController.Instance.SyncDrawing(DrawingModes.EDRAW_CLEAN);
    }

    public void SyncCreateLine(List<Vector2> _points)
    {
        this.mCurrentLine = GameObject.Instantiate(mLinePrefab, Vector3.zero, Quaternion.identity);
        this.mLineRenderer = mCurrentLine.GetComponent<LineRenderer>();
        this.mAllExistingLines.Add(this.mCurrentLine);
        this.mCurrentLinePoints = _points;

        this.mLineRenderer.positionCount = this.mCurrentLinePoints.Count;
        for (int i = 0; i < this.mCurrentLinePoints.Count; i++)
        {
            this.mLineRenderer.SetPosition(i, this.mCurrentLinePoints[i]);
        }
    }

    public void SyncCleanUp()
    {
        if (this.mAllExistingLines.Count == 0)
        {
            return;
        }
        foreach (GameObject go in this.mAllExistingLines)
        {
            Destroy(go);
        }
        this.mAllExistingLines.Clear();
        this.mCurrentLine = null;
    }
}
