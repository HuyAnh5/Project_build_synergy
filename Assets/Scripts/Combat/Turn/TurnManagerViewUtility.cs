using UnityEngine;

public static class TurnManagerViewUtility
{
    public static void RefreshAllPreviews(ActionSlotDrop[] drops, SkillPlanBoard board)
    {
        for (int i = 0; i < 3; i++)
        {
            var d = GetDrop(drops, i);
            if (d == null || d.iconPreview == null) continue;

            d.SetPreviewDetached(false);
            d.ClearPreview();
            d.iconPreview.rectTransform.position = ((RectTransform)d.transform).position;
        }

        for (int a = 0; a < 3; a++)
        {
            if (!board.IsAnchorSlot(a)) continue;

            var asset = board.GetCellSkillAsset(a);
            int span = board.GetAnchorSpan(a);

            var d = span == 3 ? GetDropByHomeSlotIndex(drops, 2) : GetDrop(drops, a);
            if (d == null || d.iconPreview == null) continue;

            d.SetPreview(asset);
            d.SetPreviewDetached(span == 3);
            d.iconPreview.rectTransform.position = GetGroupCenterWorldPos(drops, board, a);
        }
    }

    public static void UpdateAllIconsDim(SkillPlanBoard board, TurnManager turn)
    {
        var all = Object.FindObjectsOfType<DraggableSkillIcon>(true);
        foreach (var ic in all)
        {
            if (ic == null) continue;

            var asset = ic.GetSkillAsset();
            bool inUse = (asset != null) && board.IsSkillEquipped(asset);
            ic.SetInUse(inUse);
            bool castable = asset == null || asset is SkillPassiveSO || turn == null || turn.CanPrototypeCastSkillNow(asset);
            ic.SetCastable(castable);
        }
    }

    public static void UpdateAllDiceDim(TurnManager turn)
    {
        var all = Object.FindObjectsOfType<DiceDraggableUI>(true);
        foreach (var ui in all)
        {
            if (ui == null) continue;
            float alpha = 1f;
            if (turn != null && ui.dice != null && turn.IsDieSpentThisTurn(ui.dice))
                alpha = 0.5f;
            ui.SetRestingAlpha(alpha);
        }
    }

    public static void ShowEnemyIntentsImmediate(System.Collections.Generic.IReadOnlyList<CombatActor> enemies)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null) continue;
            var ui = e.GetComponentInChildren<ActorWorldUI>(true);
            if (ui != null) ui.ShowIntentImmediate();
        }
    }

    public static void FadeEnemyIntents(System.Collections.Generic.IReadOnlyList<CombatActor> enemies, float dur)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null) continue;
            var ui = e.GetComponentInChildren<ActorWorldUI>(true);
            if (ui != null) ui.FadeIntent(dur);
        }
    }

    public static void EnsureEnemyIntentsNow(System.Collections.Generic.IReadOnlyList<CombatActor> enemies, CombatActor player)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || e.IsDead) continue;

            var brain = e.GetComponent<EnemyBrainController>();
            if (brain != null && !brain.CurrentIntent.hasIntent)
                brain.DecideNextIntent(player);

            var ui = e.GetComponentInChildren<ActorWorldUI>(true);
            if (ui != null) ui.ShowIntentImmediate();
        }
    }

    private static ActionSlotDrop GetDrop(ActionSlotDrop[] drops, int i) => (i >= 0 && i < 3) ? drops[i] : null;

    private static ActionSlotDrop GetDropByHomeSlotIndex(ActionSlotDrop[] drops, int homeSlotIndex1Based)
    {
        if (drops == null)
            return null;

        for (int i = 0; i < drops.Length; i++)
        {
            ActionSlotDrop drop = drops[i];
            if (drop != null && drop.HomeSlotIndex == homeSlotIndex1Based)
                return drop;
        }

        return null;
    }

    private static Vector3 GetGroupCenterWorldPos(ActionSlotDrop[] drops, SkillPlanBoard board, int anchor0)
    {
        int sp = board.GetAnchorSpan(anchor0);
        ActionSlotDrop anchorDrop = GetDrop(drops, anchor0);
        if (sp <= 1)
            return anchorDrop != null ? anchorDrop.transform.position : Vector3.zero;

        if (sp == 2)
        {
            ActionSlotDrop dropA = GetDrop(drops, anchor0);
            ActionSlotDrop dropB = GetDrop(drops, anchor0 + 1);
            if (dropA == null && dropB == null)
                return anchorDrop != null ? anchorDrop.transform.position : Vector3.zero;
            if (dropA == null)
                return dropB.transform.position;
            if (dropB == null)
                return dropA.transform.position;

            Vector3 pA = dropA.transform.position;
            Vector3 pB = dropB.transform.position;
            return (pA + pB) * 0.5f;
        }

        ActionSlotDrop homeLeftDrop = GetDropByHomeSlotIndex(drops, 1);
        ActionSlotDrop homeRightDrop = GetDropByHomeSlotIndex(drops, 3);
        if (homeLeftDrop == null && homeRightDrop == null)
            return anchorDrop != null ? anchorDrop.transform.position : Vector3.zero;
        if (homeLeftDrop == null)
            return homeRightDrop.GetHomeWorldPosition();
        if (homeRightDrop == null)
            return homeLeftDrop.GetHomeWorldPosition();

        return (homeLeftDrop.GetHomeWorldPosition() + homeRightDrop.GetHomeWorldPosition()) * 0.5f;
    }
}
