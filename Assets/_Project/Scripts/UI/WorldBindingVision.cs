using UnityEngine;
using UnityEngine.UI;

// One screen-space overlay handles binding and injury, so the effects cannot stack.
public sealed class WorldBindingVision : MaskableGraphic
{
    public struct Settings
    {
        public float Stage1Radius;
        public float Stage2Radius;
        public float Stage1Softness;
        public float Stage2Softness;
        public float BindingStrength;
        public float EntryDuration;
        public Color InjuryColor;
        public float InjuryRadius;
        public float InjurySoftness;
        public float Stage3Strength;
        public float LowHealthThreshold;
        public float CriticalHealthThreshold;
        public float LowHealthStrength;
        public float CriticalHealthStrength;
        public float PulseSpeed;
        public float PulseAmount;
        public float RecoveryFadeDuration;
        public Color HealColor;
        public float HealStrength;
        public float HealFadeInDuration;
        public float HealHoldDuration;
        public float HealFadeOutDuration;
    }

    private enum VignetteMode { None, Binding, Stage3Injury, LowHealth }

    private static readonly int CenterId = Shader.PropertyToID("_Center");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
    private static readonly int StrengthId = Shader.PropertyToID("_Strength");
    private static readonly int ColorId = Shader.PropertyToID("_VignetteColor");

    private Material runtimeMaterial;
    private GameProgression progression;
    private Health playerHealth;
    private PlayerHeal playerHeal;
    private Transform player;
    private Camera worldCamera;
    private Settings settings;
    private VignetteMode mode;
    private Vector2 playerCenter = new Vector2(0.5f, 0.5f);
    private float entryElapsed;
    private float currentStrength;
    private float targetStrength;
    private float healthRatio = 1f;
    private bool critical;
    private bool entering;
    private bool healing;
    private float healElapsed;
    private float healBlend;
    private float healStartBlend;

    public static WorldBindingVision Create()
    {
        Shader shader = Resources.Load<Shader>("WorldBindingVignette");
        if (shader == null || !shader.isSupported)
        {
            Debug.LogError("[World Binding] Vignette shader is missing or unsupported.");
            return null;
        }

        GameObject canvasRoot = new GameObject(
            "WorldBindingVisionCanvas", typeof(RectTransform), typeof(Canvas));
        canvasRoot.layer = 5;
        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = -100;

        GameObject graphicRoot = new GameObject(
            "WorldBindingVision", typeof(RectTransform), typeof(CanvasRenderer));
        graphicRoot.layer = 5;
        graphicRoot.transform.SetParent(canvasRoot.transform, false);
        RectTransform rect = (RectTransform)graphicRoot.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        WorldBindingVision vision = graphicRoot.AddComponent<WorldBindingVision>();
        vision.runtimeMaterial = new Material(shader);
        vision.material = vision.runtimeMaterial;
        vision.raycastTarget = false;
        vision.SetVerticesDirty();
        return vision;
    }

    public void Initialize(
        GameProgression gameProgression,
        Health health,
        Transform target,
        Camera camera,
        Settings vignetteSettings)
    {
        progression = gameProgression;
        playerHealth = health;
        player = target;
        worldCamera = camera;
        settings = vignetteSettings;

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(
                playerHealth.CurrentHealth, playerHealth.MaxHealth);

            playerHeal = playerHealth.GetComponent<PlayerHeal>();
            if (playerHeal != null)
                playerHeal.OnHealthRestored += HandleHealRestored;
        }

