using System;
using System.Collections.Generic;
using UnityEngine;

// Manages dice face selection state and highlight cleanup for the dice edit sandbox.
// It does not apply consumable effects; it only tracks what face/dice the UI is pointing at.
public partial class DiceEditSandboxController
{
    /// <summary>Updates the current face preview selection, including multi-select Zodiac and copy/paste modes.</summary>
    public void SetPreviewSelection(DiceSpinnerGeneric die, int logicalFaceIndex)
    {
        if (IsCopyPasteFaceMode())
        {
            SetCopyPasteSelection(die, logicalFaceIndex);
            return;
        }

        if (IsSandboxDropdownMode())
        {
            ToggleSandboxFaceSelection(die, logicalFaceIndex);
            return;
        }

        if (_selectedDie == die && _selectedLogicalFaceIndex == logicalFaceIndex)
        {
            _selectedDie = null;
            _selectedLogicalFaceIndex = -1;
            Debug.Log($"[DiceEditSelect] Deselected die='{die?.name}' faceValue={GetDisplayFaceValue(die, logicalFaceIndex)}");
            RefreshAllHighlights();
            RefreshUi();
            return;
        }

        _selectedDie = die;
        _selectedLogicalFaceIndex = logicalFaceIndex;
        Debug.Log($"[DiceEditSelect] Selected die='{die?.name}' faceValue={GetDisplayFaceValue(die, logicalFaceIndex)} value={GetFaceValue(die, logicalFaceIndex)}");
        RefreshAllHighlights();
        RefreshUi();
    }

    public bool IsCommittedSelection(DiceSpinnerGeneric die, int logicalFaceIndex)
    {
        return die == _committedDie &&
               logicalFaceIndex == _committedLogicalFaceIndex &&
               logicalFaceIndex >= 0;
    }

    public bool IsPreviewSelection(DiceSpinnerGeneric die, int logicalFaceIndex)
    {
        if (IsCopyPasteFaceMode())
            return false;

        if (IsSandboxDropdownMode())
            return die == _selectedDie &&
                   logicalFaceIndex >= 0 &&
                   _selectedLogicalFaceIndices.Contains(logicalFaceIndex);

        return die == _selectedDie &&
               logicalFaceIndex == _selectedLogicalFaceIndex &&
               logicalFaceIndex >= 0;
    }

    /// <summary>Promotes the current preview face to a committed single-face target.</summary>
    public void CommitCurrentSelection()
    {
        if (_selectedDie == null || _selectedLogicalFaceIndex < 0)
            return;

        _committedDie = _selectedDie;
        _committedLogicalFaceIndex = _selectedLogicalFaceIndex;
        RefreshAllHighlights();
        RefreshUi();
    }

    /// <summary>Clears all dice, face, and copy/paste selections owned by this sandbox.</summary>
    public void ClearSelection()
    {
        _selectedDie = null;
        _committedDie = null;
        _selectedLogicalFaceIndex = -1;
        _committedLogicalFaceIndex = -1;
        _selectedLogicalFaceIndices.Clear();
        ClearCopyPasteSelection();
        RefreshAllHighlights();
        RefreshUi();
    }

    public bool TryGetResolvedConsumableTarget(out DiceSpinnerGeneric die, out int faceIndex)
    {
        ResolveConsumableTarget(out die, out faceIndex);
        return die != null;
    }

    public string BuildResolvedTargetLabel()
    {
        if (IsCopyPasteFaceMode())
        {
            if (_copySourceDie == null || _copySourceLogicalFaceIndex < 0)
                return "Target: choose source face";

            if (_copyTargetDie == null || _copyTargetLogicalFaceIndex < 0)
                return $"Target: source {_copySourceDie.name} face {GetDisplayFaceValue(_copySourceDie, _copySourceLogicalFaceIndex)} | choose target face";

            return $"Target: {_copySourceDie.name} face {GetDisplayFaceValue(_copySourceDie, _copySourceLogicalFaceIndex)} -> {_copyTargetDie.name} face {GetDisplayFaceValue(_copyTargetDie, _copyTargetLogicalFaceIndex)}";
        }

        DiceSpinnerGeneric die = ResolveSelectedDie();
        if (die == null)
            return "Target: no dice selected";

        if (IsSandboxDropdownMode())
        {
            if (_selectedLogicalFaceIndices.Count <= 0)
                return $"Target: {die.name}";

            return $"Target: {die.name} | Faces: {BuildDisplayFaceList(die, _selectedLogicalFaceIndices)}";
        }

        ResolveConsumableTarget(out _, out int faceIndex);
        if (faceIndex >= 0)
            return $"Target: {die.name} face {GetDisplayFaceValue(die, faceIndex)}";

        return $"Target: {die.name}";
    }

    public int GetSandboxFaceSelectionLimit()
    {
        return GetSandboxFaceSelectionLimit(GetSelectedConsumableData());
    }

