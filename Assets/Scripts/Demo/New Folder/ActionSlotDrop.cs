using System;
using System.Reflection;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActionSlotDrop : MonoBehaviour, IDropHandler
{
    public TurnManager turn;
    public int slotIndex = 1; // 1..3

    [Header("UI")]
    public Image iconPreview;

    // Cache reflection lookups so we don't allocate every drop.
    private static MethodInfo _miAssignDamage;
    private static MethodInfo _miAssignBuffDebuff;

    private void Awake()
    {
        if (turn) turn.RegisterDrop(this);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!turn || !turn.IsPlanning) return;

        var drag = eventData.pointerDrag;
        if (!drag) return;

        var d = drag.GetComponent<DraggableSkillIcon>();
        if (!d) return;

        // 1) Legacy path (unchanged)
        if (d.TryGetLegacySkill(out var legacy) && legacy != null)
        {
            bool ok = turn.TryAssignSkillToSlot(slotIndex, legacy);
            if (!ok) return;

            Punch();
            return;
        }

        // 2) V2 active skills: Damage / BuffDebuff
        //    NOTE: TurnManager overloads are introduced in later batches.
        //    Here we *optionally* route to them if they exist, without breaking current scenes.

        bool okV2 = false;

        if (d.TryGetV2Damage(out var dmg) && dmg != null)
            okV2 = TryInvokeAssignOverload(turn, slotIndex, dmg, ref _miAssignDamage);
        else if (d.TryGetV2BuffDebuff(out var bd) && bd != null)
            okV2 = TryInvokeAssignOverload(turn, slotIndex, bd, ref _miAssignBuffDebuff);
        else
        {
            // Passive (or none) => reject
            return;
        }

        if (!okV2) return;
        Punch();
    }

    private void Punch()
    {
        transform.DOKill();
        transform.DOPunchScale(Vector3.one * 0.12f, 0.18f, 8, 0.8f);
    }

    private static bool TryInvokeAssignOverload<TSkill>(TurnManager tm, int slotIndex1Based, TSkill skill, ref MethodInfo cached)
        where TSkill : ScriptableObject
    {
        if (tm == null || skill == null) return false;

        // Preferred method name in later refactor.
        // We also try a couple of reasonable alternatives to reduce churn.
        if (cached == null)
        {
            var t = tm.GetType();
            cached =
                t.GetMethod("TryAssignSkillToSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new Type[] { typeof(int), typeof(TSkill) }, null)
                ?? t.GetMethod("TryAssignActiveSkillToSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new Type[] { typeof(int), typeof(TSkill) }, null)
                ?? t.GetMethod("TryAssignToSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new Type[] { typeof(int), typeof(TSkill) }, null);
        }

        if (cached == null) return false;

        try
        {
            var ret = cached.Invoke(tm, new object[] { slotIndex1Based, skill });
            return ret is bool b && b;
        }
        catch
        {
            // Silent fail: we don't want to spam logs during refactor.
            return false;
        }
    }

    // ---------------------------
    // Preview API (legacy + future V2)
    // ---------------------------

    public void SetPreview(SkillSO skill)
    {
        SetPreviewSprite(skill != null ? skill.icon : null);
    }

    public void SetPreview(SkillDamageSO skill)
    {
        SetPreviewSprite(skill != null ? skill.icon : null);
    }

    public void SetPreview(SkillBuffDebuffSO skill)
    {
        SetPreviewSprite(skill != null ? skill.icon : null);
    }

    public void SetPreviewSprite(Sprite sprite)
    {
        if (!iconPreview) return;
        iconPreview.sprite = sprite;
        iconPreview.enabled = (sprite != null);
        iconPreview.raycastTarget = (sprite != null);
    }

    public void ClearPreview()
    {
        SetPreviewSprite(null);
    }
}
