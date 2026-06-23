using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes
{
    /// <summary>
    /// 보스(어둠의 화신 블랭크) 처치 직후 연출.
    /// "STAGE CLEAR" 화면 대신, 화신과 NPC가 사라진 상태에서 에코의 독백을 출력하고
    /// 이어서 기억 조각 배열 씬(Stage5)으로 넘어간다. 보스 BGM도 함께 정지한다.
    /// </summary>
    public class BossDefeatSequence : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private BossController boss;
        [Tooltip("처치 시 정지할 보스 BGM")]
        [SerializeField] private SceneBgm bossBgm;
        [Tooltip("독백 전에 숨길 보스 오브젝트(연출상 사라짐). 비우면 boss 사용")]
        [SerializeField] private GameObject bossObjectToHide;

        [Header("에코 독백")]
        [SerializeField] private DialogueLine[] echoMonologue;

        [Header("이동")]
        [Tooltip("처치 후 독백 시작까지 대기(초)")]
        [SerializeField] private float delayBeforeMonologue = 1.2f;
        [SerializeField] private string nextSceneName = "Stage5";

        private bool fired;

        private void Start()
        {
            if (boss == null) boss = FindObjectOfType<BossController>();
            if (boss != null) boss.OnBossDefeated += OnBossDefeated;
        }

        private void OnDisable()
        {
            if (boss != null) boss.OnBossDefeated -= OnBossDefeated;
        }

        private void OnBossDefeated()
        {
            if (fired) return;
            fired = true;
            StartCoroutine(Sequence());
        }

        private IEnumerator Sequence()
        {
            if (bossBgm != null) bossBgm.StopBgm();

            yield return new WaitForSeconds(delayBeforeMonologue);

            // 화신을 화면에서 치운다(사라진 연출)
            GameObject hide = bossObjectToHide != null ? bossObjectToHide : (boss != null ? boss.gameObject : null);
            if (hide != null) hide.SetActive(false);

            // 에코 독백
            if (DialogueSystem.Instance != null && echoMonologue != null && echoMonologue.Length > 0)
            {
                bool done = false;
                foreach (var l in echoMonologue)
                    if (string.IsNullOrEmpty(l.speaker)) l.speaker = "에코";
                DialogueSystem.Instance.Begin(echoMonologue, null, (_, __) => done = true);
                while (!done) yield return null;
            }

            GoNext();
        }

        private void GoNext()
        {
            if (string.IsNullOrEmpty(nextSceneName)) return;
            if (GameManager.Instance != null) GameManager.Instance.LoadSceneByName(nextSceneName);
            else SceneManager.LoadScene(nextSceneName);
        }
    }
}
