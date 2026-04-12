public static class DiceWholeDieTagUtility
{
    public static string GetDisplayName(DiceWholeDieTag tag)
    {
        switch (tag)
        {
            case DiceWholeDieTag.Patina:
                return "Patina";
            default:
                return "None";
        }
    }
}
