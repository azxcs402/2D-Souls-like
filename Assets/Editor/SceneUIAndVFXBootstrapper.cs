using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SceneUIAndVFXBootstrapper
{
    private const string PlayerHudName = "PlayerHUD";
    private const string UiInGamePath = "Canvas/UI_InGame";
    private const string SkillBarName = "UI_SkillBarParent";
    private const string DashSkillSlotName = "UI_SkillSlot";
    private const string LegacySkillBarName = "SkillBar";
    private const string HealthBarAssetPath = "Assets/Resources/UI/HealthBar.png";
    private const string HeartSpriteName = "health_bar_0";
    private const string BarSpriteName = "health_bar_1";
    private const string SkillFrameAssetPath = "Assets/Graphics/UI/AlexSkillUI/artdecoUI_PIPO_tr.png";
    private const string SkillFrameSpriteName = "artdecoUI_PIPO_tr_14";
    private const string SkillBackdropAssetPath = "Assets/Graphics/UI/AlexSkillUI/Background.png";
    private const string SkillBackdropSpriteName = "Cell01_0";
    private const string DashIconAssetPath = "Assets/Graphics/UI/AlexSkillUI/BG 6.png";
    private const string DashIconSpriteName = "BG 6_282";
    private const string OnHitVFXPrefabPath = "Assets/Prefabs/VFX/OnHitVFX.prefab";
    private const string OnHitVFXTemplateName = "OnHitVFX";
    private const string StaminaBarObjectName = "UI_PlayerStaminaBar";

    static SceneUIAndVFXBootstrapper()
    {
        EditorApplication.delayCall += EnsureSceneObjects;
        EditorSceneManager.sceneOpened += (_, _) => EditorApplication.delayCall += EnsureSceneObjects;
    }

    [MenuItem("Tools/Scene Setup/Create Player HUD And OnHitVFX Templates")]
    public static void CreateSceneObjectsFromMenu()
    {
        EnsureSceneObjects();
    }

    private static void EnsureSceneObjects()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return;
        }

        bool changed = false;
        changed |= EnsurePlayerHud();
        changed |= EnsureDashSkillSlot();
        changed |= EnsureEnemyOnHitVFXTemplates();

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
        }
    }

    private static bool EnsurePlayerHud()
    {
        Sprite heartSprite = LoadSprite(HeartSpriteName);
        Sprite barSprite = LoadSprite(BarSpriteName);
        int uiLayer = LayerMask.NameToLayer("UI");
        bool createdCanvas = false;
        GameObject canvasObject = GameObject.Find(PlayerHudName);
        if (canvasObject == null)
        {
            createdCanvas = true;
            canvasObject = CreateGameObject(PlayerHudName, uiLayer, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        }

        Canvas canvas = GetOrAddComponent<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;

        GetOrAddComponent<GraphicRaycaster>(canvasObject);

        bool changed = createdCanvas;
        changed |= EnsurePlayerHealthBar(canvasObject.transform as RectTransform, uiLayer, heartSprite, barSprite);
        changed |= EnsurePlayerStaminaBar(canvasObject.transform as RectTransform, uiLayer, barSprite);

        if (createdCanvas)
        {
            Selection.activeGameObject = canvasObject;
        }

        return changed;
    }

    private static bool EnsurePlayerHealthBar(RectTransform canvasRect, int uiLayer, Sprite heartSprite, Sprite barSprite)
    {
        if (canvasRect == null)
        {
            return false;
        }

        Transform existingHealthBar = canvasRect.Find("UI_PlayerHealthBar");
        if (existingHealthBar != null)
        {
            Slider existingSlider = existingHealthBar.GetComponent<Slider>();
            UI_PlayerHealthBar existingHealthBarComponent = GetOrAddComponent<UI_PlayerHealthBar>(existingHealthBar.gameObject);
            if (existingSlider != null)
            {
                existingHealthBarComponent.Configure(existingSlider);
            }

            return false;
        }

        GameObject healthBarObject = CreateGameObject("UI_PlayerHealthBar", uiLayer, typeof(RectTransform), typeof(Slider), typeof(UI_PlayerHealthBar));
        healthBarObject.transform.SetParent(canvasRect, false);

        RectTransform healthRect = healthBarObject.GetComponent<RectTransform>();
        healthRect.anchorMin = new Vector2(0f, 1f);
        healthRect.anchorMax = new Vector2(0f, 1f);
        healthRect.pivot = new Vector2(0f, 1f);
        healthRect.anchoredPosition = new Vector2(35f, -13f);
        healthRect.sizeDelta = new Vector2(260f, 72f);

        CreateImage("Heart", healthRect, uiLayer, heartSprite, Color.white, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(56f, 56f), new Vector2(28f, 0f), Image.Type.Simple);
        CreateImage("Bar Background", healthRect, uiLayer, null, new Color(0f, 0f, 0f, .55f), new Vector2(0f, .5f), new Vector2(1f, .5f), new Vector2(-72f, 26f), new Vector2(38f, 0f), Image.Type.Simple);

        RectTransform fillArea = CreateRect("Fill Area", healthRect, uiLayer, new Vector2(0f, .5f), new Vector2(1f, .5f), new Vector2(-78f, 18f), new Vector2(41f, 0f));
        Image fillImage = CreateImage("Fill", fillArea, uiLayer, null, new Color(.93f, .05f, .04f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Image.Type.Simple);
        Image borderImage = CreateImage("Border", healthRect, uiLayer, barSprite, Color.white, new Vector2(0f, .5f), new Vector2(1f, .5f), new Vector2(-64f, 34f), new Vector2(34f, 0f), Image.Type.Sliced);
        borderImage.pixelsPerUnitMultiplier = 12.5f;

        Slider slider = healthBarObject.GetComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;
        slider.targetGraphic = null;
        slider.fillRect = fillImage.rectTransform;
        slider.direction = Slider.Direction.LeftToRight;
        slider.wholeNumbers = true;

        UI_PlayerHealthBar playerHealthBar = healthBarObject.GetComponent<UI_PlayerHealthBar>();
        playerHealthBar.Configure(slider);
        EditorUtility.SetDirty(playerHealthBar);
        return true;
    }

    private static bool EnsurePlayerStaminaBar(RectTransform canvasRect, int uiLayer, Sprite barSprite)
    {
        if (canvasRect == null)
        {
            return false;
        }

        Transform existingBarTransform = canvasRect.Find(StaminaBarObjectName);
        if (existingBarTransform != null)
        {
            Slider existingSlider = existingBarTransform.GetComponent<Slider>();
            UI_PlayerStaminaBar existingStaminaBar = GetOrAddComponent<UI_PlayerStaminaBar>(existingBarTransform.gameObject);
            if (existingSlider != null)
            {
                existingStaminaBar.Configure(existingSlider);
            }

            return false;
        }

        DestroyChildIfExists(canvasRect, "UI_PlayerStaminaText");

        GameObject staminaObject = CreateGameObject(StaminaBarObjectName, uiLayer, typeof(RectTransform), typeof(Slider), typeof(UI_PlayerStaminaBar));
        staminaObject.transform.SetParent(canvasRect, false);

        RectTransform staminaRect = staminaObject.GetComponent<RectTransform>();
        staminaRect.anchorMin = new Vector2(0f, 1f);
        staminaRect.anchorMax = new Vector2(0f, 1f);
        staminaRect.pivot = new Vector2(0f, 1f);
        staminaRect.anchoredPosition = new Vector2(35f, -92f);
        staminaRect.sizeDelta = new Vector2(220f, 44f);

        CreateImage("Bar Background", staminaRect, uiLayer, null, new Color(0f, 0f, 0f, .45f), new Vector2(0f, .5f), new Vector2(1f, .5f), new Vector2(-64f, 18f), new Vector2(30f, 0f), Image.Type.Simple);
        RectTransform fillArea = CreateRect("Fill Area", staminaRect, uiLayer, new Vector2(0f, .5f), new Vector2(1f, .5f), new Vector2(-70f, 12f), new Vector2(30f, 0f));
        Image fillImage = CreateImage("Fill", fillArea, uiLayer, null, new Color(.25f, .82f, .42f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Image.Type.Simple);
        Image borderImage = CreateImage("Border", staminaRect, uiLayer, barSprite, Color.white, new Vector2(0f, .5f), new Vector2(1f, .5f), new Vector2(-54f, 24f), new Vector2(26f, 0f), Image.Type.Sliced);
        borderImage.pixelsPerUnitMultiplier = 12.5f;

        Slider slider = staminaObject.GetComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;
        slider.targetGraphic = null;
        slider.fillRect = fillImage.rectTransform;
        slider.direction = Slider.Direction.LeftToRight;
        slider.wholeNumbers = false;

        UI_PlayerStaminaBar staminaBarComponent = staminaObject.GetComponent<UI_PlayerStaminaBar>();
        staminaBarComponent.Configure(slider);
        EditorUtility.SetDirty(staminaBarComponent);
        return true;
    }

    private static bool EnsureDashSkillSlot()
    {
        GameObject uiInGame = GameObject.Find(UiInGamePath);
        if (uiInGame == null)
        {
            return false;
        }

        RectTransform uiInGameRect = uiInGame.GetComponent<RectTransform>();
        if (uiInGameRect == null)
        {
            return false;
        }

        Transform skillBarTransform = uiInGame.transform.Find(SkillBarName);
        if (skillBarTransform == null)
        {
            return false;
        }

        GameObject skillBarObject = skillBarTransform.gameObject;
        Transform existingSlot = skillBarObject.transform.Find(DashSkillSlotName);
        if (existingSlot == null)
        {
            return false;
        }

        GameObject slotObject = existingSlot.gameObject;
        slotObject.transform.SetParent(skillBarObject.transform, false);
        UI_DashCooldownImage dashSkillSlot = GetOrAddComponent<UI_DashCooldownImage>(slotObject);
        Transform cooldownImageTransform = slotObject.transform.Find("CooldownImage");
        Image cooldownImage = cooldownImageTransform != null ? cooldownImageTransform.GetComponent<Image>() : null;
        if (cooldownImage == null)
        {
            return false;
        }

        dashSkillSlot.Configure(cooldownImage);
        return false;
    }

    private static bool EnsureEnemyOnHitVFXTemplates()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OnHitVFXPrefabPath);
        if (prefab == null)
        {
            return false;
        }

        bool changed = false;
        Entity_VFX[] vfxComponents = Object.FindObjectsOfType<Entity_VFX>(true);
        foreach (Entity_VFX entityVFX in vfxComponents)
        {
            if (entityVFX == null || !IsEnemyVFX(entityVFX))
            {
                continue;
            }

            Transform existingTemplate = entityVFX.transform.Find(OnHitVFXTemplateName);
            GameObject templateObject = existingTemplate != null ? existingTemplate.gameObject : null;

            if (templateObject == null)
            {
                templateObject = PrefabUtility.InstantiatePrefab(prefab, entityVFX.transform) as GameObject;
                if (templateObject == null)
                {
                    continue;
                }

                templateObject.name = OnHitVFXTemplateName;
                templateObject.transform.localPosition = Vector3.zero;
                templateObject.transform.localRotation = Quaternion.identity;
                templateObject.transform.localScale = Vector3.one;
                templateObject.SetActive(false);
                changed = true;
            }

            SerializedObject serializedVFX = new SerializedObject(entityVFX);
            SerializedProperty hitVFX = serializedVFX.FindProperty("hitVFX");
            if (hitVFX != null && hitVFX.objectReferenceValue != templateObject)
            {
                hitVFX.objectReferenceValue = templateObject;
                serializedVFX.ApplyModifiedProperties();
                changed = true;
            }
        }

        return changed;
    }

    private static bool IsEnemyVFX(Entity_VFX entityVFX)
    {
        return entityVFX.GetComponent<Enemy>() != null
            || entityVFX.GetComponent<Enemy_Healthy>() != null
            || entityVFX.GetComponent<Enemy_Skeleton>() != null;
    }

    private static GameObject CreateGameObject(string objectName, int layer, params System.Type[] components)
    {
        GameObject gameObject = new GameObject(objectName, components);
        if (layer >= 0)
        {
            gameObject.layer = layer;
        }

        Undo.RegisterCreatedObjectUndo(gameObject, $"Create {objectName}");
        return gameObject;
    }

    private static Sprite LoadSprite(string spriteName)
    {
        return LoadSprite(HealthBarAssetPath, spriteName);
    }

    private static Sprite LoadSprite(string assetPath, string spriteName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite && sprite.name == spriteName)
            {
                return sprite;
            }
        }

        return null;
    }

    private static RectTransform GetOrCreateRect(string objectName, RectTransform parent, int layer)
    {
        Transform existingChild = parent.Find(objectName);
        if (existingChild != null && existingChild is RectTransform existingRect)
        {
            if (layer >= 0)
            {
                existingRect.gameObject.layer = layer;
            }

            return existingRect;
        }

        return CreateRect(objectName, parent, layer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private static Image GetOrCreateImage(string objectName, RectTransform parent, int layer)
    {
        RectTransform rect = GetOrCreateRect(objectName, parent, layer);
        return GetOrAddComponent<Image>(rect.gameObject);
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static void DestroyChildIfExists(RectTransform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    private static RectTransform CreateRect(
        string objectName,
        RectTransform parent,
        int layer,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 sizeDelta,
        Vector2 anchoredPosition)
    {
        GameObject rectObject = CreateGameObject(objectName, layer, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);

        RectTransform rect = rectObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;
        return rect;
    }

    private static Image CreateImage(
        string objectName,
        RectTransform parent,
        int layer,
        Sprite sprite,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 sizeDelta,
        Vector2 anchoredPosition,
        Image.Type imageType)
    {
        RectTransform rect = CreateRect(objectName, parent, layer, anchorMin, anchorMax, sizeDelta, anchoredPosition);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = imageType;
        image.raycastTarget = false;
        return image;
    }

}
