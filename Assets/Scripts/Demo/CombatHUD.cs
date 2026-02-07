using TMPro;
using UnityEngine;

public class CombatHUD : MonoBehaviour
{
    public CombatActor player;
    public CombatActor enemy;

    [Header("Player UI")]
    public TMP_Text playerHpText;
    public TMP_Text playerFocusText;

    [Header("Enemy UI")]
    public TMP_Text enemyHpText;
    public TMP_Text enemyFocusText;

    void Update()
    {
        if (player)
        {
            if (playerHpText) playerHpText.text = $"HP: {player.hp}/{player.maxHP}";
            if (playerFocusText) playerFocusText.text = $"Focus: {player.focus}/{player.maxFocus}";
        }

        if (enemy)
        {
            if (enemyHpText) enemyHpText.text = $"HP: {enemy.hp}/{enemy.maxHP}";
            if (enemyFocusText) enemyFocusText.text = $"Focus: {enemy.focus}/{enemy.maxFocus}";
        }
    }
}
