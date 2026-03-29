using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ActorWorldUI : MonoBehaviour
{
    [Header("Bind")]
    public CombatActor actor;
    public TMP_Text intentText;
    public float intentFontSize = 3.5f;

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

        var brain = actor.GetComponent<EnemyBrainController>();
        if (intentText)
        {
            if (brain != null && brain.CurrentIntent.hasIntent)
                intentText.text = brain.CurrentIntent.previewText;
            else
                intentText.text = "";
        }
    }

    private string BuildStatusString(StatusController st)
    {
        if (!st) return "";

        // Format:
        // Freeze
        // Mark
        // Burn: Burn:{stack}
        // Bleed: Bleed:{stack}
        // Chilled: Chilled:{turn}T
        // Sleep:2T (Ailment)

        var sb = new StringBuilder(64);
        bool first = true;

        void AddLine(string s)
        {
            if (!first) sb.Append('\n');
            sb.Append(s);
            first = false;
        }

        if (st.frozen) AddLine("Freeze");
        if (st.marked) AddLine("Mark");
        if (st.staggered) AddLine("Stagger");

        // ✅ Burn: total stack only
        if (st.burnStacks > 0)
            AddLine($"Burn:{st.burnStacks}");

        // ✅ Bleed: stack
        if (st.bleedStacks > 0)
            AddLine($"Bleed:{st.bleedStacks}");

        // ✅ Chilled: turn
        if (st.chilledTurns > 0)
            AddLine($"Chilled:{st.chilledTurns}T");

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

        if (intentText == null)
            intentText = Create3DText("Intent_Text", new Vector3(0f, -0.45f * lineSpacing, 0f), intentFontSize, TextAlignmentOptions.Center);

        if (statusText == null)
            statusText = Create3DText("Status_Text", new Vector3(0f, -0.70f * lineSpacing, 0f), statusFontSize, TextAlignmentOptions.Center);
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

    public void ShowIntentImmediate()
    {
        if (intentText == null) return;
        intentText.DOKill();
        var c = intentText.color;
        c.a = 1f;
        intentText.color = c;
    }

    public void FadeIntent(float duration = 0.25f)
    {
        if (intentText == null) return;
        intentText.DOKill();
        intentText.DOFade(0f, duration).SetEase(Ease.OutQuad);
    }
}
