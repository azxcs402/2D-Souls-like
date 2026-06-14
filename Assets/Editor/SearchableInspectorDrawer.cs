using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SearchableInspectorDrawer
{
    public static void DrawScriptField(SerializedObject serializedObject)
    {
        SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
        if (scriptProperty == null)
        {
            return;
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(scriptProperty);
        }
    }

    public static void DrawSearchBar(
        ref string searchQuery,
        ref bool showOnlyMatches,
        string prefsKey,
        string title,
        string[] quickFindLabels = null)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        searchQuery = EditorGUILayout.TextField("Search", searchQuery);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(prefsKey, searchQuery ?? string.Empty);
        }

        showOnlyMatches = EditorGUILayout.ToggleLeft("Show only matching fields", showOnlyMatches);

        int quickCount = quickFindLabels != null ? quickFindLabels.Length : 0;
        EditorGUILayout.LabelField($"{quickCount} quick find shortcut(s) available.");
        EditorGUILayout.EndVertical();
    }

    public static void DrawQuickFindButtons(
        Action<string> setSearch,
        params string[] labels)
    {
        if (labels == null || labels.Length == 0)
        {
            return;
        }

        EditorGUILayout.BeginHorizontal();
        foreach (string label in labels)
        {
            if (GUILayout.Button(label))
            {
                setSearch?.Invoke(label);
            }
        }

        if (GUILayout.Button("Clear"))
        {
            setSearch?.Invoke(string.Empty);
        }
        EditorGUILayout.EndHorizontal();
    }

    public static void DrawSections(
        SerializedObject serializedObject,
        IEnumerable<SearchableSection> sections,
        string searchQuery,
        bool showOnlyMatches)
    {
        bool hasSearch = !string.IsNullOrWhiteSpace(searchQuery);
        string normalizedSearch = hasSearch ? searchQuery.Trim().ToLowerInvariant() : string.Empty;

        foreach (SearchableSection section in sections)
        {
            List<SearchableField> visibleFields = section.Fields
                .Where(field => !hasSearch || Matches(field, normalizedSearch))
                .ToList();

            if (visibleFields.Count == 0 && hasSearch && showOnlyMatches)
            {
                continue;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(section.Title, EditorStyles.boldLabel);

            if (!string.IsNullOrWhiteSpace(section.Description))
            {
                EditorGUILayout.LabelField(section.Description, EditorStyles.miniLabel);
            }

            if (hasSearch && visibleFields.Count == 0)
            {
                EditorGUILayout.HelpBox("No matching fields in this section.", MessageType.None);
            }

            foreach (SearchableField field in section.Fields)
            {
                bool matches = !hasSearch || Matches(field, normalizedSearch);
                if (hasSearch && showOnlyMatches && !matches)
                {
                    continue;
                }

                DrawField(serializedObject, field, matches, searchQuery);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2f);
        }
    }

    public static int CountMatches(IEnumerable<SearchableSection> sections, string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            return sections.Sum(section => section.Fields.Length);
        }

        string normalizedQuery = searchQuery.Trim().ToLowerInvariant();
        return sections.Sum(section => section.Fields.Count(field => Matches(field, normalizedQuery)));
    }

    private static void DrawField(
        SerializedObject serializedObject,
        SearchableField field,
        bool highlight,
        string searchQuery)
    {
        SerializedProperty property = serializedObject.FindProperty(field.PropertyName);
        if (property == null)
        {
            return;
        }

        Color previousBackground = GUI.backgroundColor;
        if (highlight)
        {
            GUI.backgroundColor = new Color(.85f, .95f, 1f, 1f);
        }

        using (new EditorGUI.DisabledScope(field.Disabled))
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(property, new GUIContent(field.Label), true);

            if (highlight && !string.IsNullOrWhiteSpace(searchQuery))
            {
                EditorGUILayout.LabelField($"Matched by: {field.Keywords}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        GUI.backgroundColor = previousBackground;
    }

    private static bool Matches(SearchableField field, string normalizedQuery)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return true;
        }

        return field.PropertyName.ToLowerInvariant().Contains(normalizedQuery)
            || field.Label.ToLowerInvariant().Contains(normalizedQuery)
            || field.Keywords.ToLowerInvariant().Contains(normalizedQuery);
    }
}

public readonly struct SearchableSection
{
    public SearchableSection(string title, string description, params SearchableField[] fields)
    {
        Title = title;
        Description = description;
        Fields = fields;
    }

    public string Title { get; }
    public string Description { get; }
    public SearchableField[] Fields { get; }
}

public readonly struct SearchableField
{
    public SearchableField(string propertyName, string label, string keywords, bool disabled = false)
    {
        PropertyName = propertyName;
        Label = label;
        Keywords = keywords;
        Disabled = disabled;
    }

    public string PropertyName { get; }
    public string Label { get; }
    public string Keywords { get; }
    public bool Disabled { get; }
}
