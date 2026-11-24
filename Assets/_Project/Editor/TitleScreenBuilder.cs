#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TitleScreenBuilder
{
    private const string AssetFolder = "Assets/_Project/Art/TitleScreen";
    private const string ScenePath = "Assets/_Project/Scenes/TitleScreen.unity";

    [MenuItem("THE LAST OATH/Build Title Screen")]
    public static void Build()
    {
        string[] required =
        {
            "Title_Background",
            "Title_Logo",
            "Start_Normal",
            "Start_Selected",
            "Settings_Normal",
            "Settings_Selected",
            "Exit_Normal",
            "Exit_Selected"
        };

        var missing = new List<string>();
        var sprites = new Dictionary<string, Sprite>();

        foreach (string name in required)
        {
            string path = FindTexturePath(name);

            if (string.IsNullOrEmpty(path))
            {
                missing.Add(name + ".png");
                continue;
            }

            PrepareTexture(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                missing.Add(name + ".png");
            else
                sprites[name] = sprite;
        }

        if (missing.Count > 0)
        {
            Debug.LogError(
                "[Title Screen] Missing required PNGs in " +
                AssetFolder + ":\n" +
                string.Join("\n", missing)
            );
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        Canvas canvas = CreateCanvas();

        // Background
        GameObject backgroundFrame = CreateUIObject(
            "BackgroundFrame",
            canvas.transform
        );
        Stretch(backgroundFrame.GetComponent<RectTransform>());

        Image background = CreateImage(
            "Background",
            backgroundFrame.transform,
            sprites["Title_Background"]
        );
        background.raycastTarget = false;

        RectTransform bgRect = background.rectTransform;
        bgRect.anchorMin = bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(1920f, 1080f);

        AspectRatioFitter fitter =
            background.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 16f / 9f;

        // Controller
        GameObject controllerObject =
            new GameObject("TitleScreenController");
        TitleScreenController controller =
            controllerObject.AddComponent<TitleScreenController>();

        // Logo
        Image logo = CreateImage(
            "Title_Logo",
            canvas.transform,
            sprites["Title_Logo"]
        );
        logo.preserveAspect = true;
        logo.raycastTarget = false;
        SetRect(
            logo.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 315f),
            new Vector2(900f, 250f)
        );

        // Menu
        GameObject menuRoot = CreateUIObject(
            "MenuRoot",
            canvas.transform
        );
        SetRect(
            menuRoot.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -280f),
            new Vector2(720f, 390f)
        );

        Button startButton = CreateSpriteButton(
            "StartButton",
            menuRoot.transform,
            sprites["Start_Normal"],
            sprites["Start_Selected"],
            new Vector2(0f, 105f),
            new Vector2(650f, 120f)
        );

        Button settingsButton = CreateSpriteButton(
            "SettingsButton",
            menuRoot.transform,
            sprites["Settings_Normal"],
            sprites["Settings_Selected"],
            new Vector2(0f, -10f),
            new Vector2(610f, 105f)
        );

        Button exitButton = CreateSpriteButton(
            "ExitButton",
            menuRoot.transform,
            sprites["Exit_Normal"],
            sprites["Exit_Selected"],
            new Vector2(0f, -120f),
            new Vector2(610f, 105f)
        );

        ConfigureVerticalNavigation(
            startButton,
            settingsButton,
            exitButton
        );

        // Settings panel
        GameObject settingsPanel =
            CreateSettingsPanel(
                canvas.transform,
                out Slider volumeSlider,
                out Button backButton
            );

        // Fade
        CanvasGroup fade = CreateFade(canvas.transform);

        // Event System
        CreateEventSystem();

        // Button callbacks
        UnityEventTools.AddPersistentListener(
            startButton.onClick,
            controller.StartGame
        );

        UnityEventTools.AddPersistentListener(
            settingsButton.onClick,
            controller.OpenSettings
        );

        UnityEventTools.AddPersistentListener(
            exitButton.onClick,
            controller.ExitGame
        );

        UnityEventTools.AddPersistentListener(
            backButton.onClick,
            controller.CloseSettings
        );

        // Wire private fields
        SerializedObject so = new SerializedObject(controller);

        so.FindProperty("firstSceneName").stringValue = "BossBattle";
        so.FindProperty("startButton").objectReferenceValue = startButton;
        so.FindProperty("settingsButton").objectReferenceValue = settingsButton;
        so.FindProperty("exitButton").objectReferenceValue = exitButton;
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        so.FindProperty("masterVolumeSlider").objectReferenceValue = volumeSlider;
        so.FindProperty("settingsBackButton").objectReferenceValue = backButton;
        so.FindProperty("fadeCanvasGroup").objectReferenceValue = fade;
        so.FindProperty("fadeDuration").floatValue = 0.45f;

        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        AddSceneFirstInBuildSettings(ScenePath);

        Selection.activeGameObject = controllerObject;

        Debug.Log(
            "[Title Screen] Built successfully: " + ScenePath
        );
    }

    private static Canvas CreateCanvas()
    {
        GameObject go = new GameObject(
            "TitleCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution =
            new Vector2(1920f, 1080f);
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static Button CreateSpriteButton(
        string name,
        Transform parent,
        Sprite normal,
        Sprite selected,
        Vector2 position,
        Vector2 size)
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        SetRect(
            rect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            position,
            size
        );

        Image image = go.GetComponent<Image>();
        image.sprite = normal;
        image.preserveAspect = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.SpriteSwap;

        SpriteState state = button.spriteState;
        state.highlightedSprite = selected;
        state.selectedSprite = selected;
        state.pressedSprite = selected;
        state.disabledSprite = normal;
        button.spriteState = state;

        return button;
    }

    private static void ConfigureVerticalNavigation(
        Button start,
        Button settings,
        Button exit)
    {
        start.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = exit,
            selectOnDown = settings
        };

        settings.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = start,
            selectOnDown = exit
        };

        exit.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = settings,
            selectOnDown = start
        };
    }

    private static GameObject CreateSettingsPanel(
        Transform parent,
        out Slider slider,
        out Button backButton)
    {
        GameObject panel = new GameObject(
            "SettingsPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        panel.transform.SetParent(parent, false);

        SetRect(
            panel.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(680f, 360f)
        );

        panel.GetComponent<Image>().color =
            new Color(0.02f, 0.03f, 0.07f, 0.96f);

        CreateText(
            "SettingsTitle",
            panel.transform,
            "SETTINGS",
            42f,
            new Vector2(0f, 110f),
            new Vector2(520f, 65f)
        );

        CreateText(
            "VolumeLabel",
            panel.transform,
            "MASTER VOLUME",
            23f,
            new Vector2(0f, 28f),
            new Vector2(450f, 45f)
        );

        slider = CreateSlider(
            panel.transform,
            new Vector2(0f, -25f)
        );

        backButton = CreateTextButton(
            panel.transform,
            "BACK",
            new Vector2(0f, -112f)
        );

        panel.SetActive(false);
        return panel;
    }

    private static Slider CreateSlider(
        Transform parent,
        Vector2 position)
    {
        GameObject root = CreateUIObject(
            "MasterVolumeSlider",
            parent
        );

        SetRect(
            root.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            position,
            new Vector2(460f, 30f)
        );

        Slider slider = root.AddComponent<Slider>();

        Image bg = CreateSolidImage(
            "Background",
            root.transform,
            new Color(0.1f, 0.12f, 0.2f, 1f)
        );
        Stretch(bg.rectTransform);

        GameObject fillArea = CreateUIObject(
            "Fill Area",
            root.transform
        );

        RectTransform fillAreaRect =
            fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(6f, 0f);
        fillAreaRect.offsetMax = new Vector2(-6f, 0f);

        Image fill = CreateSolidImage(
            "Fill",
            fillArea.transform,
            new Color(0.75f, 0.08f, 0.13f, 1f)
        );
        Stretch(fill.rectTransform);

        GameObject handleArea = CreateUIObject(
            "Handle Slide Area",
            root.transform
        );

        RectTransform handleAreaRect =
            handleArea.GetComponent<RectTransform>();
        Stretch(handleAreaRect);
        handleAreaRect.offsetMin = new Vector2(8f, 0f);
        handleAreaRect.offsetMax = new Vector2(-8f, 0f);

        Image handle = CreateSolidImage(
            "Handle",
            handleArea.transform,
            new Color(0.95f, 0.91f, 0.85f, 1f)
        );

        RectTransform handleRect =
            handle.rectTransform;
        handleRect.anchorMin =
            new Vector2(0f, 0.5f);
        handleRect.anchorMax =
            new Vector2(0f, 0.5f);
        handleRect.pivot =
            new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta =
            new Vector2(18f, 34f);
        handleRect.anchoredPosition =
            Vector2.zero;

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        return slider;
    }

    private static Button CreateTextButton(
        Transform parent,
        string label,
        Vector2 position)
    {
        GameObject go = new GameObject(
            label + "Button",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

        go.transform.SetParent(parent, false);

        SetRect(
            go.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            position,
            new Vector2(220f, 55f)
        );

        Image image = go.GetComponent<Image>();
        image.color =
            new Color(0.1f, 0.11f, 0.17f, 0.95f);

        Button button = go.GetComponent<Button>();

        TMP_Text text = CreateText(
            "Label",
            go.transform,
            label,
            26f,
            Vector2.zero,
            new Vector2(200f, 50f)
        );
        Stretch(text.rectTransform);

        return button;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string content,
        float fontSize,
        Vector2 position,
        Vector2 size)
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        go.transform.SetParent(parent, false);

        TextMeshProUGUI text =
            go.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color =
            new Color(0.94f, 0.92f, 0.88f, 1f);
        text.raycastTarget = false;

        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;

        SetRect(
            text.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            position,
            size
        );

        return text;
    }

    private static CanvasGroup CreateFade(Transform parent)
    {
        GameObject go = new GameObject(
            "FadeOverlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup)
        );
        go.transform.SetParent(parent, false);

        Stretch(go.GetComponent<RectTransform>());

        Image image = go.GetComponent<Image>();
        image.color = Color.black;

        CanvasGroup group = go.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        go.transform.SetAsLastSibling();
        return group;
    }

    private static void CreateEventSystem()
    {
        GameObject go = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule)
        );

        InputSystemUIInputModule module =
            go.GetComponent<InputSystemUIInputModule>();

        module.AssignDefaultActions();
    }

    private static Image CreateImage(
        string name,
        Transform parent,
        Sprite sprite)
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        return image;
    }

    private static Image CreateSolidImage(
        string name,
        Transform parent,
        Color color)
    {
        Image image = CreateImage(name, parent, null);
        image.color = color;
        return image;
    }

    private static GameObject CreateUIObject(
        string name,
        Transform parent)
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform)
        );
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static string FindTexturePath(string assetName)
    {
        EnsureFolder("Assets/_Project/Art");
        EnsureFolder(AssetFolder);

        string[] guids = AssetDatabase.FindAssets(
            assetName + " t:Texture2D",
            new[] { AssetFolder }
        );

        if (guids.Length == 0)
            return null;

        string exact = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(
                path => System.IO.Path
                    .GetFileNameWithoutExtension(path)
                    .Equals(
                        assetName,
                        System.StringComparison.OrdinalIgnoreCase
                    )
            );

        return exact ??
            AssetDatabase.GUIDToAssetPath(guids[0]);
    }

    private static void PrepareTexture(string path)
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
            return;

        importer.textureType =
            TextureImporterType.Sprite;
        importer.spriteImportMode =
            SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression =
            TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;

        importer.SaveAndReimport();
    }

    private static void AddSceneFirstInBuildSettings(
        string path)
    {
        List<EditorBuildSettingsScene> scenes =
            EditorBuildSettings.scenes.ToList();

        scenes.RemoveAll(scene => scene.path == path);

        scenes.Insert(
            0,
            new EditorBuildSettingsScene(path, true)
        );

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent =
            System.IO.Path
                .GetDirectoryName(folder)
                ?.Replace("\\", "/");

        string child =
            System.IO.Path.GetFileName(folder);

        if (!string.IsNullOrEmpty(parent) &&
            !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
