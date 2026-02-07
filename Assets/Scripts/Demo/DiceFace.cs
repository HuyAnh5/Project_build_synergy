using System;
using UnityEngine;

[Serializable]
public struct DiceFace
{
    public int value;            // giá trị gameplay (vd 1..20, hoặc tùy bạn)
    public Vector3 localEuler;   // rotation LOCAL để mặt này quay đúng hướng camera (Euler bạn đã chốt)
}
