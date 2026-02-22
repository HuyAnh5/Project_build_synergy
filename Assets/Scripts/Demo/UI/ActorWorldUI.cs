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

        Transform anchor = actor.uiAnchor ? actor.uiAnchor : actor.transform;
        transform.SetParent(anchor, worldPositionStays: false);
        transform.localPosition = localOffset;

        DisableAllGraphicRaycasts();
        EnsureTextsExist_3D_TMP();

        if (forceSorting)
            ForceSortingOrder(sortingOrder);

        Refresh();
    }

    private void LateUpdate()
    {
        if (!actor) return;
        Refresh();
    }

    void Refresh()
    {
        if (!actor) return;

        if (hpText) hpText.text = $"{actor.hp}/{actor.maxHP}";
        if (guardText) guardText.text = actor.guardPool > 0 ? $"Guard:{actor.guardPool}" : "Guard:0";
        if (statusText) statusText.text = BuildStatusString(actor.status);
    }

    private string BuildStatusString(StatusController st)
    {
        if (!st) return "";

        // format:
        // Burn:2, 3T
        // Bleed:2
        // Mark
        // Freeze
        // Sleep:2T (Ailment)

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

        // ✅ Ailment display
        if (st.HasAilment(out var at, out var left))
            AddLine($"{at}:{left}T");

        return sb.ToString();
    }

    private void EnsureTextsExist_3D_TMP()
    {
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
        go.transform.SetParent(transform, worldPositionStays: false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var t = go.AddComponent<TextMeshPro>();
        t.text = "";
        t.fontSize = fontSize;
        t.alignment = align;
        t.enableWordWrapping = false;

        return t;
    }

    private void DisableAllGraphicRaycasts()
    {
        var graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;
    }

    private void ForceSortingOrder(int order)
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = order;
        }
    }
}
