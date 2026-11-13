using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance
    {
        get;
        private set;
    }

    [Header("Scene Names")]
    [SerializeField]
    private string battleSceneName =
        "BossBattle";

    [SerializeField]
    private string afterlifeSceneName =
        "AfterLife";

    [Header("Transition Delay")]
    [SerializeField]
    private float playerDeathDelay =
        1.5f;

    [SerializeField]
    private float bossDeathDelay =
        2.0f;

    private bool transitionStarted;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(
            gameObject
        );
    }

    // =====================================
    // Player Defeat
    // =====================================

    public void HandlePlayerDefeat()
    {
        if (transitionStarted)
            return;

        StartCoroutine(
            PlayerDefeatRoutine()
        );
    }

    private IEnumerator
        PlayerDefeatRoutine()
    {
        transitionStarted = true;

        Debug.Log(
            "[Game Flow] Player Defeat"
        );

        // Death animation을 보여주기 위한 시간
        yield return new WaitForSeconds(
            playerDeathDelay
        );

        LoadAfterlife();
    }

    // =====================================
    // Boss Defeat
    // =====================================

    public void HandleBossDefeat()
    {
        if (transitionStarted)
            return;

        StartCoroutine(
            BossDefeatRoutine()
        );
    }

    private IEnumerator
        BossDefeatRoutine()
    {
        transitionStarted = true;

        Debug.Log(
            "[Game Flow] Boss Defeat"
        );

        yield return new WaitForSeconds(
            bossDeathDelay
        );

        LoadAfterlife();
    }

    // =====================================
    // Scene Transition
    // =====================================

    private void LoadAfterlife()
    {
        Debug.Log(
            "[Game Flow] Load Afterlife"
        );

        SceneManager.LoadScene(
            afterlifeSceneName
        );

        transitionStarted = false;
    }

    public void ReturnToBattle()
    {
        if (transitionStarted)
            return;

        StartCoroutine(
            ReturnToBattleRoutine()
        );
    }

    private IEnumerator
        ReturnToBattleRoutine()
    {
        transitionStarted = true;

        Debug.Log(
            "[Game Flow] Return To Battle"
        );

        yield return null;

        SceneManager.LoadScene(
            battleSceneName
        );

        transitionStarted = false;
    }
}