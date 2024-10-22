using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LSystemRule
{
    public char InitialFormula;
    public string ConvertedFormula;
}

[System.Serializable]
public class LSystemBranch
{
    public float Angle;
    public float BranchLength;
    public Vector3 Position;
    public List<BranchPart> BranchComponents = new List<BranchPart>();

    public LSystemBranch(Vector3 position, float angle, float branchLength)
    {
        Position = position;
        Angle = angle;
        BranchLength = branchLength;
    }

    public void GenerateObjectsHierarchy(VisualGenerator generator, BranchPart parentBranchPart = null)
    {
        Vector3 start = Position;
        Quaternion q = Quaternion.AngleAxis(Angle, Vector3.forward);
        Vector3 end = start + q * Vector3.up * BranchLength;
        Position = end;

        Vector2 dir = (start - end).normalized;

        BranchPart parentBranchForCreation = BranchComponents.Count == 0 ? parentBranchPart : BranchComponents[BranchComponents.Count - 1];
        BranchPart branchPart = new BranchPart(start, end, dir, parentBranchForCreation, generator.gameObject);
        BranchComponents.Add(branchPart);

        if (BranchComponents.Count > 1)
        {
            BranchPart directParent = BranchComponents[BranchComponents.Count - 2];
            directParent.ChildrenBranches.Add(branchPart);
            branchPart.ParentObj = directParent.ParentObj;
        }

        if (parentBranchPart != null)
        {
            GameObject newObj = new GameObject("Branch");
            branchPart.ParentObj = newObj;
            newObj.transform.SetParent(parentBranchPart.ParentObj.transform);

            branchPart.SplinePointRootParent = parentBranchPart.SplinePoint;
            parentBranchPart.ChildrenBranches.Add(branchPart);
        }

        GameObject splinePt = new GameObject("Spline Point");
        splinePt.transform.position = branchPart.SplinePointRootParent != null ? branchPart.SplinePointRootParent.transform.position : branchPart.StartingPoint;
        splinePt.transform.SetParent(branchPart.ParentObj.transform);
        branchPart.SplinePoint = splinePt;
    }
}

public class LSystem : MonoBehaviour
{
    [Header("Components linked with rules / formula")]
    [SerializeField] private string _initialFormula = "F";
    [SerializeField] private string _currentFormula = "F";
    [SerializeField] private List<LSystemRule> _lSystemRules = new List<LSystemRule>();

    [Header("Components linked with iterations")]
    [SerializeField] private int _currentIteration = 0;
    [SerializeField] private int _neededIterations = 3;

    [Header("Branches components")]
    [SerializeField] private float _branchAngle = 30f;
    [SerializeField] private float _branchLength = 0.7f;
    [SerializeField] private List<LSystemBranch> _lSystemBranches = new List<LSystemBranch>();

    [Header("Generator")]
    [SerializeField] private VisualGenerator _generator;
    private List<BranchPart> _branchesParts = new List<BranchPart>();

    // Start is called before the first frame update
    void Start()
    {
        _currentFormula = _initialFormula;

        if (_lSystemRules.Count < 0) Debug.LogError("No LSystem rules");
        if (_generator == null) Debug.LogError("No generator");

        InitializeLSystem();
    }

    private void InitializeLSystem()
    {
        for (int i = 0; i < _neededIterations; i++)
        {
            string newFormula = "";

            for (int j = 0; j < _currentFormula.Length; j++)
            {
                string currentCharacterFormula = _currentFormula[j].ToString();
                bool isConverted = false;

                for (int k = 0; k < _lSystemRules.Count; k++)
                {
                    if (currentCharacterFormula == _lSystemRules[k].InitialFormula.ToString())
                    {
                        isConverted = true;
                        newFormula += _lSystemRules[k].ConvertedFormula;
                        break;
                    }
                }

                if (!isConverted) newFormula += currentCharacterFormula;
            }

            _currentFormula = newFormula;
        }

        InterpretFormula();
    }

    private void InterpretFormula()
    {
        LSystemBranch lSystemBranch = new LSystemBranch(Vector3.zero, 0, _branchLength);
        _lSystemBranches.Add(lSystemBranch);

        for(int i = 0; i < _currentFormula.Length; i++)
        {
            int idLSystemBranch = _lSystemBranches.Count - 1;
            lSystemBranch = _lSystemBranches[idLSystemBranch];

            BranchPart parentBranchPart = null;
            if (idLSystemBranch > 0)
            {//We want the last hierarchical lsystem parent branch having branches part
                int idLastLSystemBranch = idLSystemBranch;

                for (int j = idLSystemBranch; j >= 0; j--)
                {
                    if (_lSystemBranches[j].BranchComponents.Count > 0)
                    {
                        idLastLSystemBranch = j;
                        break;
                    }
                }

                int lastParentBranchPartCompIndex = _lSystemBranches[idLastLSystemBranch].BranchComponents.Count - 1;
                if (lastParentBranchPartCompIndex >= 0) parentBranchPart = _lSystemBranches[idLastLSystemBranch].BranchComponents[lastParentBranchPartCompIndex];
            }

            int lastBranchComponentIndex = lSystemBranch.BranchComponents.Count - 1;
            Vector3 lastBranchComponentPosition = Vector3.zero;
            if (lastBranchComponentIndex >= 0) lastBranchComponentPosition = lSystemBranch.BranchComponents[lastBranchComponentIndex].EndingPoint;
            if (parentBranchPart != null) lastBranchComponentPosition = parentBranchPart.EndingPoint;

            char currentCharFormula = _currentFormula[i];
            switch (currentCharFormula)
            {
                case 'F':
                    lSystemBranch.GenerateObjectsHierarchy(_generator, lSystemBranch.BranchComponents.Count == 0 ? parentBranchPart : null);
                    _branchesParts.Add(lSystemBranch.BranchComponents[lSystemBranch.BranchComponents.Count - 1]);
                    break;
                case 'L': // Leaf
                    BranchPart parentBranchLeafShape = lSystemBranch.BranchComponents[lSystemBranch.BranchComponents.Count - 1];
                    Shape leaf = new Shape(parentBranchLeafShape, ShapeType.Leaf, Random.insideUnitSphere.normalized);
                    parentBranchLeafShape.Shapes.Add(leaf);
                    break;
                case '+':
                    lSystemBranch.Angle += _branchAngle;
                    break;
                case '-':
                    lSystemBranch.Angle -= _branchAngle;
                    break;
                case '[':
                    LSystemBranch newLSystemBranch = new LSystemBranch(lastBranchComponentPosition, 0, _branchLength);
                    _lSystemBranches.Add(newLSystemBranch);
                    break;
                case ']':
                    _lSystemBranches.RemoveAt(_lSystemBranches.Count - 1);
                    break;
            }
        }

        for (int i = 0; i < _branchesParts.Count; i++) //Order childs foreach branch
        {
            if (_branchesParts[i].ChildrenBranches.Count > 1)
            {
                int lastIndex = _branchesParts[i].ChildrenBranches.Count - 1;
                BranchPart directChildBranchPart = _branchesParts[i].ChildrenBranches[lastIndex];
                _branchesParts[i].ChildrenBranches.RemoveAt(lastIndex);
                _branchesParts[i].ChildrenBranches.Insert(0, directChildBranchPart);
            }
        }

        _generator.FormatIntoSplineBranches(_branchesParts);
        _generator.InitializeVisualGeneration();
    }
}
