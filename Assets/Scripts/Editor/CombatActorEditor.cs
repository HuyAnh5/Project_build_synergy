using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CombatActor))]
public class CombatActorEditor : Editor
{
    private SerializedProperty _teamProperty;
    private SerializedProperty _rowProperty;
    private SerializedProperty _isPlayerProperty;
    private SerializedProperty _maxHpProperty;
    private SerializedProperty _hpProperty;
    private SerializedProperty _maxFocusProperty;
    private SerializedProperty _focusProperty;
    private SerializedProperty _startingFocusProperty;
    private SerializedProperty _guardPoolProperty;
    private SerializedProperty _firePointProperty;
    private SerializedProperty _statusProperty;
    private SerializedProperty _worldUiTagProperty;
    private SerializedProperty _uiAnchorProperty;
    private SerializedProperty _autoSetupUiAnchorProperty;
    private SerializedProperty _uiAnchorNameProperty;

    private void OnEnable()
    {
        _teamProperty = serializedObject.FindProperty("team");
        _rowProperty = serializedObject.FindProperty("row");
        _isPlayerProperty = serializedObject.FindProperty("isPlayer");
        _maxHpProperty = serializedObject.FindProperty("maxHP");
        _hpProperty = serializedObject.FindProperty("hp");
        _maxFocusProperty = serializedObject.FindProperty("maxFocus");
        _focusProperty = serializedObject.FindProperty("focus");
        _startingFocusProperty = serializedObject.FindProperty("startingFocus");
        _guardPoolProperty = serializedObject.FindProperty("guardPool");
        _firePointProperty = serializedObject.FindProperty("firePoint");
        _statusProperty = serializedObject.FindProperty("status");
        _worldUiTagProperty = serializedObject.FindProperty("worldUiTag");
        _uiAnchorProperty = serializedObject.FindProperty("uiAnchor");
        _autoSetupUiAnchorProperty = serializedObject.FindProperty("autoSetupUiAnchor");
        _uiAnchorNameProperty = serializedObject.FindProperty("uiAnchorName");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_teamProperty);
        EditorGUILayout.PropertyField(_rowProperty);
        EditorGUILayout.PropertyField(_isPlayerProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_maxHpProperty);
        EditorGUILayout.PropertyField(_hpProperty);
        EditorGUILayout.PropertyField(_maxFocusProperty);
        EditorGUILayout.PropertyField(_focusProperty);
        EditorGUILayout.PropertyField(_startingFocusProperty);
        EditorGUILayout.PropertyField(_guardPoolProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Refs", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_firePointProperty);
        EditorGUILayout.PropertyField(_statusProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("World UI", EditorStyles.boldLabel);
        DrawWorldUiTagDropdown();
        EditorGUILayout.PropertyField(_uiAnchorProperty);
        EditorGUILayout.PropertyField(_autoSetupUiAnchorProperty);
        EditorGUILayout.PropertyField(_uiAnchorNameProperty);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawWorldUiTagDropdown()
    {
        List<string> tags = CollectWorldUiTags();
        string currentValue = string.IsNullOrWhiteSpace(_worldUiTagProperty.stringValue)
            ? CombatActor.DefaultWorldUiTag
            : _worldUiTagProperty.stringValue.Trim();

        if (!tags.Contains(currentValue))
            tags.Add(currentValue);

        tags.Sort(System.StringComparer.OrdinalIgnoreCase);

        int selectedIndex = Mathf.Max(0, tags.IndexOf(currentValue));
        int newIndex = EditorGUILayout.Popup(new GUIContent("World Ui Tag"), selectedIndex, tags.ToArray());
        _worldUiTagProperty.stringValue = tags[newIndex];
    }

    private static List<string> CollectWorldUiTags()
    {
        var tags = new List<string> { CombatActor.DefaultWorldUiTag };
        BattlePartyManager2D[] managers = Resources.FindObjectsOfTypeAll<BattlePartyManager2D>();
        for (int managerIndex = 0; managerIndex < managers.Length; managerIndex++)
        {
            BattlePartyManager2D manager = managers[managerIndex];
            if (manager == null || manager.worldUiPrefabs == null)
                continue;

            for (int i = 0; i < manager.worldUiPrefabs.Count; i++)
            {
                BattlePartyManager2D.WorldUiPrefabEntry entry = manager.worldUiPrefabs[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.tag))
                    continue;

                string tag = entry.tag.Trim();
                if (!tags.Contains(tag))
                    tags.Add(tag);
            }
        }

        return tags;
    }
}
