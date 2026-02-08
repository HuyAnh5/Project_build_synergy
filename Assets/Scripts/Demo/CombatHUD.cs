using System.Text;
using TMPro;
using UnityEngine;

public class CombatHUD : MonoBehaviour
{
    [Header("Auto Resolve")]
    public BattlePartyManager2D party;
    public TurnManager turnManager;
    public CombatActor player;

    [Header("Player UI")]
    public TMP_Text playerHpText;
    public TMP_Text playerFocusText;
    public TMP_Text playerGuardText;
    public TMP_Text playerStatusText;

    void Awake()
    {
        TryResolveRefs();
    }

    void Update()
    {
        if (!player) TryResolveRefs();
        if (!player) return;

        if (playerHpText) playerHpText.text = $"HP: {player.hp}/{player.maxHP}";
        if (playerFocusText) playerFocusText.text = $"Focus: {player.focus}/{player.maxFocus}";
        if (playerGuardText) playerGuardText.text = $"Guard: {player.guardPool}";

        if (playerStatusText)
            playerStatusText.text = BuildStatusString(player.status);
    }

    void TryResolveRefs()
    {
        if (!party) party = FindObjectOfType<BattlePartyManager2D>(true);
        if (!turnManager) turnManager = FindObjectOfType<TurnManager>(true);

        // ưu tiên player từ Party (nếu bạn spawn player qua party)
        if (!player && party != null)
        {
            // nếu BattlePartyManager2D của bạn có field player public:
            if (party.Player != null) player = party.Player;

            // nếu sau này bạn đổi thành property Player:
            // if (party.Player != null) player = party.Player;
        }

        // fallback: player từ TurnManager
        if (!player && turnManager != null && turnManager.player != null)
            player = turnManager.player;

        // fallback cuối: tìm actor có name/tag tuỳ bạn
        if (!player)
        {
            var all = FindObjectsOfType<CombatActor>(true);
            foreach (var a in all)
            {
                if (!a) continue;
                if (a.name.ToLower().Contains("player"))
                {
                    player = a;
                    break;
                }
            }
        }
    }

    string BuildStatusString(StatusController st)
    {
        if (!st) return "";

        var sb = new StringBuilder(64);
        bool first = true;

        void Add(string s)
        {
            if (!first) sb.Append("  ");
            sb.Append(s);
            first = false;
        }

        if (st.burnStacks > 0 || st.burnTurns > 0) Add($"Burn:{st.burnStacks},{st.burnTurns}T");
        if (st.bleedTurns > 0) Add($"Bleed:{st.bleedTurns}");
        if (st.marked) Add("Mark");
        if (st.frozen) Add("Freeze");

        return sb.ToString();
    }
}