    public int GetSelectedFaceCount()
    {
        if (IsCopyPasteFaceMode())
            return (_copySourceLogicalFaceIndex >= 0 ? 1 : 0) + (_copyTargetLogicalFaceIndex >= 0 ? 1 : 0);

        if (IsSandboxDropdownMode())
            return _selectedLogicalFaceIndices.Count;

        return _selectedLogicalFaceIndex >= 0 ? 1 : 0;
    }

    public void CopySelectedFacesTo(List<int> buffer)
    {
        if (buffer == null)
            return;

        buffer.Clear();
        if (IsCopyPasteFaceMode())
        {
            return;
        }

        if (UsesExternalZodiacSandbox())
        {
            buffer.AddRange(_selectedLogicalFaceIndices);
            return;
        }

        if (_selectedLogicalFaceIndex >= 0)
            buffer.Add(_selectedLogicalFaceIndex);
    }

    public SandboxFaceHighlightKind GetHighlightKindForFace(DiceSpinnerGeneric die, int logicalFaceIndex)
    {
        if (IsCopyPasteFaceMode())
        {
            if (die == _copySourceDie && logicalFaceIndex == _copySourceLogicalFaceIndex)
                return SandboxFaceHighlightKind.CopySource;
            if (die == _copyTargetDie && logicalFaceIndex == _copyTargetLogicalFaceIndex)
                return SandboxFaceHighlightKind.CopyTarget;
            return SandboxFaceHighlightKind.None;
        }

        if (IsCommittedSelection(die, logicalFaceIndex))
            return SandboxFaceHighlightKind.Committed;
        if (IsPreviewSelection(die, logicalFaceIndex))
            return SandboxFaceHighlightKind.Preview;
        return SandboxFaceHighlightKind.None;
    }

    private void RefreshAllHighlights()
    {
        for (int i = 0; i < _interactables.Count; i++)
        {
            if (_interactables[i] != null)
                _interactables[i].RefreshHighlight();
        }
    }

    private void ForceClearAllHighlights()
    {
        for (int i = 0; i < _interactables.Count; i++)
        {
            if (_interactables[i] != null)
            {
                _interactables[i].ClearHighlight();
                DestroyHighlightChildren(_interactables[i].transform);
            }
        }
    }

    private static void DestroyAllHighlightObjectsGlobally()
    {
        Transform[] allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = allTransforms.Length - 1; i >= 0; i--)
        {
            Transform tr = allTransforms[i];
            if (tr == null || !tr.name.StartsWith("DiceFaceHighlight", StringComparison.Ordinal))
                continue;

            MeshFilter filter = tr.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                Mesh mesh = filter.sharedMesh;
                filter.sharedMesh = null;
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(mesh);
                else
                    UnityEngine.Object.DestroyImmediate(mesh);
            }

            MeshRenderer renderer = tr.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = false;

            tr.gameObject.SetActive(false);

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(tr.gameObject);
            else
                UnityEngine.Object.DestroyImmediate(tr.gameObject);
        }
    }

    private static void DestroyHighlightChildren(Transform root)
    {
        if (root == null)
            return;

        Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
        for (int i = allChildren.Length - 1; i >= 0; i--)
        {
            Transform child = allChildren[i];
            if (child == null || child == root)
                continue;

            if (!child.name.StartsWith("DiceFaceHighlight", StringComparison.Ordinal))
                continue;

            MeshFilter filter = child.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                Mesh mesh = filter.sharedMesh;
                filter.sharedMesh = null;
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(mesh);
                else
                    UnityEngine.Object.DestroyImmediate(mesh);
            }

            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = false;

            child.gameObject.SetActive(false);

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(child.gameObject);
            else
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }

    private DiceEditInteractable ResolvePrimaryInteractable()
    {
        if (_focusedInteractable != null)
            return _focusedInteractable;

        DiceSpinnerGeneric selectedDie = ResolveSelectedDie();
        if (selectedDie == null)
            return null;

        for (int i = 0; i < _interactables.Count; i++)
        {
            DiceEditInteractable interactable = _interactables[i];
            if (interactable == null)
                continue;

            DiceSpinnerGeneric spinner = interactable.GetComponent<DiceSpinnerGeneric>();
            if (spinner == selectedDie)
                return interactable;
        }

        return null;
    }

    private static int GetFaceValue(DiceSpinnerGeneric die, int logicalFaceIndex)
    {
        if (die == null || die.faces == null || logicalFaceIndex < 0 || logicalFaceIndex >= die.faces.Length)
            return 0;

        return die.faces[logicalFaceIndex].value;
    }

    private static string GetDisplayFaceValue(DiceSpinnerGeneric die, int logicalFaceIndex)
    {
        return GetFaceValue(die, logicalFaceIndex).ToString();
    }

    private static string BuildDisplayFaceList(DiceSpinnerGeneric die, IReadOnlyList<int> logicalFaceIndices)
    {
        if (logicalFaceIndices == null || logicalFaceIndices.Count == 0)
            return string.Empty;

        List<string> labels = new List<string>(logicalFaceIndices.Count);
        for (int i = 0; i < logicalFaceIndices.Count; i++)
            labels.Add(GetDisplayFaceValue(die, logicalFaceIndices[i]));

        return string.Join(", ", labels);
    }
}
