using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Small UI factory helpers shared by the reward gacha demo partials.
public sealed partial class RewardGachaDemoController
{
    // Creates a colored panel and returns its RectTransform.
    private static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        Image image = MapPrototypeUIFactory.CreateImage(name, parent, color, false);
        return image.rectTransform;
    }

    // Creates a compact demo button with consistent sizing.
    private static Button CreateSmallButton(Transform parent, string label)
    {
        Button button = MapPrototypeUIFactory.CreateButton(label.Replace(" ", "") + "Button", parent, label, ButtonColor, TextColor, 16);
        MapPrototypeUIFactory.AddLayoutElement(button.gameObject, preferredWidth: 128f, preferredHeight: 42f);
        return button;
    }

    // Creates a pill-style label used for base gold and selected count.
    private static TextMeshProUGUI CreatePill(Transform parent, string text, Color color)
    {
        Image image = MapPrototypeUIFactory.CreateImage(text.Replace(" ", "") + "Pill", parent, color, false);
        MapPrototypeUIFactory.AddLayoutElement(image.gameObject, preferredWidth: 142f, preferredHeight: 34f);
        TextMeshProUGUI label = MapPrototypeUIFactory.CreateText("Label", image.transform, text, 14, FontStyles.Bold, TextColor, TextAlignmentOptions.Center);
        MapPrototypeUIFactory.Stretch(label.rectTransform, Vector2.zero, Vector2.zero);
        return label;
    }

    // Gets a component or adds it to the provided GameObject.
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    // Removes all children from a generated demo UI container.
    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }
}