        if (progression != null)
            progression.OnPhaseChanged += HandlePhaseChanged;
    }

    public void Begin(int stage, GamePhase phase)
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            HideImmediately();
            return;
        }

        if (phase == GamePhase.Act1 && (stage == 1 || stage == 2))
        {
            mode = VignetteMode.Binding;
            entryElapsed = 0f;
            entering = true;
            currentStrength = Mathf.Clamp01(settings.BindingStrength);
        }
        else if (phase == GamePhase.Act1 && stage == 3)
        {
            mode = VignetteMode.Stage3Injury;
            entering = false;
            currentStrength = Mathf.Clamp01(settings.Stage3Strength);
        }
        else if ((phase == GamePhase.Act1 && stage >= 4) ||
                 phase == GamePhase.Act2Adaptive)
        {
            mode = VignetteMode.LowHealth;
            entering = false;
            UpdateHealthTarget();
            currentStrength = targetStrength;
        }
        else
        {
            HideImmediately();
            return;
        }

        UpdateShader();
    }

    public void HideImmediately()
    {
        mode = VignetteMode.None;
        entering = false;
        healing = false;
        healElapsed = 0f;
        healBlend = 0f;
        healStartBlend = 0f;
        currentStrength = 0f;
        targetStrength = 0f;
        if (runtimeMaterial != null)
            runtimeMaterial.SetFloat(StrengthId, 0f);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Canvas.willRenderCanvases += UpdateBeforeCanvasRender;
    }

    protected override void OnDisable()
    {
        Canvas.willRenderCanvases -= UpdateBeforeCanvasRender;
        HideImmediately();
        base.OnDisable();
    }

    private void UpdateBeforeCanvasRender()
    {
        if (mode == VignetteMode.None && !healing)
            return;

        if (mode == VignetteMode.Binding)
            UpdatePlayerCenter();
        UpdateShader();
    }

    private void HandleHealthChanged(int current, int maximum)
    {
        healthRatio = maximum > 0
            ? Mathf.Clamp01((float)current / maximum)
            : 1f;

        if (current <= 0)
        {
            HideImmediately();
            return;
        }

        if (mode != VignetteMode.LowHealth)
            return;

        UpdateHealthTarget();
        // Damage produces an immediate visible cue; recovery fades out.
        if (targetStrength > currentStrength)
            currentStrength = Mathf.Max(currentStrength, targetStrength * 0.4f);
        UpdateShader();
    }

    private void HandleHealRestored()
    {
        if (!isActiveAndEnabled || playerHealth == null || playerHealth.IsDead)
            return;

        healStartBlend = healBlend;
        healing = true;
        healElapsed = 0f;
    }

    private void UpdateHealthTarget()
    {
        critical = healthRatio <= settings.CriticalHealthThreshold;
        targetStrength = critical
            ? settings.CriticalHealthStrength
            : healthRatio <= settings.LowHealthThreshold
                ? settings.LowHealthStrength
                : 0f;
        targetStrength = Mathf.Clamp01(targetStrength);
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase != GamePhase.Act1 && phase != GamePhase.Act2Adaptive)
            HideImmediately();
        else if (phase == GamePhase.Act2Adaptive)
            Begin(0, phase);
    }

    private void LateUpdate()
    {
        if ((mode == VignetteMode.None && !healing) || runtimeMaterial == null)
            return;

        if (mode == VignetteMode.Binding)
        {
            UpdatePlayerCenter();
            if (entering)
            {
                entryElapsed += Time.unscaledDeltaTime;
                entering = entryElapsed < settings.EntryDuration;
            }
        }
        else if (mode == VignetteMode.LowHealth)
        {
            float desired = targetStrength;
            if (critical && targetStrength > 0f)
            {
                desired += settings.PulseAmount *
                    Mathf.Sin(Time.unscaledTime * settings.PulseSpeed *
                              Mathf.PI * 2f);
            }

            float fadeDuration = Mathf.Max(0.01f,
                settings.RecoveryFadeDuration);
            currentStrength = Mathf.MoveTowards(
                currentStrength, Mathf.Clamp01(desired),
                Time.unscaledDeltaTime / fadeDuration);
        }

        if (healing)
        {
            healElapsed += Time.unscaledDeltaTime;
            float fadeIn = Mathf.Max(0f, settings.HealFadeInDuration);
            float holdEnd = fadeIn + Mathf.Max(0f, settings.HealHoldDuration);
            float fadeOut = Mathf.Max(0f, settings.HealFadeOutDuration);

            if (healElapsed < fadeIn)
            {
                healBlend = Mathf.Lerp(healStartBlend, 1f,
                    Mathf.SmoothStep(0f, 1f, healElapsed / fadeIn));
            }
            else if (healElapsed < holdEnd)
            {
                healBlend = 1f;
            }
            else if (healElapsed < holdEnd + fadeOut)
            {
                healBlend = 1f - Mathf.SmoothStep(0f, 1f,
                    (healElapsed - holdEnd) / fadeOut);
            }
            else
            {
                healing = false;
                healBlend = 0f;
            }
        }

        UpdateShader();
    }

    private void UpdatePlayerCenter()
    {
        if (player == null)
            return;

        if (worldCamera == null)
            worldCamera = Camera.main;
        if (worldCamera == null)
            return;

        Vector3 screen = worldCamera.WorldToScreenPoint(player.position);
        if (screen.z <= 0f)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screen, null, out Vector2 local))
        {
            Rect bounds = rectTransform.rect;
            if (bounds.width > 0f && bounds.height > 0f)
            {
                playerCenter = new Vector2(
                    (local.x - bounds.xMin) / bounds.width,
                    (local.y - bounds.yMin) / bounds.height);
            }
        }
    }

    private float GetEntryRadius(float aspect, float softness)
    {
        Vector2 center = playerCenter;
        float maximum = 0f;
        Vector2[] corners =
        {
            Vector2.zero, Vector2.right, Vector2.up, Vector2.one
        };
        foreach (Vector2 corner in corners)
        {
            Vector2 difference = corner - center;
            difference.x *= aspect;
            maximum = Mathf.Max(maximum, difference.magnitude);
        }
        return maximum + softness;
    }

    private void UpdateShader()
    {
        if (runtimeMaterial == null)
            return;

        Rect bounds = rectTransform.rect;
        float aspect = bounds.height > 0f
            ? bounds.width / bounds.height
            : 1f;
        bool binding = mode == VignetteMode.Binding;
        Vector2 center = binding
            ? playerCenter
            : new Vector2(0.5f, 0.5f);
        float radius = binding
            ? (settings.Stage1Radius)
            : settings.InjuryRadius;
        float softness = binding
            ? settings.Stage1Softness
            : settings.InjurySoftness;

        if (binding && progression != null &&
            progression.FearRecoveryStage == 2)
        {
            radius = settings.Stage2Radius;
            softness = settings.Stage2Softness;
        }

        if (binding && entering)
        {
            float t = Mathf.Clamp01(
                entryElapsed / Mathf.Max(0.01f, settings.EntryDuration));
            radius = Mathf.SmoothStep(
                GetEntryRadius(aspect, softness), radius, t);
        }

        float strength = Mathf.Clamp01(currentStrength);
        Color vignetteColor = binding ? Color.black : settings.InjuryColor;
        if (healing)
        {
            center = Vector2.Lerp(center, new Vector2(0.5f, 0.5f), healBlend);
            radius = Mathf.Lerp(radius, settings.InjuryRadius, healBlend);
            softness = Mathf.Lerp(softness, settings.InjurySoftness, healBlend);
            strength = Mathf.Lerp(strength,
                Mathf.Clamp01(settings.HealStrength), healBlend);
            vignetteColor = Color.Lerp(vignetteColor,
                settings.HealColor, healBlend);
        }

        softness = Mathf.Max(0.001f, softness);

        runtimeMaterial.SetVector(CenterId,
            new Vector4(center.x, center.y, 0f, 0f));
        runtimeMaterial.SetFloat(AspectId, aspect);
        runtimeMaterial.SetFloat(RadiusId, radius);
        runtimeMaterial.SetFloat(SoftnessId, softness);
        runtimeMaterial.SetFloat(StrengthId,
            strength);
        runtimeMaterial.SetColor(ColorId, vignetteColor);
    }

    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        if (runtimeMaterial == null)
            return;

        Rect bounds = rectTransform.rect;
        helper.AddVert(new Vector2(bounds.xMin, bounds.yMin),
            Color.white, new Vector2(0f, 0f));
        helper.AddVert(new Vector2(bounds.xMin, bounds.yMax),
            Color.white, new Vector2(0f, 1f));
        helper.AddVert(new Vector2(bounds.xMax, bounds.yMax),
            Color.white, new Vector2(1f, 1f));
        helper.AddVert(new Vector2(bounds.xMax, bounds.yMin),
            Color.white, new Vector2(1f, 0f));
        helper.AddTriangle(0, 1, 2);
        helper.AddTriangle(0, 2, 3);
    }

    protected override void OnDestroy()
    {
        Canvas.willRenderCanvases -= UpdateBeforeCanvasRender;
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= HandleHealthChanged;
        if (playerHeal != null)
            playerHeal.OnHealthRestored -= HandleHealRestored;
        if (progression != null)
            progression.OnPhaseChanged -= HandlePhaseChanged;
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
        base.OnDestroy();
    }
}
