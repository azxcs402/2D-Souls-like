using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SceneUIAndVFXBootstrapper
{
    private const string MainMenuSceneName = "MainMenu";
    private const string PlayerHudName = "PlayerHUD";
    private const string UiInGameName = "UI_InGame";
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
    private const string HealingPotionIconAssetPath = "Assets/Graphics/UI/AlexSkillUI/BG 6.png";
    private const string HealingPotionIconSpriteName = "BG 6_148";
    private const string OnHitVFXPrefabPath = "Assets/Prefabs/VFX/OnHitVFX.prefab";
    private const string OnHitVFXTemplateName = "OnHitVFX";
    private const string StaminaBarObjectName = "UI_PlayerStaminaBar";
    private const string HealingPotionObjectName = "UI_HealingPotionSlot";

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

    [MenuItem("Tools/Scene Setup/Create Healing Potion Slot")]
    public static void CreateHealingPotionSlotFromMenu()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += CreateHealingPotionSlotFromMenu;
            return;
        }

        if (EnsureHealingPotionSkillSlot())
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }

    private static void EnsureSceneObjects()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += EnsureSceneObjects;
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return;
        }

        if (IsMainMenuScene(activeScene))
        {
            if (RemovePlayerHudIfExists())
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
            }

            return;
        }

        bool changed = false;
        changed |= EnsurePlayerHud();
        changed |= EnsureDashSkillSlot();
        changed |= EnsureHealingPotionSkillSlot();
        changed |= EnsureHealingPotionWorldIcon();
        changed |= EnsureEnemyOnHitVFXTemplates();

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
        }
    }

    private static bool IsMainMenuScene(Scene activeScene)
    {
        return activeScene.name == MainMenuSceneName;
    }

    private static bool RemovePlayerHudIfExists()
    {
        GameObject playerHud = GameObject.Find(PlayerHudName);
        if (playerHud == null)
        {
            return false;
        }

        Undo.DestroyObjectImmediate(playerHud);
        return true;
    }

    private static bool EnsurePlayerHud()
    {
        Sprite heartSprite = LoadSprite(HeartSpriteName);
        Sprite barSprite = LoadSprite(BarSpriteName);
        int uiLayer = LayerMask.NameToLayer("UI");
        bool createdCanvas = false;
        bool changed = createdCanvas;
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

        RectTransform canvasRect = canvasObject.transform as RectTransform;
        if (canvasRect != null)
        {
            Vector2 zero = Vector2.zero;
            Vector2 one = Vector2.one;
            Vector2 center = new Vector2(0.5f, 0.5f);

            if (canvasRect.anchorMin != zero)
            {
                canvasRect.anchorMin = zero;
                changed = true;
            }

            if (canvasRect.anchorMax != one)
            {
                canvasRect.anchorMax = one;
                changed = true;
            }

            if (canvasRect.offsetMin != zero)
            {
                canvasRect.offsetMin = zero;
                changed = true;
            }

            if (canvasRect.offsetMax != zero)
            {
                canvasRect.offsetMax = zero;
                changed = true;
            }

            if (canvasRect.pivot != center)
            {
                canvasRect.pivot = center;
                changed = true;
            }

            if (canvasRect.anchoredPosition != zero)
            {
                canvasRect.anchoredPosition = zero;
                changed = true;
            }

            if (canvasRect.localScale != Vector3.one)
            {
                canvasRect.localScale = Vector3.one;
                changed = true;
            }
        }

        changed |= EnsurePlayerHealthBar(canvasObject.transform as RectTransform, uiLayer, heartSprite, barSprite);
        changed |= EnsurePlayerStaminaBar(canvasObject.transform as RectTransform, uiLayer, barSprite);

        if (createdCanvas)
        {
            Selection.activeGameObject = canvasObject;
        }

        return changed;
    }

    private static bool EnsureUiInGame()
    {
        GameObject playerHud = GameObject.Find(PlayerHudName);
        if (playerHud == null)
        {
            return false;
        }

        GameObject uiInGame = GameObject.Find(UiInGameName);
        if (uiInGame == null)
        {
            return false;
        }

        bool changed = false;
        Transform targetParent = playerHud.transform;
        if (uiInGame.transform.parent != targetParent)
        {
            Undo.SetTransformParent(uiInGame.transform, targetParent, "Move UI_InGame Under PlayerHUD");
            changed = true;
        }

        RectTransform uiRect = uiInGame.GetComponent<RectTransform>();
        if (uiRect != null)
        {
            Vector2 stretchMin = Vector2.zero;
            Vector2 stretchMax = Vector2.one;
            Vector2 zero = Vector2.zero;
            Vector2 centerPivot = new Vector2(0.5f, 0.5f);

            if (uiRect.anchorMin != stretchMin)
            {
                uiRect.anchorMin = stretchMin;
                changed = true;
            }

            if (uiRect.anchorMax != stretchMax)
            {
                uiRect.anchorMax = stretchMax;
                changed = true;
            }

            if (uiRect.offsetMin != zero)
            {
                uiRect.offsetMin = zero;
                changed = true;
            }

            if (uiRect.offsetMax != zero)
            {
                uiRect.offsetMax = zero;
                changed = true;
            }

            if (uiRect.pivot != centerPivot)
            {
                uiRect.pivot = centerPivot;
                changed = true;
            }

            if (uiRect.anchoredPosition != zero)
            {
                uiRect.anchoredPosition = zero;
                changed = true;
            }

            if (uiRect.localScale != Vector3.one)
            {
                uiRect.localScale = Vector3.one;
                changed = true;
            }
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
        GameObject playerHud = GameObject.Find(PlayerHudName);
        if (playerHud == null)
        {
            return false;
        }

        Transform skillBarTransform = FindSkillBarTransform();
        if (skillBarTransform == null)
        {
            return false;
        }

        if (skillBarTransform.parent != playerHud.transform)
        {
            Undo.SetTransformParent(skillBarTransform, playerHud.transform, "Move UI_SkillBarParent Under PlayerHUD");
        }

        bool changed = EnsureSkillBarLayout(skillBarTransform as RectTransform);

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
        return changed;
    }

    private static bool EnsureHealingPotionSkillSlot()
    {
        GameObject playerHud = GameObject.Find(PlayerHudName);
        if (playerHud == null)
        {
            return false;
        }

        Transform skillBarTransform = FindSkillBarTransform();
        if (skillBarTransform == null)
        {
            return false;
        }

        if (skillBarTransform.parent != playerHud.transform)
        {
            Undo.SetTransformParent(skillBarTransform, playerHud.transform, "Move UI_SkillBarParent Under PlayerHUD");
        }

        bool changed = false;
        changed |= EnsureSkillBarLayout(skillBarTransform as RectTransform);

        Sprite potionSprite = LoadSprite(HealingPotionIconAssetPath, HealingPotionIconSpriteName);

        GameObject potionObject = null;
        Transform existingPotion = skillBarTransform.Find(HealingPotionObjectName);
        if (existingPotion == null)
        {
            existingPotion = skillBarTransform.Find("UI生命药水");
        }
        if (existingPotion != null)
        {
            if (existingPotion.name != HealingPotionObjectName)
            {
                existingPotion.name = HealingPotionObjectName;
                changed = true;
            }

            potionObject = existingPotion.gameObject;
        }

        if (potionObject == null)
        {
            potionObject = CreateGameObject(
                HealingPotionObjectName,
                LayerMask.NameToLayer("UI"),
                typeof(RectTransform),
                typeof(Image),
                typeof(UI_PlayerHealingPotion)
            );
            potionObject.transform.SetParent(skillBarTransform, false);

            RectTransform potionRect = potionObject.GetComponent<RectTransform>();
            potionRect.anchorMin = new Vector2(0.5f, 0f);
            potionRect.anchorMax = new Vector2(0.5f, 0f);
            potionRect.pivot = new Vector2(0.5f, 0f);
            potionRect.anchoredPosition = new Vector2(-108f, 0f);
            potionRect.sizeDelta = new Vector2(96f, 96f);
            changed = true;
        }

        potionObject.SetActive(true);
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(potionObject);

        RectTransform potionRectTransform = potionObject.GetComponent<RectTransform>();
        Image potionImage = GetOrAddComponent<Image>(potionObject);
        if (potionSprite != null && potionImage.sprite != potionSprite)
        {
            potionImage.sprite = potionSprite;
            changed = true;
        }

        if (potionImage.color != Color.white)
        {
            potionImage.color = Color.white;
            changed = true;
        }

        if (potionImage.type != Image.Type.Simple)
        {
            potionImage.type = Image.Type.Simple;
            changed = true;
        }

        if (potionImage.raycastTarget)
        {
            potionImage.raycastTarget = false;
            changed = true;
        }

        RectTransform cooldownRect = GetOrCreateRect(
            "CooldownImage",
            potionRectTransform,
            LayerMask.NameToLayer("UI")
        );
        if (cooldownRect.anchorMin != Vector2.zero)
        {
            cooldownRect.anchorMin = Vector2.zero;
            changed = true;
        }

        if (cooldownRect.anchorMax != Vector2.one)
        {
            cooldownRect.anchorMax = Vector2.one;
            changed = true;
        }

        if (cooldownRect.offsetMin != Vector2.zero)
        {
            cooldownRect.offsetMin = Vector2.zero;
            changed = true;
        }

        if (cooldownRect.offsetMax != Vector2.zero)
        {
            cooldownRect.offsetMax = Vector2.zero;
            changed = true;
        }

        Vector2 cooldownAnchoredPosition = new Vector2(0f, -0.70000076f);
        if (cooldownRect.anchoredPosition != cooldownAnchoredPosition)
        {
            cooldownRect.anchoredPosition = cooldownAnchoredPosition;
            changed = true;
        }

        Image cooldownImage = GetOrAddComponent<Image>(cooldownRect.gameObject);
        Color cooldownColor = new Color(0f, 0f, 0f, 0.627451f);
        if (potionSprite != null && cooldownImage.sprite != potionSprite)
        {
            cooldownImage.sprite = potionSprite;
            changed = true;
        }

        if (cooldownImage.color != cooldownColor)
        {
            cooldownImage.color = cooldownColor;
            changed = true;
        }

        if (cooldownImage.type != Image.Type.Filled)
        {
            cooldownImage.type = Image.Type.Filled;
            changed = true;
        }

        if (cooldownImage.fillMethod != Image.FillMethod.Radial360)
        {
            cooldownImage.fillMethod = Image.FillMethod.Radial360;
            changed = true;
        }

        if (!Mathf.Approximately(cooldownImage.fillAmount, 0f))
        {
            cooldownImage.fillAmount = 0f;
            changed = true;
        }

        if (cooldownImage.fillClockwise)
        {
            cooldownImage.fillClockwise = false;
            changed = true;
        }

        if (cooldownImage.fillOrigin != 2)
        {
            cooldownImage.fillOrigin = 2;
            changed = true;
        }

        if (cooldownImage.raycastTarget)
        {
            cooldownImage.raycastTarget = false;
            changed = true;
        }

        Transform existingCountTransform = potionObject.transform.Find("CountText");
        bool countRectWasCreated = existingCountTransform == null;
        RectTransform countRect = GetOrCreateRect(
            "CountText",
            potionRectTransform,
            LayerMask.NameToLayer("UI")
        );
        Vector2 countAnchorMin = new Vector2(0.5f, 0f);
        Vector2 countAnchorMax = new Vector2(0.5f, 0f);
        Vector2 countPivot = new Vector2(0.5f, 1f);
        Vector2 countSize = new Vector2(48f, 20f);
        if (countRectWasCreated && countRect.anchorMin != countAnchorMin)
        {
            countRect.anchorMin = countAnchorMin;
            changed = true;
        }

        if (countRectWasCreated && countRect.anchorMax != countAnchorMax)
        {
            countRect.anchorMax = countAnchorMax;
            changed = true;
        }

        if (countRectWasCreated && countRect.offsetMin != Vector2.zero)
        {
            countRect.offsetMin = Vector2.zero;
            changed = true;
        }

        if (countRectWasCreated && countRect.offsetMax != Vector2.zero)
        {
            countRect.offsetMax = Vector2.zero;
            changed = true;
        }

        if (countRectWasCreated && countRect.pivot != countPivot)
        {
            countRect.pivot = countPivot;
            changed = true;
        }

        if (countRectWasCreated && countRect.sizeDelta != countSize)
        {
            countRect.sizeDelta = countSize;
            changed = true;
        }

        Text countText = GetOrAddComponent<Text>(countRect.gameObject);
        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null)
        {
            uiFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
        }
        if (countRectWasCreated && countText.font != uiFont)
        {
            countText.font = uiFont;
            changed = true;
        }

        if (countRectWasCreated && countText.fontSize != 18)
        {
            countText.fontSize = 18;
            changed = true;
        }

        if (countRectWasCreated && countText.alignment != TextAnchor.MiddleCenter)
        {
            countText.alignment = TextAnchor.MiddleCenter;
            changed = true;
        }

        if (countRectWasCreated && countText.fontStyle != FontStyle.Bold)
        {
            countText.fontStyle = FontStyle.Bold;
            changed = true;
        }

        Color countColor = new Color(1f, 0.95f, 0.3f, 1f);
        if (countRectWasCreated && countText.color != countColor)
        {
            countText.color = countColor;
            changed = true;
        }

        if (countText.raycastTarget)
        {
            countText.raycastTarget = false;
            changed = true;
        }

        if (countRectWasCreated && countText.supportRichText)
        {
            countText.supportRichText = false;
            changed = true;
        }

        if (countRectWasCreated && countText.resizeTextForBestFit)
        {
            countText.resizeTextForBestFit = false;
            changed = true;
        }

        countText.transform.SetAsLastSibling();

        if (countText.text != "5/5")
        {
            countText.text = "5/5";
            changed = true;
        }

        Player player = Object.FindObjectOfType<Player>();
        UI_PlayerHealingPotion potionComponent = GetOrAddComponent<UI_PlayerHealingPotion>(potionObject);
        SerializedObject potionSo = new SerializedObject(potionComponent);
        SerializedProperty potionPlayerProperty = potionSo.FindProperty("player");
        SerializedProperty potionPlayerHealthProperty = potionSo.FindProperty("playerHealth");
        SerializedProperty potionImageProperty = potionSo.FindProperty("potionImage");
        SerializedProperty potionCooldownProperty = potionSo.FindProperty("cooldownImage");
        SerializedProperty potionCountProperty = potionSo.FindProperty("countText");
        SerializedProperty potionCountOffsetProperty = potionSo.FindProperty("countTextOffset");

        if (potionPlayerProperty != null && potionPlayerProperty.objectReferenceValue != player)
        {
            potionPlayerProperty.objectReferenceValue = player;
            changed = true;
        }

        if (player != null)
        {
            Entity_Health playerHealth = player.GetComponent<Entity_Health>();
            if (potionPlayerHealthProperty != null && potionPlayerHealthProperty.objectReferenceValue != playerHealth)
            {
                potionPlayerHealthProperty.objectReferenceValue = playerHealth;
                changed = true;
            }
        }

        if (potionImageProperty != null && potionImageProperty.objectReferenceValue != potionImage)
        {
            potionImageProperty.objectReferenceValue = potionImage;
            changed = true;
        }

        if (potionCooldownProperty != null && potionCooldownProperty.objectReferenceValue != cooldownImage)
        {
            potionCooldownProperty.objectReferenceValue = cooldownImage;
            changed = true;
        }

        if (potionCountProperty != null && potionCountProperty.objectReferenceValue != countText)
        {
            potionCountProperty.objectReferenceValue = countText;
            changed = true;
        }

        if (potionCountOffsetProperty != null && potionCountOffsetProperty.vector2Value != countRect.anchoredPosition)
        {
            potionCountOffsetProperty.vector2Value = countRect.anchoredPosition;
            changed = true;
        }

        potionSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(potionObject);
        EditorUtility.SetDirty(potionComponent);

        potionComponent.Configure(potionImage, cooldownImage, countText);

        if (player != null)
        {
            SerializedObject playerSo = new SerializedObject(player);
            SerializedProperty spriteProperty = playerSo.FindProperty("healingPotionWorldIconSprite");
            if (spriteProperty != null && potionSprite != null && spriteProperty.objectReferenceValue != potionSprite)
            {
                spriteProperty.objectReferenceValue = potionSprite;
                playerSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(player);
                changed = true;
            }
        }

        if (player != null)
        {
            changed |= EnsureHealingPotionWorldIcon(player, potionSprite);
        }

        Selection.activeGameObject = potionObject;
        EditorUtility.SetDirty(skillBarTransform.gameObject);
        return changed;
    }

    private static Transform FindSkillBarTransform()
    {
        GameObject skillBar = GameObject.Find(SkillBarName);
        if (skillBar != null)
        {
            return skillBar.transform;
        }

        GameObject playerHud = GameObject.Find(PlayerHudName);
        if (playerHud != null)
        {
            Transform child = playerHud.transform.Find(SkillBarName);
            if (child != null)
            {
                return child;
            }
        }

        GameObject rootCanvas = GameObject.Find("Canvas");
        if (rootCanvas != null)
        {
            Transform child = rootCanvas.transform.Find(SkillBarName);
            if (child != null)
            {
                return child;
            }
        }

        return null;
    }

    private static bool EnsureSkillBarLayout(RectTransform skillBarRect)
    {
        if (skillBarRect == null)
        {
            return false;
        }

        bool changed = false;
        Vector2 anchor = new Vector2(0.5f, 0f);
        Vector2 pivot = new Vector2(0.5f, 0f);
        Vector2 targetPosition = new Vector2(529f, 155.6f);

        if (skillBarRect.anchorMin != anchor)
        {
            skillBarRect.anchorMin = anchor;
            changed = true;
        }

        if (skillBarRect.anchorMax != anchor)
        {
            skillBarRect.anchorMax = anchor;
            changed = true;
        }

        if (skillBarRect.pivot != pivot)
        {
            skillBarRect.pivot = pivot;
            changed = true;
        }

        if (skillBarRect.anchoredPosition != targetPosition)
        {
            skillBarRect.anchoredPosition = targetPosition;
            changed = true;
        }

        if (skillBarRect.localScale != Vector3.one)
        {
            skillBarRect.localScale = Vector3.one;
            changed = true;
        }

        return changed;
    }

    private static bool EnsureHealingPotionWorldIcon()
    {
        Player player = Object.FindObjectOfType<Player>();
        Sprite potionSprite = LoadSprite(HealingPotionIconAssetPath, HealingPotionIconSpriteName);
        return player != null && EnsureHealingPotionWorldIcon(player, potionSprite);
    }

    private static bool EnsureHealingPotionWorldIcon(Player player, Sprite potionSprite)
    {
        if (player == null)
        {
            return false;
        }

        bool changed = false;
        Transform iconTransform = player.transform.Find("HealingPotionWorldIcon");
        GameObject iconObject = iconTransform != null ? iconTransform.gameObject : null;
        if (iconObject == null)
        {
            iconObject = CreateGameObject(
                "HealingPotionWorldIcon",
                player.gameObject.layer,
                typeof(SpriteRenderer),
                typeof(PlayerHealingPotionWorldIcon)
            );
            iconObject.transform.SetParent(player.transform, false);
            iconObject.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            changed = true;
        }

        iconObject.SetActive(true);

        SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(iconObject);
        if (potionSprite != null && spriteRenderer.sprite != potionSprite)
        {
            spriteRenderer.sprite = potionSprite;
            changed = true;
        }

        if (!string.Equals(spriteRenderer.sortingLayerName, "Player", System.StringComparison.Ordinal))
        {
            spriteRenderer.sortingLayerName = "Player";
            changed = true;
        }

        if (spriteRenderer.sortingOrder != 250)
        {
            spriteRenderer.sortingOrder = 250;
            changed = true;
        }

        PlayerHealingPotionWorldIcon iconComponent = GetOrAddComponent<PlayerHealingPotionWorldIcon>(iconObject);
        SerializedObject iconSo = new SerializedObject(iconComponent);
        SerializedProperty playerProperty = iconSo.FindProperty("player");
        SerializedProperty rendererProperty = iconSo.FindProperty("spriteRenderer");
        if (playerProperty != null && playerProperty.objectReferenceValue != player)
        {
            playerProperty.objectReferenceValue = player;
            changed = true;
        }

        if (rendererProperty != null && rendererProperty.objectReferenceValue != spriteRenderer)
        {
            rendererProperty.objectReferenceValue = spriteRenderer;
            changed = true;
        }

        if (changed)
        {
            iconSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(iconObject);
            EditorUtility.SetDirty(iconComponent);
            EditorUtility.SetDirty(player);
        }

        return changed;
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
