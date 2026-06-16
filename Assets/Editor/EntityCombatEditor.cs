using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Entity_Combat))]
public class EntityCombatEditor : Editor
{
    private void OnEnable()
    {
        EnsureHealthComponents();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DrawHealthSettings();

        EditorGUILayout.Space();
        if (GUILayout.Button("Open Combat Preview"))
        {
            CombatPreviewWindow.Open((Entity_Combat)target);
        }
    }

    private void EnsureHealthComponents()
    {
        foreach (Object inspectedTarget in targets)
        {
            Entity_Combat combat = inspectedTarget as Entity_Combat;
            if (combat == null || combat.GetComponent<Entity_Health>() != null)
            {
                continue;
            }

            if (combat.GetComponent<Enemy>() != null)
            {
                Undo.AddComponent<Enemy_Healthy>(combat.gameObject);
            }
            else
            {
                Undo.AddComponent<Entity_Health>(combat.gameObject);
            }
        }
    }

    private void DrawHealthSettings()
    {
        Entity_Combat combat = (Entity_Combat)target;
        if (combat == null)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Health Settings", EditorStyles.boldLabel);

        Enemy enemy = combat.GetComponent<Enemy>();
        Object healthOwner = enemy != null
            ? enemy
            : combat.GetComponent<Entity_Health>();

        if (healthOwner == null)
        {
            EditorGUILayout.HelpBox("No Entity_Health component found.", MessageType.Warning);
            return;
        }

        SerializedObject healthObject = new SerializedObject(healthOwner);
        healthObject.Update();

        SerializedProperty maxHealthProperty = healthObject.FindProperty("maxHealth");
        SerializedProperty canTakeDamageProperty = healthObject.FindProperty("canTakeDamage");

        if (maxHealthProperty != null)
        {
            maxHealthProperty.intValue = EditorGUILayout.IntSlider(
                "Max Health",
                maxHealthProperty.intValue,
                1,
                200
            );
        }

        if (canTakeDamageProperty != null)
        {
            EditorGUILayout.PropertyField(canTakeDamageProperty);
        }

        healthObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI()
    {
        Entity_Combat combat = (Entity_Combat)target;
        if (combat == null || !combat.ShowTargetCheckGizmos)
        {
            return;
        }

        DrawTargetCheck(combat, true);
    }

    [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Active)]
    private static void DrawAlwaysVisibleGizmos(Entity_Combat combat, GizmoType gizmoType)
    {
        if (combat == null
            || !combat.ShowTargetCheckGizmos)
        {
            return;
        }

        bool showForTargetCheckSelection = IsTargetCheckSelected(combat);
        if (!combat.AlwaysShowTargetCheckGizmos && !showForTargetCheckSelection)
        {
            return;
        }

        DrawTargetCheck(combat, showForTargetCheckSelection);
    }

    private static bool IsTargetCheckSelected(Entity_Combat combat)
    {
        Transform selectedTransform = Selection.activeTransform;
        return selectedTransform != null
            && combat.TargetCheck != null
            && (selectedTransform == combat.TargetCheck
                || selectedTransform.IsChildOf(combat.TargetCheck));
    }

    public static void DrawTargetCheck(Entity_Combat combat, bool showLabel)
    {
        if (combat.TargetCheck == null)
        {
            return;
        }

        Transform targetCheck = combat.TargetCheck;
        Vector3 center = targetCheck.position;
        float radius = combat.TargetCheckRadius;

        Handles.color = new Color(1f, 0f, 1f, .16f);
        Handles.DrawSolidDisc(center, Vector3.forward, radius);

        Handles.color = new Color(1f, 0f, 1f, 1f);
        Handles.DrawWireDisc(center, Vector3.forward, radius);
        Handles.DrawLine(combat.transform.position, center);

        if (!showLabel)
        {
            return;
        }

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal =
            {
                textColor = Color.magenta
            }
        };

