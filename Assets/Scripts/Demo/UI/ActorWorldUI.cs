using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActorWorldUI : MonoBehaviour
{
    [Header("Bind")]
    public CombatActor actor;

    [Header("Follow")]
    public Vector3 localOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Texts (optional). If left empty, auto-create 3D TMP texts)")]
    public TMP_Text hpText;
    public TMP_Text guardText;
    public TMP_Text statusText;

    [Header("Render order (optional)")]
    public bool forceSorting = true;
    public int sortingOrder = 500;

    [Header("Text style")]
    public float hpFontSize = 4f;
    public float guardFontSize = 3.5f;
    public float statusFontSize = 3f;
    public float lineSpacing = 0.9f; // khoảng cách giữa các dòng

    public void Bind(CombatActor a)
    {
        actor = a;
        if (!actor)
        {
            gameObject.SetActive(false);
            return;
        }

        // attach to actor (ưu tiên uiAnchor nếu có)
        Transform anchor = actor.uiAnchor ? actor.uiAnchor : actor.transform;
        transform.SetParent(anchor, worldPositionStays: false);
        transform.localPosition = localOffset;

        // Nếu prefab có Canvas/UGUI thì phải disable raycast để không chặn click
        DisableAllGraphicRaycasts();

        // Nếu bạn chưa tạo text trong prefab, script sẽ tạo TextMeshPro (3D) luôn
        EnsureTextsExist_3D_TMP();

        if (forceSorting)
            ForceSortingOrder();

        Refresh();
    }

    void LateUpdate()
    {
        if (!actor)
        {
            Destroy(gameObject);
            return;
        }

        Refresh();
    }

    private void Refresh()
    {
        if (!actor) return;

        if (hpText) hpText.text = $"{actor.hp}/{actor.maxHP}";
        if (guardText) guardText.text = actor.guardPool > 0 ? $"Guard:{actor.guardPool}" : "Guard:0";
        if (statusText) statusText.text = BuildStatusString(actor.status);
    }

    private string BuildStatusString(StatusController st)
    {
        if (!st) return "";

        // format đúng yêu cầu:
        // Burn:2, 3T
        // Bleed:2
        // Mark
        // Freeze

        var sb = new StringBuilder(64);
        bool first = true;

        void AddLine(string s)
        {
            if (!first) sb.Append('\n');
            sb.Append(s);
            first = false;
        }

        if (st.burnStacks > 0 || st.burnTurns > 0)
            AddLine($"Burn:{st.burnStacks}, {st.burnTurns}T");

        if (st.bleedTurns > 0)
            AddLine($"Bleed:{st.bleedTurns}");

        if (st.marked)
            AddLine("Mark");

        if (st.frozen)
            AddLine("Freeze");

        return sb.ToString();
    }

    private void EnsureTextsExist_3D_TMP()
    {
        // Nếu bạn đã gán sẵn trong inspector thì giữ nguyên.
        // Nếu chưa có thì tự tạo 3D TextMeshPro (không Canvas).

        if (hpText == null)
            hpText = Create3DText("HP_Text", new Vector3(0f, 0f, 0f), hpFontSize, TextAlignmentOptions.Center);

        if (guardText == null)
            guardText = Create3DText("Guard_Text", new Vector3(0f, -0.25f * lineSpacing, 0f), guardFontSize, TextAlignmentOptions.Center);

        if (statusText == null)
            statusText = Create3DText("Status_Text", new Vector3(0f, -0.55f * lineSpacing, 0f), statusFontSize, TextAlignmentOptions.Center);
    }

    private TMP_Text Create3DText(string name, Vector3 localPos, float fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;

        var t = go.AddComponent<TextMeshPro>();
        t.text = "";
        t.fontSize = fontSize;
        t.alignment = align;
        t.enableWordWrapping = false;
        t.richText = false;

        // Gợi ý: nếu chữ quá to/nhỏ thì chỉnh SCALE của prefab world-ui (ví dụ 0.1 / 0.05)
        return t;
    }

    private void DisableAllGraphicRaycasts()
    {
        // Nếu prefab có UGUI (Canvas + TMP_Text (UGUI) + Image...) thì disable raycast để không chặn click
        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        var graphics = GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics)
            if (g) g.raycastTarget = false;
    }

    private void ForceSortingOrder()
    {
        // Case A: Canvas
        var canvas = GetComponentInChildren<Canvas>(true);
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            return;
        }

        // Case B: TextMeshPro 3D (MeshRenderer)
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
            r.sortingOrder = sortingOrder;
    }
}
