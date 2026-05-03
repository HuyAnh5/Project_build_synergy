using UnityEngine;

[CreateAssetMenu(menuName = "Game/Forge/Dice Color Ore", fileName = "Ore_")]
public sealed class DiceColorOreSO : ScriptableObject
{
    public Sprite icon;
    public string displayName;
    [TextArea(2, 4)]
    public string description;
    public DiceWholeDieTag targetTag = DiceWholeDieTag.Patina;
    public ContentRarity rarity = ContentRarity.Rare;
    public Color displayColor = new Color(0.45f, 0.72f, 0.62f, 1f);

    public string GetDisplayName()
    {
        return string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}