        Handles.Label(
            center + Vector3.up * (radius + .12f),
            $"TargetCheck / Radius {radius:0.##}",
            labelStyle
        );
    }

    public static void DrawAttackData(Entity_Combat combat, Entity_AttackData attackData, string label, bool showLabel)
    {
        if (combat == null)
        {
            return;
        }

        Vector3 center = combat.GetAttackCenter(attackData);
        float radius = attackData.TargetCheckRadius;

        Handles.color = new Color(1f, .2f, 0f, .16f);
        Handles.DrawSolidDisc(center, Vector3.forward, radius);

        Handles.color = new Color(1f, .2f, 0f, 1f);
        Handles.DrawWireDisc(center, Vector3.forward, radius);
        Handles.DrawLine(combat.transform.position, center);

        if (!showLabel)
        {
            return;
        }

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal =
            {
                textColor = new Color(1f, .35f, 0f, 1f)
            }
        };

        Handles.Label(
            center + Vector3.up * (radius + .12f),
            $"{label} / Radius {radius:0.##}",
            labelStyle
        );
    }
}

public class CombatPreviewWindow : EditorWindow
{
    private Entity_Combat combat;
    private SerializedObject serializedCombat;
    private AnimationClip[] availableClips;
    private int selectedClipIndex;
    private bool previewActive;

    public static void Open(Entity_Combat initialCombat = null)
    {
        CombatPreviewWindow window = GetWindow<CombatPreviewWindow>("Combat Preview");
        window.SetCombat(initialCombat);
        window.Show();
    }

    [MenuItem("Tools/Combat Preview")]
    private static void OpenFromMenu()
    {
        Open(FindCombatFromSelection());
    }

    private void OnEnable()
    {
        Selection.selectionChanged += Repaint;
        if (combat == null)
        {
            SetCombat(FindCombatFromSelection());
        }
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= Repaint;
        SceneView.duringSceneGui -= DuringSceneGUI;
        StopPreview();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Select Player or Enemy_Skeleton root. Use this window instead of Unity's Animation window to preview an attack frame and TargetCheck range on the same Scene view.",
            MessageType.Info
        );

