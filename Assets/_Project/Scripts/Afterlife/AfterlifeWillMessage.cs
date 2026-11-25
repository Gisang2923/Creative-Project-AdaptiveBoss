using System;
using System.Collections;
using TMPro;
using UnityEngine;

public sealed class AfterlifeWillMessage : MonoBehaviour
{
    public readonly struct StageMessage
    {
        public string Text { get; }
        public bool IsMajor { get; }

        public StageMessage(string text, bool isMajor)
        {
            Text = text;
            IsMajor = isMajor;
        }
    }

    private static readonly StageMessage[] Messages =
    {
        default,
        new StageMessage("세계의 구속에 대한 저항이 시작되었습니다.", true),
        new StageMessage("공격 능력에 대한 구속이 해제되었습니다.", false),
        new StageMessage("시야에 대한 구속이 해제되었습니다.", false),
        new StageMessage(
            "모든 구속이 해제되었습니다.\n스테이터스 강화가 해금되었습니다.",
            true
        )
    };

    [SerializeField] private TMP_Text messageText;
    [SerializeField, Min(0f)] private float displayDuration = 3f;

    private Coroutine hideCoroutine;
    private int visibleStage;

    public bool CurrentIsMajorStage { get; private set; }

    public static AfterlifeWillMessage GetOrCreate(TMP_Text fontSource)
    {
        if (fontSource == null || fontSource.font == null)
            return null;

        Canvas canvas = fontSource.GetComponentInParent<Canvas>(true);
        if (canvas == null || !canvas.isActiveAndEnabled)
            return null;

        AfterlifeWillMessage existing =
            canvas.GetComponentInChildren<AfterlifeWillMessage>(true);
        if (existing != null)
            return existing;

        GameObject root = new GameObject(
            "WillSystemMessage",
            typeof(RectTransform),
            typeof(CanvasRenderer)
        );
        root.layer = canvas.gameObject.layer;
        root.transform.SetParent(canvas.transform, false);

        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -48f);
        rect.sizeDelta = new Vector2(1600f, 180f);

        TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
        text.font = fontSource.font;
        text.fontSharedMaterial = fontSource.fontSharedMaterial;
        text.fontSize = 36f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = string.Empty;
        text.enabled = false;

        AfterlifeWillMessage presenter =
            root.AddComponent<AfterlifeWillMessage>();
        presenter.messageText = text;
        return presenter;
    }

    public bool TryShow(int stage, Action<int> onDisplayed)
    {
        if (stage < 1 || stage >= Messages.Length)
            return false;

        return TryShowMessage(stage, Messages[stage],
            displayDuration, onDisplayed);
    }

    public bool TryShowText(int key, string text, float duration = 3f)
    {
        if (key <= 0 || string.IsNullOrEmpty(text))
            return false;

        return TryShowMessage(key, new StageMessage(text, false),
            duration, null);
    }

    private bool TryShowMessage(
        int key,
        StageMessage message,
        float duration,
        Action<int> onDisplayed)
    {
        if (!isActiveAndEnabled ||
            messageText == null ||
            messageText.font == null ||
            !messageText.gameObject.activeInHierarchy ||
            string.IsNullOrEmpty(message.Text))
        {
            return false;
        }

        if (visibleStage == key && messageText.enabled)
            return false;

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        messageText.text = message.Text;
        messageText.enabled = true;
        CurrentIsMajorStage = message.IsMajor;
        visibleStage = key;
        hideCoroutine = StartCoroutine(
            HideAfterDuration(key, duration, onDisplayed)
        );
        return true;
    }

    private IEnumerator HideAfterDuration(
        int key,
        float duration,
        Action<int> onDisplayed)
    {
        // Wait until the text has had a frame to render before recording it.
        yield return null;
        if (!isActiveAndEnabled ||
            messageText == null ||
            !messageText.enabled ||
            visibleStage != key)
        {
            yield break;
        }

        onDisplayed?.Invoke(key);
        yield return new WaitForSecondsRealtime(duration);
        if (visibleStage == key)
            Hide();
    }

    private void OnDisable()
    {
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        Hide();
    }

    private void Hide()
    {
        hideCoroutine = null;
        visibleStage = 0;
        CurrentIsMajorStage = false;

        if (messageText != null)
        {
            messageText.text = string.Empty;
            messageText.enabled = false;
        }
    }
}
