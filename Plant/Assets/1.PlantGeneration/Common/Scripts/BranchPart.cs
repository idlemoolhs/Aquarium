using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// ∑÷÷ß¿‡
/// </summary>
public class BranchPart
{
    //Branch part global components
    private int _id;
    private bool _isStem = false;
    public bool HasGrown { get; set; } = false;

    //Branch part positional & directional components
    public Vector3 Direction { get; set; }
    public Vector3 StartingPoint { get; set; }
    public Vector3 EndingPoint { get; set; }

    //Branch part size components
    private float _size;
    private float _endSize;

    //Branch part legacy components
    private BranchPart _parentBranch;
    public GameObject SplinePointRootParent = null;
    public List<BranchPart> ChildrenBranches = new List<BranchPart>();
    public List<Shape> Shapes = new List<Shape>();

    //Branch part physical components
    public GameObject ParentObj;
    public GameObject SplinePoint;

    //Branch space colonization algorithm components
    public List<Vector3> AttractionPoints = new List<Vector3>();

    public BranchPart(Vector3 startingPoint, Vector3 endingPoint, Vector3 direction, BranchPart parentBranch = null, GameObject parentObj = null)
    {
        StartingPoint = startingPoint;
        EndingPoint = endingPoint;
        Direction = direction;
        _parentBranch = parentBranch;
        if (parentBranch == null) _isStem = true;
        if (parentObj != null) ParentObj = parentObj;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
