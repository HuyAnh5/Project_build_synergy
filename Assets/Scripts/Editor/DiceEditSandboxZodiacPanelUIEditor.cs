using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DiceEditSandboxZodiacPanelUI))]
public class DiceEditSandboxZodiacPanelUIEditor : Editor
{
    private SerializedProperty _zodiacSourceFolder;
    private SerializedProperty _zodiacOptions;
    private SerializedProperty _selectedZodiac;

    private SerializedProperty _zodiacNameText;
    private SerializedProperty _targetStatusText;
    private SerializedProperty _selectionRuleText;
    private SerializedProperty _useButton;
    private SerializedProperty _useButtonText;
    private SerializedProperty _useButtonBackground;
    private SerializedProperty _cancelButton;
    private SerializedProperty _cancelButtonText;
    private SerializedProperty _cancelButtonBackground;
    private SerializedProperty _autoUprightButton;
    private SerializedProperty _rollButton;
    private SerializedProperty _useEnabledColor;
    private SerializedProperty _useDisabledColor;

    private void OnEnable()
    {
        _zodiacSourceFolder = serializedObject.FindProperty("zodiacSourceFolder");
        _zodiacOptions = serializedObject.FindProperty("zodiacOptions");
        _selectedZodiac = serializedObject.FindProperty("selectedZodiac");

        _zodiacNameText = serializedObject.FindProperty("zodiacNameText");
        _targetStatusText = serializedObject.FindProperty("targetStatusText");
        _selectionRuleText = serializedObject.FindProperty("selectionRuleText");
        _useButton = serializedObject.FindProperty("useButton");
        _useButtonText = serializedObject.FindProperty("useButtonText");
        _useButtonBackground = serializedObject.FindProperty("useButtonBackground");
        _cancelButton = serializedObject.FindProperty("cancelButton");
        _cancelButtonText = serializedObject.FindProperty("cancelButtonText");
        _cancelButtonBackground = serializedObject.FindProperty("cancelButtonBackground");
        _autoUprightButton = serializedObject.FindProperty("autoUprightButton");
        _rollButton = serializedObject.FindProperty("rollButton");
        _useEnabledColor = serializedObject.FindProperty("useEnabledColor");
        _useDisabledColor = serializedObject.FindProperty("useDisabledColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Zodiac Source", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_zodiacSourceFolder, new GUIContent("Zodiac Source Folder"));

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh Zodiac List"))
                RefreshZodiacList();

            using (new EditorGUI.DisabledScope(_zodiacOptions.arraySize == 0))
            {
                if (GUILayout.Button("Clear List"))
                    ClearZodiacList();
            }
        }

        DrawSelectedZodiacPopup();
        DrawZodiacListPreview();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Bindings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_zodiacNameText);
        EditorGUILayout.PropertyField(_targetStatusText);
        EditorGUILayout.PropertyField(_selectionRuleText);
        EditorGUILayout.PropertyField(_useButton);
        EditorGUILayout.PropertyField(_useButtonText);
        EditorGUILayout.PropertyField(_useButtonBackground);
        EditorGUILayout.PropertyField(_cancelButton);
        EditorGUILayout.PropertyField(_cancelButtonText);
        EditorGUILayout.PropertyField(_cancelButtonBackground);
        EditorGUILayout.PropertyField(_autoUprightButton);
        EditorGUILayout.PropertyField(_rollButton);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_useEnabledColor);
        EditorGUILayout.PropertyField(_useDisabledColor);

        serializedObject.ApplyModifiedProperties();
    }

    private void RefreshZodiacList()
    {
        string folderPath = GetFolderPath(_zodiacSourceFolder.objectReferenceValue);
        if (string.IsNullOrEmpty(folderPath))
        {
            EditorUtility.DisplayDialog("Refresh Zodiac List", "Assign a valid project folder first.", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:ConsumableDataSO", new[] { folderPath });
        List<ConsumableDataSO> results = new List<ConsumableDataSO>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            ConsumableDataSO data = AssetDatabase.LoadAssetAtPath<ConsumableDataSO>(path);
            if (data != null && data.family == ConsumableFamily.Zodiac)
                results.Add(data);
        }

        results.Sort((a, b) => string.Compare(a.displayName, b.displayName, System.StringComparison.OrdinalIgnoreCase));

        _zodiacOptions.arraySize = results.Count;
        for (int i = 0; i < results.Count; i++)
            _zodiacOptions.GetArrayElementAtIndex(i).objectReferenceValue = results[i];

        if (results.Count == 0)
            _selectedZodiac.objectReferenceValue = null;
        else if (_selectedZodiac.objectReferenceValue == null || !results.Contains((ConsumableDataSO)_selectedZodiac.objectReferenceValue))
            _selectedZodiac.objectReferenceValue = results[0];
    }

    private void ClearZodiacList()
    {
        _zodiacOptions.ClearArray();
        _selectedZodiac.objectReferenceValue = null;
    }

    private void DrawSelectedZodiacPopup()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Selected Zodiac", EditorStyles.boldLabel);

        if (_zodiacOptions.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No Zodiac assets loaded. Drag a folder, then click Refresh Zodiac List.", MessageType.Info);
            EditorGUILayout.PropertyField(_selectedZodiac, new GUIContent("Selected Zodiac"));
            return;
        }

        string[] names = new string[_zodiacOptions.arraySize];
        int currentIndex = 0;
        Object current = _selectedZodiac.objectReferenceValue;

        for (int i = 0; i < _zodiacOptions.arraySize; i++)
        {
            Object option = _zodiacOptions.GetArrayElementAtIndex(i).objectReferenceValue;
            names[i] = option != null ? option.name : $"Missing {i}";
            if (option == current)
                currentIndex = i;
        }

        int nextIndex = EditorGUILayout.Popup("Selected Zodiac", currentIndex, names);
        _selectedZodiac.objectReferenceValue = _zodiacOptions.GetArrayElementAtIndex(nextIndex).objectReferenceValue;
    }

    private void DrawZodiacListPreview()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Zodiac Options", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            for (int i = 0; i < _zodiacOptions.arraySize; i++)
                EditorGUILayout.PropertyField(_zodiacOptions.GetArrayElementAtIndex(i), new GUIContent($"Option {i + 1}"));
        }
    }

    private static string GetFolderPath(Object sourceFolder)
    {
        if (sourceFolder == null)
            return null;

        string path = AssetDatabase.GetAssetPath(sourceFolder);
        if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
            return null;

        return path;
    }
}
