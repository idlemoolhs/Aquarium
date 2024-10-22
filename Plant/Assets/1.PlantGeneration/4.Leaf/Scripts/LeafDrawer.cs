using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LeafDrawer : MonoBehaviour
{
    [Header("Drawing components")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Material _lineMaterial;
    private float _drawerGap = 1.2f;

    private List<Vector2> _drawnPoints = new List<Vector2>();

    private DrawerState _state = DrawerState.Waiting;

    private Vector3 _startingPoint;

    [Header("Leaf builder")]
    [SerializeField] private LeafMeshBuilder _leafBuilder;

    // Start is called before the first frame update
    void Start()
    {
        if (_mainCamera == null) Debug.LogError("No camera");
        if (_lineMaterial == null) Debug.LogError("No material for drawing");
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

                    if (_drawnPoints.Count <= 0 || Vector2.Distance(point2D, _drawnPoints.Last()) > _drawerGap)
                    {
                        if (point2D.x <= 0) _drawnPoints.Add(point2D);
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
                    _drawnPoints.Clear();
                    _drawnPoints.Add(new Vector2(0, -20));

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
        if (_drawnPoints[_drawnPoints.Count - 1].x != 0) _drawnPoints.Add(new Vector2(0, _drawnPoints[_drawnPoints.Count - 1].y));

        _mainCamera.clearFlags = CameraClearFlags.Skybox;
        _leafBuilder.Build(_drawnPoints);
    }

    private void OnDrawGizmos()
    {
        if (_drawnPoints != null)
        {
            Gizmos.color = Color.white;

            _drawnPoints.ForEach(point =>
            {
                Gizmos.DrawSphere(point, 0.01f);
            });
        }
    }

    private void OnRenderObject()
    {
        if (_drawnPoints != null)
        {
            GL.PushMatrix();
            GL.MultMatrix(transform.localToWorldMatrix);

            _lineMaterial.SetColor("white", Color.white);
            _lineMaterial.SetPass(0);

            GL.Begin(GL.LINES);

            for (int j = 0, k = _drawnPoints.Count - 1; j < k; j++)
            {
                GL.Vertex(_drawnPoints[j]);
                GL.Vertex(_drawnPoints[j + 1]);

                GL.Vertex(new Vector3(-_drawnPoints[j].x, _drawnPoints[j].y, 0));
                GL.Vertex(new Vector3(-_drawnPoints[j + 1].x, _drawnPoints[j + 1].y, 0));
            }

            GL.Vertex(new Vector3(0, -50, 0));
            GL.Vertex(new Vector3(0, 50, 0));

            GL.End();
            GL.PopMatrix();
        }
    }
}