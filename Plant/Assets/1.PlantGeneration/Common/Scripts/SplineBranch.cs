using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 以Spline控制分支的生长动画
/// </summary>
public class SplineBranch
{
    //Spline Components
    private int _id;
    public List<BranchPart> BranchesPart = new List<BranchPart>();
    public SplineBranch ParentSplineBranch;

    private List<Transform> _splinePoints = new List<Transform>();
    private List<Vector3> _splinePointsPositions = new List<Vector3>();

    public int IndexParentDivergence;
    public float RootRadius;
    public bool IsGenerated = false;
    public bool IsSenescent = false;

    public bool HasBeganGeneration = false;
    public float DelayBeforeGrowth = 0f;
    public float GrowthBranchPartDuration = 0f;
    public float ProgressionGrowth = 0f;
    public float ProgressionBranchPartGrowth = 0f;

    private float _totalLength;
    private List<float> _segmentLengths = new List<float>();

    private float _animationRotationSpeed = Random.Range(45f, 90f);

    //Catmull Rom Spline components
    public CatmullRomSpline Spline;
    public int Resolution = 10;

    public SplineBranch(int id)
    {
        _id = id;
    }

    public void Update()
    {
        if(Spline != null)
        {
            Spline.Update(_splinePoints.ToArray());
            Spline.Update(Resolution);
            AnimateSenescence();
            UpdateSplinePoints();
            Spline.DrawSpline();
        }
        else
        {
            if (_splinePoints.Count <= 3) return;
            Spline = new CatmullRomSpline(_splinePoints.ToArray(), Resolution);
        }
    }

    public void AddBranchPart(BranchPart branchPart)
    {
        BranchesPart.Add(branchPart);

        _splinePoints.Add(branchPart.SplinePoint.transform);
        _splinePointsPositions.Add(branchPart.SplinePoint.transform.position);

        if(_splinePointsPositions.Count > 1)
        {
            float distance = 0f;

            for(int i = 0; i < _splinePointsPositions.Count - 2; i++)
            {
                distance += Vector3.Distance(_splinePointsPositions[i], _splinePointsPositions[i + 1]);
            }

            _totalLength = distance;

            _segmentLengths.Add(Vector3.Distance(_splinePointsPositions[_splinePointsPositions.Count - 2], _splinePointsPositions[_splinePointsPositions.Count - 1]));
        }
    }

    public void AnimateGrowth(float timeElapsed, float totalTimeGrowth)
    {
        if(Spline != null && !IsSenescent)
        {
            int currentAnimatedPtIndex = System.Convert.ToInt32(Mathf.Ceil(timeElapsed / totalTimeGrowth * _splinePoints.Count));

            if (currentAnimatedPtIndex > _splinePoints.Count - 1) currentAnimatedPtIndex = _splinePoints.Count - 1;

            Vector3 position = _splinePointsPositions[currentAnimatedPtIndex];
            float rotationSpeed = _animationRotationSpeed;
            float rotationRadius = 0.0001f;

            Vector3 newPosition = new Vector3(
                position.x + Mathf.Cos(Time.time * rotationSpeed * Mathf.Deg2Rad) * (rotationRadius * currentAnimatedPtIndex),
                position.y,
                position.z + Mathf.Sin(Time.time * rotationSpeed * Mathf.Deg2Rad) * (rotationRadius * currentAnimatedPtIndex)
            );

            _splinePoints[currentAnimatedPtIndex].transform.position = newPosition;

            for(int i = 1; i <= currentAnimatedPtIndex - 1; i++)
            {
                Vector3 pos = _splinePointsPositions[i];
                float coeff = 1 / (float)(currentAnimatedPtIndex - 1);

                Vector3 newPos = new Vector3(
                    pos.x + Mathf.Cos(Time.time * rotationSpeed * Mathf.Deg2Rad) * rotationRadius * coeff,
                    pos.y,
                    pos.z + Mathf.Sin(Time.time * rotationSpeed * Mathf.Deg2Rad) * rotationRadius * coeff
                );

                _splinePoints[i].transform.position = newPos;
            }
        }
    }

    public void AnimateSenescence()
    {
        if (!IsSenescent) return;

        _splinePoints[_splinePoints.Count - 1].position += new Vector3(0, -0.01f, 0);
    }

    private void UpdateSplinePoints()
    {
        if(BranchesPart[0].SplinePointRootParent != null)
        {
            if(BranchesPart[0].SplinePointRootParent.transform.position != BranchesPart[0].SplinePoint.transform.position)
            {
                Vector3 newDir = BranchesPart[0].SplinePointRootParent.transform.position - BranchesPart[0].SplinePoint.transform.position;

                BranchesPart[0].SplinePoint.transform.position = BranchesPart[0].SplinePointRootParent.transform.position;

                for(int i = 1; i < BranchesPart.Count; i++)
                {
                    _splinePoints[i].transform.position += newDir;
                }
            }
        }

        for (int i = 0; i < _splinePoints.Count; i++)
        {
            if (_splinePointsPositions[i] != null && _splinePoints[i] != null && _splinePointsPositions[i] != _splinePoints[i].position)
            {
                if(IsSenescent)
                {
                    int movingPointIndex = i;

                    Vector3 displacement = _splinePoints[movingPointIndex].position - _splinePointsPositions[movingPointIndex];
                    float stiffness = 0.2f;

                    for (int k = 0; k < _splinePoints.Count; k++)
                    {
                        if (k != movingPointIndex)
                        {
                            Vector3 newPosition = _splinePointsPositions[k] + displacement;

                            if (k < _splinePoints.Count - 1)
                            {
                                Vector3 direction = (_splinePoints[k + 1].position - newPosition).normalized;
                                newPosition = _splinePoints[k + 1].position - direction * _segmentLengths[k];
                            }
                            else if (k == _splinePoints.Count - 1)
                            {
                                Vector3 direction = (newPosition - _splinePoints[k - 1].position).normalized;
                                newPosition = _splinePoints[k - 1].position + direction * _segmentLengths[k - 1];
                            }

                            _splinePoints[k].position = Vector3.Lerp(_splinePoints[k].position, newPosition, stiffness * Time.deltaTime);

                            if (k > 0)
                            {
                                BranchesPart[k - 1].EndingPoint = _splinePoints[k].position;
                            }

                            BranchesPart[k].StartingPoint = _splinePoints[k].position;
                        }
                    }
                }

                _splinePointsPositions[i] = _splinePoints[i].position;

                if(i > 0)
                {
                    BranchesPart[i - 1].EndingPoint = _splinePoints[i].position;
                }

                BranchesPart[i].StartingPoint = _splinePoints[i].position;
            }
        }
    }

    public void SetRootRadius(float rootRadius)
    {
        RootRadius = rootRadius;
    }
}
