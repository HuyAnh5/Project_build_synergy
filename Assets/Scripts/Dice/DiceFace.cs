using System;
using UnityEngine;
using TMPro;

[Serializable]
public struct DiceFace
{
    public int value;
    public Vector3 localEuler;
    public DiceFaceEnchantKind enchant;
    public TMP_Text faceValueText3D;
}
