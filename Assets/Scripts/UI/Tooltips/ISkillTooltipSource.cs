using UnityEngine;

public interface ISkillTooltipSource
{
    bool TryGetSkillTooltip(out Canvas canvas, out RectTransform target, out ScriptableObject asset, out SkillRuntime runtime);
}
