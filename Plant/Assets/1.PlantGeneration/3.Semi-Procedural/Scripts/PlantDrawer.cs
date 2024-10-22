using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum DrawerState
{
    Drawing,
    Moving,
    Waiting
}

public class PlantDrawer : MonoBehaviour
{
    [Header("Main Semi-Procedural components")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Material _lineMaterial;
    private float _drawerGap = 1.2f;

    private int _currentIndex = 0;
    private List<List<Vector2>> _drawnPoints = new List<List<Vector2>>();

    private DrawerState _state = DrawerState.Waiting;
    private Vector3 _origin;
    private Vector3 _startingPoint;

    [Header("Visual generator")]
    [SerializeField] private VisualGenerator _generator;
    private List<List<BranchPart>> _branchesParts = new List<List<BranchPart>>();

    // Start is called before the first frame update
    void Start()
    {
        if (_mainCamera == null) Debug.LogError("No camera");
        if (_lineMaterial == null) Debug.LogError("No material for drawing");

        _drawnPoints.Add(new List<Vector2>());
        _branchesParts.Add(new List<BranchPart>());
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 mousePosition = Input.mousePosition;

        switch (_state)
        {
            case DrawerState.Drawing:
                if (Input.GetMouseButtonUp(0))
                {
                    _state = DrawerState.Waiting;
                }
                else
                {
                    Vector3 point = _mainCamera.ScreenToWorldPoint(mousePosition);
                    Vector2 point2D = new Vector2(point.x, point.y);

                    BranchPart parentBranch = null;

                    if (_drawnPoints[_currentIndex].Count <= 0 || Vector2.Distance(point2D, _drawnPoints[_currentIndex].Last()) > _drawerGap)
                    {
                        if (_drawnPoints[_currentIndex].Count <= 0 && _currentIndex > 0)
                        {
                            float distance = 9999f;
                            Vector2 pointValue = _drawnPoints[0][0];

                            for (int i = 0; i < _drawnPoints.Count; i++)
                            {
                                for (int j = 0; j < _drawnPoints[i].Count - 1; j++)
                                {
                                    if (Vector2.Distance(_drawnPoints[i][j], point2D) < distance)
                                    {
                                        distance = Vector2.Distance(_drawnPoints[i][j], point2D);
                                        pointValue = _drawnPoints[i][j];
                                        parentBranch = _currentIndex == 0 ? null : _branchesParts[i][j];
                                    }
                                }
                            }

                            _drawnPoints[_currentIndex].Add(pointValue);
                        }

                        _drawnPoints[_currentIndex].Add(point2D);

                        if (_currentIndex > 0 || (_currentIndex == 0 && _drawnPoints[_currentIndex].Count > 1))
                        {
                            int drawPointsNb = _drawnPoints[_currentIndex].Count;
                            Vector2 firstPoint = _drawnPoints[_currentIndex][drawPointsNb - 1];
                            Vector2 secondPoint = _drawnPoints[_currentIndex][drawPointsNb - 2];
                            Vector2 dir = (firstPoint - secondPoint).normalized;

                            int branchNb = _branchesParts[_currentIndex].Count;
                            BranchPart branchPart = new BranchPart(secondPoint, firstPoint, dir,
                                branchNb == 0 ? parentBranch : _branchesParts[_currentIndex][_branchesParts[_currentIndex].Count - 1],
                                _generator.gameObject);

                            _branchesParts[_currentIndex].Add(branchPart);

                            if (_branchesParts[_currentIndex].Count > 1)
                            {
                                BranchPart directParent = _branchesParts[_currentIndex][_branchesParts[_currentIndex].Count - 2];
                                directParent.ChildrenBranches.Add(branchPart);
                                branchPart.ParentObj = directParent.ParentObj;
                            }

                            if (parentBranch != null)
                            {
                                GameObject newObj = new GameObject("Branch");
                                branchPart.ParentObj = newObj;
                                newObj.transform.SetParent(parentBranch.ParentObj.transform);

                                branchPart.SplinePointRootParent = parentBranch.SplinePoint;
                                parentBranch.ChildrenBranches.Add(branchPart);
                            }

                            GameObject splinePt = new GameObject("Spline Point");
                            splinePt.transform.position = branchPart.SplinePointRootParent != null ? branchPart.SplinePointRootParent.transform.position : branchPart.StartingPoint;
                            splinePt.transform.SetParent(branchPart.ParentObj.transform);
                            branchPart.SplinePoint = splinePt;
                        }
                    }
                }
                break;

            case DrawerState.Moving:
                if (Input.GetMouseButtonUp(0))
                {
                    _state = DrawerState.Waiting;
                }
                else
                {
                    Vector3 point = _mainCamera.ScreenToWorldPoint(mousePosition);
                    Vector3 offset = point - _startingPoint;
                }
                break;

            case DrawerState.Waiting:
                //Initialize new list of plant for stem or branching + initialize drawing mode
                if (Input.GetMouseButtonDown(0))
                {
                    if (_drawnPoints[_currentIndex].Count > 0)
                    {
                        _drawnPoints.Add(new List<Vector2>());
                        _branchesParts.Add(new List<BranchPart>());
                        _currentIndex += 1;
                    }

                    Ray rayPoint = _mainCamera.ScreenPointToRay(mousePosition);
                    RaycastHit hit;

                    if (Physics.Raycast(rayPoint.origin, rayPoint.direction, out hit, float.MaxValue))
                    {
                        _startingPoint = _mainCamera.ScreenToWorldPoint(mousePosition);
                        _startingPoint = hit.point;
                        _state = DrawerState.Moving;
                    }
                    else
                    {
                        _state = DrawerState.Drawing;
                    }
                }
                break;
        }

        if (Input.GetKeyDown("space"))
        {
            Validate();
        }
    }

    private void Validate()
    {
        List<BranchPart> branchesToUse = new List<BranchPart>();

        for (int i = 0; i < _branchesParts.Count; i++)
        {
            for (int j = 0; j < _branchesParts[i].Count; j++)
            {
                branchesToUse.Add(_branchesParts[i][j]);
            }
        }

        _mainCamera.clearFlags = CameraClearFlags.Skybox;
        _generator.FormatIntoSplineBranches(branchesToUse);
        _generator.InitializeVisualGeneration();
    }

    private void OnDrawGizmos()
    {
        if (_drawnPoints != null)
        {
            Gizmos.color = Color.white;

            for (int i = 0; i < _drawnPoints.Count; i++)
            {
                _drawnPoints[i].ForEach(point =>
                {
                    Gizmos.DrawSphere(point, 0.01f);
                });
            }
        }
    }

    private void OnRenderObject()
    {
        if (_drawnPoints[0].Count <= 0) return;

        for (int i = 0; i < _drawnPoints.Count; i++)
        {
            if (_drawnPoints[i] != null)
            {
                GL.PushMatrix();
                GL.MultMatrix(transform.localToWorldMatrix);

                _lineMaterial.SetColor("white", Color.white);
                _lineMaterial.SetPass(0);

                GL.Begin(GL.LINES);

                for (int j = 0, k = _drawnPoints[i].Count - 1; j < k; j++)
                {
                    GL.Vertex(_drawnPoints[i][j]);
                    GL.Vertex(_drawnPoints[i][j + 1]);
                }

                GL.End();
                GL.PopMatrix();
            }
        }
    }
}