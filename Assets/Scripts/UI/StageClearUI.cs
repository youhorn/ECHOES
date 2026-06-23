using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace Echoes
{
    /// <summary>
    /// 보스 처치 시 "STAGE CLEAR" 연출을 띄운다.
    /// BossController.OnBossDefeated를 구독해, 지정 지연 후 패널을 표시하고
    /// 플레이어 입력을 잠근 뒤, 키 입력으로 다음 씬 이동 또는 현재 씬 재시작한다.
    /// </summary>
    public class StageClearUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subText;
        [Tooltip("\"아무 키나 누르면 계속\" 안내 (선택)")]
        [SerializeField] private GameObject continuePrompt;

        [Header("문구")]
        [SerializeField] private string titleMessage = "STAGE CLEAR";
        [SerializeField] private string subMessage = "어둠의 화신 블랭크를 물리쳤다";

        [Header("동작")]
        [Tooltip("처치 후 패널이 뜰 때까지 지연(초) — 사망 연출용")]
        [SerializeField] private float showDelay = 1.5f;
        [Tooltip("이동할 다음 씬 이름. 비우면 현재 씬을 재시작(리플레이).")]
        [SerializeField] private string nextSceneName = "";
        [Tooltip("패널 표시 후 키 입력을 받기까지 최소 대기(초)")]
        [SerializeField] private float inputArmDelay = 0.6f;

        private BossController boss;
        private bool shown;
        private bool canContinue;

        private void Start()
        {
            if (panel != null) panel.SetActive(false);
            if (continuePrompt != null) continuePrompt.SetActive(false);

            boss = FindObjectOfType<BossController>();
            if (boss != null) boss.OnBossDefeated += OnBossDefeated;
        }

        private void OnDisable()
        {
            if (boss != null) boss.OnBossDefeated -= OnBossDefeated;
        }

        private void OnBossDefeated()
        {
            if (shown) return;
            shown = true;
            StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            yield return new WaitForSeconds(showDelay);

            LockPlayer(true);
            if (panel != null) panel.SetActive(true);
            if (titleText != null) titleText.text = titleMessage;
            if (subText != null) subText.text = subMessage;

            yield return new WaitForSeconds(inputArmDelay);
            if (continuePrompt != null) continuePrompt.SetActive(true);
            canContinue = true;
        }

        private void Update()
        {
            if (!canContinue) return;
            if (Input.anyKeyDown) Continue();
        }

        private void Continue()
        {
            canContinue = false;
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                if (GameManager.Instance != null) GameManager.Instance.LoadSceneByName(nextSceneName);
                else SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                // 다음 씬 미지정 → 현재 씬 재시작(리플레이)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        private void LockPlayer(bool locked)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            var pc = p.GetComponent<PlayerController>();
            var pa = p.GetComponent<PlayerAttack>();
            if (pc != null) pc.SetControlsEnabled(!locked);
            if (pa != null) pa.SetControlsEnabled(!locked);
        }
    }
}
