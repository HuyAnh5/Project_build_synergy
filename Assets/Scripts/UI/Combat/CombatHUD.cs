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

    private int _lastHp = int.MinValue;
    private int _lastMaxHp = int.MinValue;
    private int _lastFocus = int.MinValue;
    private int _lastMaxFocus = int.MinValue;
    private int _lastGuard = int.MinValue;
    private string _lastStatusText = null;

    void Awake()
    {
        TryResolveRefs();
    }

    void Update()
    {
        if (!player) TryResolveRefs();
        if (!player) return;

        if (playerHpText && (_lastHp != player.hp || _lastMaxHp != player.maxHP))
        {
            _lastHp = player.hp;
            _lastMaxHp = player.maxHP;
            playerHpText.text = $"HP: {player.hp}/{player.maxHP}";
        }

        if (playerFocusText && (_lastFocus != player.focus || _lastMaxFocus != player.maxFocus))
        {
            _lastFocus = player.focus;
            _lastMaxFocus = player.maxFocus;
            playerFocusText.text = $"Focus: {player.focus}/{player.maxFocus}";
        }

        if (playerGuardText && _lastGuard != player.guardPool)
        {
            _lastGuard = player.guardPool;
            playerGuardText.text = $"Guard: {player.guardPool}";
        }

        if (playerStatusText)
        {
            string nextStatus = BuildStatusString(player.status);
            if (_lastStatusText != nextStatus)
            {
                _lastStatusText = nextStatus;
                playerStatusText.text = nextStatus;
            }
        }
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

        if (st.burnStacks > 0) Add($"Burn:{st.burnStacks}");
        if (st.bleedStacks > 0) Add($"Bleed:{st.bleedStacks}");
        if (st.marked) Add("Mark");
        if (st.staggered) Add("Stagger");
        if (st.frozen) Add("Freeze");

        return sb.ToString();
    }
}
