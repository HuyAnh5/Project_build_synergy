using UnityEngine;

public static class TurnManagerViewUtility
{
    public static void RefreshAllPreviews(ActionSlotDrop[] drops, SkillPlanBoard board)
    {
        for (int i = 0; i < 3; i++)
        {
            var d = GetDrop(drops, i);
            if (d == null || d.iconPreview == null) continue;

            d.ClearPreview();
            d.iconPreview.rectTransform.position = ((RectTransform)d.transform).position;
        }

        for (int a = 0; a < 3; a++)
        {
            if (!board.IsAnchorSlot(a)) continue;

            var d = GetDrop(drops, a);
            if (d == null || d.iconPreview == null) continue;

            var asset = board.GetCellSkillAsset(a);
            d.SetPreview(asset);
            d.iconPreview.rectTransform.position = GetGroupCenterWorldPos(drops, board, a);
        }
    }

    public static void UpdateAllIconsDim(SkillPlanBoard board)
    {
        var all = Object.FindObjectsOfType<DraggableSkillIcon>(true);
        foreach (var ic in all)
        {
            if (ic == null) continue;

            var asset = ic.GetSkillAsset();
            bool inUse = (asset != null) && board.IsSkillEquipped(asset);
            ic.SetInUse(inUse);
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

    private static Vector3 GetGroupCenterWorldPos(ActionSlotDrop[] drops, SkillPlanBoard board, int anchor0)
    {
        int sp = board.GetAnchorSpan(anchor0);
        if (sp <= 1) return GetDrop(drops, anchor0).transform.position;

        if (sp == 2)
        {
            Vector3 pA = GetDrop(drops, anchor0).transform.position;
            Vector3 pB = GetDrop(drops, anchor0 + 1).transform.position;
            return (pA + pB) * 0.5f;
        }

        return GetDrop(drops, 1).transform.position;
    }
}
