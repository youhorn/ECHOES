using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Echoes
{
    /// <summary>
    /// 대화창 UI 컨트롤러(싱글톤). 텍스트 순차 출력(타자기 효과)과 선택지 분기를 담당한다.
    /// 조작: E 대화 진행 / ↑↓ 선택 / Z 확인.
    /// 대화 중에는 플레이어 입력을 잠근다.
    /// </summary>
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        [Header("대화창")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private GameObject continueIndicator; // 다음 표시(▼)

        [Header("선택지")]
        [SerializeField] private GameObject choicePanel;
        [Tooltip("선택지 버튼들 (각자 자식에 TMP_Text). 최대 개수만큼 미리 배치")]
        [SerializeField] private Button[] choiceButtons;

        [Header("타자기")]
        [Tooltip("글자당 출력 간격(초)")]
        [SerializeField] private float charInterval = 0.03f;

        public bool IsActive { get; private set; }

        private Coroutine typeRoutine;
        private bool lineFinished;

        // 플레이어 입력 제어 캐시
        private PlayerController playerController;
        private PlayerAttack playerAttack;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (choicePanel != null) choicePanel.SetActive(false);
        }

        // =============================================================
        // 진입점: 대사 출력 → (선택지 있으면) 선택지
        // =============================================================

        /// <summary>
        /// 대사들을 차례로 출력한 뒤, 선택지가 있으면 선택을 받는다.
        /// </summary>
        /// <param name="lines">순차 출력할 대사</param>
        /// <param name="choices">선택지 (null/빈 배열이면 단순 대화)</param>
        /// <param name="onComplete">완료 콜백. 인자는 (성공 여부, 고른 선택지 인덱스).
        /// 단순 대화는 (true, -1). 선택지 대화는 (정답 여부, 인덱스).</param>
        public void Begin(DialogueLine[] lines, DialogueChoice[] choices, Action<bool, int> onComplete)
        {
            if (IsActive) return;
            StartCoroutine(RunDialogue(lines, choices, onComplete));
        }

        private IEnumerator RunDialogue(DialogueLine[] lines, DialogueChoice[] choices, Action<bool, int> onComplete)
        {
            IsActive = true;
            LockPlayer(true);
            if (dialoguePanel != null) dialoguePanel.SetActive(true);

            // 1) 대사 순차 출력
            if (lines != null)
            {
                foreach (var line in lines)
                    yield return ShowLineRoutine(line.speaker, line.text);
            }

            bool success = true;
            int chosenIndex = -1;

            // 2) 선택지 처리
            if (choices != null && choices.Length > 0)
            {
                int picked = -1;
                yield return ChoiceRoutine(choices, i => picked = i);
                chosenIndex = picked;
                success = picked >= 0 && choices[picked].isCorrect;

                // 선택 반응 대사
                string response = choices[picked].responseText;
                if (!string.IsNullOrEmpty(response))
                    yield return ShowLineRoutine(speakerText != null ? speakerText.text : "", response);
            }

            // 3) 종료
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (choicePanel != null) choicePanel.SetActive(false);
            LockPlayer(false);
            IsActive = false;

            onComplete?.Invoke(success, chosenIndex);
        }

        // =============================================================
        // 대사 한 줄 (타자기 + E로 진행)
        // =============================================================

        private IEnumerator ShowLineRoutine(string speaker, string text)
        {
            if (speakerText != null) speakerText.text = speaker;
            if (continueIndicator != null) continueIndicator.SetActive(false);

            lineFinished = false;
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            typeRoutine = StartCoroutine(TypeText(text));

            // 출력 중 E를 누르면 즉시 전체 표시, 다 끝났으면 E로 다음
            while (true)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (!lineFinished)
                    {
                        if (typeRoutine != null) StopCoroutine(typeRoutine);
                        if (bodyText != null) bodyText.text = text;
                        lineFinished = true;
                        if (continueIndicator != null) continueIndicator.SetActive(true);
                    }
                    else
                    {
                        break; // 다음 줄로
                    }
                }
                yield return null;
            }
        }

        private IEnumerator TypeText(string text)
        {
            if (bodyText != null) bodyText.text = "";
            foreach (char c in text)
            {
                if (bodyText != null) bodyText.text += c;
                yield return new WaitForSeconds(charInterval);
            }
            lineFinished = true;
            if (continueIndicator != null) continueIndicator.SetActive(true);
        }

        // =============================================================
        // 선택지 (↑↓ 이동 / Z 확인)
        // =============================================================

        private IEnumerator ChoiceRoutine(DialogueChoice[] choices, Action<int> onPicked)
        {
            if (choicePanel != null) choicePanel.SetActive(true);
            if (continueIndicator != null) continueIndicator.SetActive(false);

            int count = Mathf.Min(choices.Length, choiceButtons.Length);
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                bool used = i < count;
                if (choiceButtons[i] == null) continue;
                choiceButtons[i].gameObject.SetActive(used);
                if (used)
                {
                    var label = choiceButtons[i].GetComponentInChildren<TMP_Text>();
                    if (label != null) label.text = choices[i].text;
                }
            }

            int sel = 0;
            int picked = -1;

            // 마우스 클릭도 지원
            for (int i = 0; i < count; i++)
            {
                int idx = i;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => picked = idx);
            }

            HighlightChoice(sel, count);

            while (picked < 0)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                {
                    sel = (sel - 1 + count) % count;
                    HighlightChoice(sel, count);
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                {
                    sel = (sel + 1) % count;
                    HighlightChoice(sel, count);
                }
                else if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return))
                {
                    picked = sel;
                }
                yield return null;
            }

            if (choicePanel != null) choicePanel.SetActive(false);
            onPicked?.Invoke(picked);
        }

        private void HighlightChoice(int sel, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (choiceButtons[i] == null) continue;
                // 선택된 항목은 약간 밝게 (ColorBlock 대신 스케일/색 간단 처리)
                var label = choiceButtons[i].GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = (i == sel ? "> " : "   ") + StripMarker(label.text);
            }
        }

        private string StripMarker(string s)
        {
            if (s.StartsWith("> ")) return s.Substring(2);
            if (s.StartsWith("   ")) return s.Substring(3);
            return s;
        }

        // =============================================================
        // 플레이어 입력 잠금
        // =============================================================

        private void LockPlayer(bool locked)
        {
            if (playerController == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null)
                {
                    playerController = p.GetComponent<PlayerController>();
                    playerAttack = p.GetComponent<PlayerAttack>();
                }
            }
            if (playerController != null) playerController.SetControlsEnabled(!locked);
            if (playerAttack != null) playerAttack.SetControlsEnabled(!locked);
        }
    }
}
