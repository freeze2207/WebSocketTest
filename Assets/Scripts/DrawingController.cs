using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawingController : MonoBehaviour
{
    [SerializeField] private GameObject mLinePrefab;
    private GameObject mCurrentLine;
    private LineRenderer mLineRenderer;
    private List<Vector2> mCurrentLinePoints = new List<Vector2>();
    private List<GameObject> mCurrentLines = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
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
