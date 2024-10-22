using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceColonization : MonoBehaviour
{
    [Header("Stem & Branches components")]
    private float _stemLength = 0.7f;
    private BranchPart _stem;
    [SerializeField] private float _branchPartLength = 0.7f;
    private List<BranchPart> _branchesParts = new List<BranchPart>();
    private List<BranchPart> _extremityBranchesParts = new List<BranchPart>();

    [Header("Attraction points")]
    [SerializeField] private List<Vector3> _attractionPoints = new List<Vector3>();
    [SerializeField] private List<int> _activeAttractors = new List<int>();
    private float _attractionRange = 7f;
    private float _killRange = 1.4f;//Must be inferior to attractionRange but higher branchLength
    private int _nbAttractionPoints = 200;
    private int _radiusAttractionPointsContainer = 5;

    [Header("Generator")]
    [SerializeField] private VisualGenerator _generator;

    // Start is called before the first frame update
    void Start()
    {
        CreateAttractionPoints();

        InitializeBranches();
    }

    // Update is called once per frame
    void Update()
    {
        Colonization();
    }

    void CreateAttractionPoints()
    {
        for(int i = 0; i < _nbAttractionPoints; i++)
        {
            _attractionPoints.Add(new Vector3(0, 10, 0) + Random.insideUnitSphere * _radiusAttractionPointsContainer);
        }
    }

    void InitializeBranches()
    {
        _stem = new BranchPart(new Vector3(0, 0, 0), new Vector3(0, _stemLength, 0), Vector3.up, null, _generator.gameObject);
        _branchesParts.Add(_stem);
        _extremityBranchesParts.Add(_stem);
    }

    public void Colonization()
    {
        if (_attractionPoints.Count > 0)
        {
            foreach(BranchPart b in _extremityBranchesParts)
            {
                b.HasGrown = true;
            }

            for(int i = _attractionPoints.Count - 1; i >= 0; i--)
            {
                foreach(BranchPart b in _branchesParts)
                {
                    if(Vector3.Distance(b.EndingPoint, _attractionPoints[i]) < _killRange)
                    {
                        _attractionPoints.Remove(_attractionPoints[i]);
                        _nbAttractionPoints--;
                        break;
                    }
                }
            }

            if(_attractionPoints.Count > 0)
            {
                _activeAttractors.Clear();

                foreach(BranchPart b in _branchesParts)
                {
                    b.AttractionPoints.Clear();
                }

                int counter = 0;

                foreach (Vector3 point in _attractionPoints)
                {
                    float minDistance = 99999f;

                    BranchPart closestBranchPart = null;

                    foreach(BranchPart b in _branchesParts)
                    {
                        float currentDistance = Vector3.Distance(b.EndingPoint, point);

                        if(currentDistance < _attractionRange && currentDistance < minDistance)
                        {
                            minDistance = currentDistance;
                            closestBranchPart = b;
                        }
                    }

                    if(closestBranchPart != null)
                    {
                        closestBranchPart.AttractionPoints.Add(point);
                        _activeAttractors.Add(counter);
                    }

                    counter++;
                }

                if(_activeAttractors.Count != 0)
                {
                    _extremityBranchesParts.Clear();

                    List<BranchPart> newBranchesParts = new List<BranchPart>();

                    foreach(BranchPart b in _branchesParts)
                    {
                        if(b.AttractionPoints.Count > 0)
                        {
                            Vector3 dir = Vector3.zero;

                            foreach(Vector3 pointPos in b.AttractionPoints)
                            {
                                dir += (pointPos - b.EndingPoint).normalized;
                            }

                            dir /= b.AttractionPoints.Count;
                            dir.Normalize();

                            if(b.AttractionPoints.Count > 0 && Vector3.Distance(b.EndingPoint, b.AttractionPoints[0]) < Vector3.Distance(b.EndingPoint + dir * _branchPartLength, b.AttractionPoints[0]))
                            {
                                dir = (b.AttractionPoints[0] - b.EndingPoint).normalized;
                            }

                            BranchPart branchPart = new BranchPart(b.EndingPoint, b.EndingPoint + dir * _branchPartLength, dir, b);
                            b.ChildrenBranches.Add(branchPart);
                            newBranchesParts.Add(branchPart);
                            _extremityBranchesParts.Add(branchPart);
                        }
                        else
                        {
                            if(b.ChildrenBranches.Count == 0)
                            {
                                _extremityBranchesParts.Add(b);
                            }
                        }
                    }

                    _branchesParts.AddRange(newBranchesParts);
                }
                else
                {
                    for(int i = 0; i < _extremityBranchesParts.Count; i++)
                    {
                        BranchPart extremityBranchPart = _extremityBranchesParts[i];

                        Vector3 start = extremityBranchPart.EndingPoint;
                        Vector3 dir = extremityBranchPart.Direction;
                        Vector3 ending = extremityBranchPart.EndingPoint + dir * _branchPartLength;

                        BranchPart newBranchPart = new BranchPart(start, ending, dir, extremityBranchPart);
                        extremityBranchPart.ChildrenBranches.Add(newBranchPart);
                        _branchesParts.Add(newBranchPart);
                        _extremityBranchesParts[i] = newBranchPart;
                    }
                }

                GenerateObjectsHierarchy();
            }

            if (_attractionPoints.Count <= 0)
            {
                _generator.FormatIntoSplineBranches(_branchesParts);
                _generator.InitializeVisualGeneration();
            }
        }
    }

    private void GenerateObjectsHierarchy()
    {
        foreach (BranchPart b in _branchesParts)
        {
            if (b.ChildrenBranches.Count > 1)
            {
                b.ChildrenBranches[0].ParentObj = b.ParentObj;

                for (int i = 1; i < b.ChildrenBranches.Count; i++)
                {
                    if (b.ChildrenBranches[i].SplinePointRootParent == null && b.SplinePoint != null) b.ChildrenBranches[i].SplinePointRootParent = b.SplinePoint;

                    if (b.ChildrenBranches[i].ParentObj != null) continue;

                    GameObject branchObj = new GameObject("Branch");
                    b.ChildrenBranches[i].ParentObj = branchObj;
                    branchObj.transform.SetParent(b.ParentObj.transform);
                }
            }
            else
            {
                if (b.ChildrenBranches.Count == 1)
                {
                    b.ChildrenBranches[0].ParentObj = b.ParentObj;
                }
            }

            if (b.ParentObj == null) Debug.LogError("No obj");

            if (b.SplinePoint != null) continue;

            GameObject splinePt = new GameObject("Spline Point");
            splinePt.transform.SetParent(b.ParentObj.transform);
            splinePt.transform.position = b.SplinePointRootParent != null ? b.SplinePointRootParent.transform.position : b.StartingPoint;
            b.SplinePoint = splinePt;
        }
    }
}