        Entity_Combat selectedCombat = FindCombatFromSelection();
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.ObjectField("Selected Combat", selectedCombat, typeof(Entity_Combat), true);
            if (GUILayout.Button("Use Selection", GUILayout.Width(110)))
            {
                SetCombat(selectedCombat);
            }
        }

        Entity_Combat newCombat = (Entity_Combat)EditorGUILayout.ObjectField(
            "Preview Combat",
            combat,
            typeof(Entity_Combat),
            true
        );

        if (newCombat != combat)
        {
            SetCombat(newCombat);
        }

        if (combat == null)
        {
            EditorGUILayout.HelpBox("No Entity_Combat selected.", MessageType.Warning);
            return;
        }

        serializedCombat ??= new SerializedObject(combat);
        serializedCombat.Update();

        EditorGUILayout.PropertyField(serializedCombat.FindProperty("showTargetCheckGizmos"));
        EditorGUILayout.PropertyField(serializedCombat.FindProperty("alwaysShowTargetCheckGizmos"));
        EditorGUILayout.PropertyField(serializedCombat.FindProperty("targetCheck"));
        EditorGUILayout.PropertyField(serializedCombat.FindProperty("targetCheckRadius"));
        EditorGUILayout.PropertyField(serializedCombat.FindProperty("whatIsTarget"));
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedCombat.FindProperty("enableCombatPreview"));
        EditorGUILayout.PropertyField(serializedCombat.FindProperty("showPreviewTargetCheckObject"));
        EditorGUILayout.PropertyField(serializedCombat.FindProperty("showPreviewAttackData"));
        bool previewEnabled = serializedCombat.FindProperty("enableCombatPreview").boolValue;

        RefreshClips();
        DrawClipSelector();

        SerializedProperty clipProperty = serializedCombat.FindProperty("previewAnimationClip");
        SerializedProperty timeProperty = serializedCombat.FindProperty("previewNormalizedTime");
        AnimationClip clip = clipProperty.objectReferenceValue as AnimationClip;

        using (new EditorGUI.DisabledScope(!previewEnabled || clip == null))
        {
            DrawFrameSelector(clip, timeProperty);

            DrawAttackEventButtons(clip, timeProperty);
        }

        serializedCombat.ApplyModifiedProperties();

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(!previewEnabled || clip == null))
            {
                if (GUILayout.Button("Sample Preview Frame"))
                {
                    SamplePreviewFrame();
                }
            }

            if (GUILayout.Button("Stop Preview"))
            {
                StopPreview();
            }
        }

        if (Event.current.type == EventType.Repaint && previewEnabled && clip != null)
        {
            SamplePreviewFrame();
        }

        SceneView.duringSceneGui -= DuringSceneGUI;
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void SetCombat(Entity_Combat newCombat)
    {
        if (combat == newCombat)
        {
            return;
        }

        StopPreview();
        combat = newCombat;
        serializedCombat = combat != null ? new SerializedObject(combat) : null;
        availableClips = null;
        selectedClipIndex = 0;
        Repaint();
    }

    private void RefreshClips()
    {
        if (combat == null)
        {
            availableClips = null;
            return;
        }

        Animator animator = combat.GetComponentInChildren<Animator>();
        RuntimeAnimatorController controller = animator != null ? animator.runtimeAnimatorController : null;
        availableClips = controller != null
            ? controller.animationClips.Where(clip => clip != null).Distinct().ToArray()
            : null;
    }

    private void DrawClipSelector()
    {
        SerializedProperty clipProperty = serializedCombat.FindProperty("previewAnimationClip");

        if (availableClips == null || availableClips.Length == 0)
        {
            EditorGUILayout.PropertyField(clipProperty);
            EditorGUILayout.HelpBox("No clips found from child Animator controller.", MessageType.Warning);
            return;
        }

        string[] clipNames = availableClips.Select(clip => clip.name).ToArray();
        AnimationClip currentClip = clipProperty.objectReferenceValue as AnimationClip;
        selectedClipIndex = Mathf.Max(0, System.Array.IndexOf(availableClips, currentClip));
        int newIndex = EditorGUILayout.Popup("Preview Animation Clip", selectedClipIndex, clipNames);
        selectedClipIndex = newIndex;
        clipProperty.objectReferenceValue = availableClips[selectedClipIndex];
    }

    private static void DrawFrameSelector(AnimationClip clip, SerializedProperty timeProperty)
    {
        int maxFrame = GetMaxFrame(clip);
        int currentFrame = NormalizedTimeToFrame(clip, timeProperty.floatValue);

        EditorGUILayout.LabelField(
            "Clip Frames",
            $"{maxFrame + 1} frames @ {clip.frameRate:0.##} FPS"
        );

        int newFrame = EditorGUILayout.IntSlider("Preview Frame", currentFrame, 0, maxFrame);
        if (newFrame != currentFrame)
        {
            timeProperty.floatValue = FrameToNormalizedTime(clip, newFrame);
        }
    }

    private void DrawAttackEventButtons(AnimationClip clip, SerializedProperty timeProperty)
    {
        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
        AnimationEvent[] attackEvents = events
            .Where(IsAttackEvent)
            .OrderBy(animationEvent => animationEvent.time)
            .ToArray();

        if (attackEvents.Length == 0)
        {
            EditorGUILayout.HelpBox("This clip has no AttackTrigger animation event.", MessageType.None);
            return;
        }

        EditorGUILayout.LabelField("Attack Event Frames", EditorStyles.boldLabel);
        foreach (AnimationEvent attackEvent in attackEvents)
        {
            int eventFrame = TimeToFrame(clip, attackEvent.time);
            string label = $"{attackEvent.functionName} @ Frame {eventFrame} ({attackEvent.time:0.###}s)";
            if (GUILayout.Button(label))
            {
                timeProperty.floatValue = FrameToNormalizedTime(clip, eventFrame);
            }
        }
    }

    private static int GetMaxFrame(AnimationClip clip)
    {
        if (clip == null)
        {
            return 0;
        }

        return Mathf.Max(0, Mathf.RoundToInt(clip.length * Mathf.Max(.01f, clip.frameRate)));
    }

    private static int NormalizedTimeToFrame(AnimationClip clip, float normalizedTime)
    {
        float clipLength = clip != null ? Mathf.Max(.01f, clip.length) : .01f;
        float time = Mathf.Clamp01(normalizedTime) * clipLength;
        return TimeToFrame(clip, time);
    }

    private static int TimeToFrame(AnimationClip clip, float time)
    {
        if (clip == null)
        {
            return 0;
        }

        float frameRate = Mathf.Max(.01f, clip.frameRate);
        return Mathf.Clamp(Mathf.RoundToInt(time * frameRate), 0, GetMaxFrame(clip));
    }

    private static float FrameToNormalizedTime(AnimationClip clip, int frame)
    {
        if (clip == null)
        {
            return 0f;
        }

        float clipLength = Mathf.Max(.01f, clip.length);
        float frameRate = Mathf.Max(.01f, clip.frameRate);
        float time = Mathf.Clamp(frame / frameRate, 0f, clipLength);
        return Mathf.Clamp01(time / clipLength);
    }

    private static bool IsAttackEvent(AnimationEvent animationEvent)
    {
        return animationEvent != null
            && animationEvent.functionName == "AttackTrigger";
    }

    private void SamplePreviewFrame()
    {
        if (combat == null || Application.isPlaying)
        {
            return;
        }

        AnimationClip clip = combat.PreviewAnimationClip;
        if (clip == null)
        {
            return;
        }

        Animator animator = combat.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            return;
        }

        if (!AnimationMode.InAnimationMode())
        {
            AnimationMode.StartAnimationMode();
            previewActive = true;
        }

        float time = Mathf.Clamp01(combat.PreviewNormalizedTime) * Mathf.Max(.01f, clip.length);
        AnimationMode.SampleAnimationClip(animator.gameObject, clip, time);
        SceneView.RepaintAll();
    }

    private void DuringSceneGUI(SceneView sceneView)
    {
        if (combat == null)
        {
            return;
        }

        if (combat.ShowPreviewTargetCheckObject)
        {
            EntityCombatEditor.DrawTargetCheck(combat, true);
        }

        if (!combat.ShowPreviewAttackData
            || !TryGetPreviewAttackData(out Entity_AttackData attackData, out string label))
        {
            return;
        }

        EntityCombatEditor.DrawAttackData(combat, attackData, label, true);
    }

    private bool TryGetPreviewAttackData(out Entity_AttackData attackData, out string label)
    {
        attackData = default;
        label = string.Empty;

        AnimationClip clip = combat != null ? combat.PreviewAnimationClip : null;
        if (combat == null || clip == null)
        {
            return false;
        }

        Player player = combat.GetComponent<Player>();
        if (player != null)
        {
            for (int i = 0; i < player.BasicAttackCount; i++)
            {
                if (clip.name == player.GetBasicAttackAnimationName(i))
                {
                    attackData = player.GetBasicAttackData(i);
                    label = $"Basic Attack {i + 1}";
                    return true;
                }
            }

            for (int i = 0; i < player.AirAttackCount; i++)
            {
                if (clip.name == player.GetAirAttackAnimationName(i))
                {
                    attackData = player.GetAirAttackData(i);
                    label = $"Air Attack {i + 1}";
                    return true;
                }
            }

            if (clip.name == player.FallAttackStartAnimationName
                || clip.name == player.FallAttackPerformed1AnimationName
                || clip.name == player.FallAttackPerformed2AnimationName)
            {
                attackData = player.FallAttackData;
                label = "Fall Attack";
                return true;
            }
        }

        Enemy_Skeleton skeleton = combat.GetComponent<Enemy_Skeleton>();
        if (skeleton != null && clip.name == "skeletonAttack")
        {
            attackData = skeleton.SkeletonAttackData;
            label = "Skeleton Attack";
            return true;
        }

        return false;
    }

    private void StopPreview()
    {
        if (!previewActive && !AnimationMode.InAnimationMode())
        {
            return;
        }

        AnimationMode.StopAnimationMode();
        previewActive = false;
        SceneView.RepaintAll();
    }

    private static Entity_Combat FindCombatFromSelection()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            return null;
        }

        Entity_Combat combat = selected.GetComponent<Entity_Combat>();
        if (combat != null)
        {
            return combat;
        }

        combat = selected.GetComponentInParent<Entity_Combat>();
        if (combat != null)
        {
            return combat;
        }

        return selected.GetComponentInChildren<Entity_Combat>();
    }
}
