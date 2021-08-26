using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawingController : Singleton<DrawingController>
{
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
            Vector2 hitPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (Vector2.Distance(hitPosition, mCurrentLinePoints[mCurrentLinePoints.Count - 1]) > 0.1f)
            {
                UpdateLine(hitPosition);
            }
        }
    }

    private void CreateLine()
    {
        mCurrentLine = GameObject.Instantiate(mLinePrefab, Vector3.zero, Quaternion.identity);
        mLineRenderer = mCurrentLine.GetComponent<LineRenderer>();
        mCurrentLinePoints.Clear();
        mCurrentLines.Add(mCurrentLine);
        
        // Sync with all client here

        Vector2 currentPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mCurrentLinePoints.Add(currentPoint);
        mCurrentLinePoints.Add(currentPoint);
        mLineRenderer.SetPosition(0, mCurrentLinePoints[0]);
        mLineRenderer.SetPosition(1, mCurrentLinePoints[1]);
    }

    private void UpdateLine(Vector2 _newPoint)
    {
        mCurrentLinePoints.Add(_newPoint);
        mLineRenderer.positionCount++;
        mLineRenderer.SetPosition(mLineRenderer.positionCount - 1, _newPoint);

        // Sync with all client here
    }

    private void CleanUpLines()
    {
        foreach (GameObject go in this.mCurrentLines)
        {
            Destroy(go);
        }
        this.mCurrentLines.Clear();
    }
}
