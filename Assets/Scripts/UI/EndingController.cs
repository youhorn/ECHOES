using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Echoes
{
    /// <summary>
    /// Stage 5 "기억의 귀환" 엔딩 — 조각 배열 연출.
    /// (「지강이에게」 기획 수정: 거짓 문장 제거 → 7개 영어 단어 조각을 올바른 순서로
    ///  배열해 한 문장 "I have always loved you, my Echo." 를 완성한다.)
    ///
    /// 조작: ←→(또는 ↑↓) 단어 선택 / Z 배치 / Backspace 되돌리기.
    /// 정답 순서대로 모두 배치하면 → 완성 문장 → 페이드 블랙 → 스토리 설명 → 엔딩 크레딧.
    /// 실패 상태는 없다(잘못 고르면 무시/흔들림, 다시 시도).
    /// </summary>
    public class EndingController : MonoBehaviour
    {
        [Header("조각 배열 UI")]
        [SerializeField] private GameObject assemblyPanel;
        [Tooltip("완성 중인 문장을 표시")]
        [SerializeField] private TMP_Text sentenceLine;
        [Tooltip("섞인 단어 조각 라벨들(7개). 수집 순서대로 배치해도 됨")]
        [SerializeField] private TMP_Text[] wordSlots;

        [Tooltip("정답 문장 순서 = 조각 id 시퀀스. 기본 [3,1,5,7,6,4,2]")]
        [SerializeField] private int[] correctOrder = { 3, 1, 5, 7, 6, 4, 2 };

        [Header("색상")]
        [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.85f);
        [SerializeField] private Color selectColor = new Color(1f, 0.85f, 0.4f);
        [SerializeField] private Color placedColor = new Color(0.35f, 0.35f, 0.4f);
        [SerializeField] private Color wrongColor = new Color(1f, 0.3f, 0.3f);

        [Header("엔딩 연출")]
        [Tooltip("페이드용 전체 검정 이미지")]
        [SerializeField] private Image blackOverlay;
        [Tooltip("스토리 설명 텍스트(흰 글씨)")]
        [SerializeField] private TMP_Text storyText;
        [TextArea(4, 12)]
        [SerializeField] private string storyContent =
            "에코는 오래전, 할아버지와 단둘이 살았다.\n" +
            "곧 떠날 것을 안 할아버지는, 자신을 잃은 슬픔으로부터 에코를 지키려\n" +
            "어둠의 화신 ‘블랭크’에게 자신의 모든 것을 건넸다.\n" +
            "— “내가 죽은 뒤, 아이가 나를 기억하지 못하게 해다오.”\n\n" +
            "흩어진 기억을 다시 모은 끝에 에코는 알게 된다.\n" +
            "기억을 잃은 건 버려졌기 때문이 아니라,\n" +
            "너무 사랑받았기 때문이었다.";

        [Header("엔딩 이미지 (할아버지와 에코)")]
        [Tooltip("스토리 텍스트 후 페이드인되는 마지막 장면 이미지 (ending.png)")]
        [SerializeField] private Image endingImage;
        [SerializeField] private float endingImageHold = 2f;

        [Header("엔딩 크레딧")]
        [Tooltip("위로 스크롤되는 크레딧 루트")]
        [SerializeField] private RectTransform creditsRoot;
        [SerializeField] private TMP_Text creditsText;
        [TextArea(4, 12)]
        [SerializeField] private string creditsContent =
            "ECHOES\n\n\n기획 · 제작\n김민선\n\n학번\n202305135\n\n가상현실\n\n\n\nThank you for playing.";
        [SerializeField] private float creditsSpeed = 40f;
        [Tooltip("크레딧이 이만큼 올라가면 멈추고 타이틀 버튼 노출")]
        [SerializeField] private float creditsEndY = 1600f;
        [SerializeField] private GameObject returnButton;
        [Tooltip("엔딩 연출을 건너뛰는 스킵 버튼(우측 상단)")]
        [SerializeField] private GameObject skipButton;

        [Header("오디오")]
        [SerializeField] private AudioSource endingBgm;

        [Header("동작")]
        [SerializeField] private KeyCode placeKey = KeyCode.Z;
        [Tooltip("크레딧 전 스토리 텍스트(storyContent)를 화면에 보여주는 시간(초)")]
        [SerializeField] private float storyHold = 14f;
        [SerializeField] private float beforeFade = 2.5f;
        [SerializeField] private float fadeDuration = 2f;

        public event Action OnComplete;

        // 슬롯별 대응 조각 id (wordSlots와 동일 인덱스)
        private int[] slotFragmentId;
        private bool[] slotPlaced;
        private int selected;
        private int placedCount;
        private bool finished;
        private bool inputLocked;
        private bool skipped;
        private Coroutine endingCo;

        private void Start()
        {
            if (blackOverlay != null) { SetOverlayAlpha(0f); blackOverlay.gameObject.SetActive(true); }
            if (storyText != null) storyText.gameObject.SetActive(false);
            if (creditsRoot != null) creditsRoot.gameObject.SetActive(false);
            if (returnButton != null) returnButton.SetActive(false);
            if (skipButton != null) skipButton.SetActive(false);
            Populate();
            Highlight();
        }

        /// <summary>스킵 버튼 OnClick 연결용. 엔딩 연출을 즉시 끝까지 건너뛴다.</summary>
        public void SkipEnding()
        {
            if (skipped) return;
            skipped = true;
            if (endingCo != null) { StopCoroutine(endingCo); endingCo = null; }

            // 연출 정리 → 마지막 장면(엔딩 이미지) + 타이틀 버튼만 남긴다.
            if (assemblyPanel != null) assemblyPanel.SetActive(false);
            if (storyText != null) storyText.gameObject.SetActive(false);
            SetOverlayAlpha(1f);
            if (endingBgm != null && !endingBgm.isPlaying) { endingBgm.loop = true; endingBgm.Play(); }
            if (endingImage != null)
            {
                endingImage.gameObject.SetActive(true);
                Color ic = endingImage.color; ic.a = 1f; endingImage.color = ic;
            }
            if (creditsRoot != null) creditsRoot.gameObject.SetActive(false);
            if (skipButton != null) skipButton.SetActive(false);
            if (returnButton != null) returnButton.SetActive(true);
            OnComplete?.Invoke();
        }

        private void Populate()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[EndingController] GameManager가 없습니다.");
                return;
            }
            var mem = GameManager.Instance.Memories;
            int n = wordSlots != null ? wordSlots.Length : 0;
            slotFragmentId = new int[n];
            slotPlaced = new bool[n];

            // wordSlots를 수집 순서(조각 id 1..n)로 채운다 — 이 자체가 문장 순서와 달라 '섞임'이 됨.
            for (int i = 0; i < n; i++)
            {
                if (wordSlots[i] == null) continue;
                int fragId = i + 1; // id 1..7
                slotFragmentId[i] = fragId;
                var m = FindById(mem, fragId);
                wordSlots[i].text = m != null ? m.text : $"?{fragId}";
                wordSlots[i].gameObject.SetActive(true);
            }
            if (sentenceLine != null) sentenceLine.text = "";
        }

        private MemorySentence FindById(IReadOnlyList<MemorySentence> mem, int id)
        {
            foreach (var m in mem) if (m.id == id) return m;
            return null;
        }

        private void Update()
        {
            if (finished || inputLocked) return;
            if (slotFragmentId == null || slotFragmentId.Length == 0) return;

            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)
                || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                MoveSelection(1);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)
                || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                MoveSelection(-1);
            }
            else if (Input.GetKeyDown(placeKey) || Input.GetKeyDown(KeyCode.Return))
            {
                TryPlace();
            }
            else if (Input.GetKeyDown(KeyCode.Backspace))
            {
                Undo();
            }
        }

        private void MoveSelection(int dir)
        {
            int n = slotFragmentId.Length;
            for (int k = 0; k < n; k++)
            {
                selected = (selected + dir + n) % n;
                if (!slotPlaced[selected]) break;
            }
            Highlight();
        }

        // 실제로 배치한 슬롯 인덱스를 놓은 순서대로 기록(어떤 단어든 배치 가능)
        private readonly List<int> placedSeq = new List<int>();

        private void TryPlace()
        {
            if (slotPlaced[selected]) return;

            // 어떤 단어든 다음 칸에 배치한다(정답/오답 가리지 않음).
            slotPlaced[selected] = true;
            placedSeq.Add(selected);
            placedCount++;
            if (wordSlots[selected] != null) wordSlots[selected].color = placedColor;
            AppendToSentence();

            if (placedCount >= correctOrder.Length)
                CheckAnswer();
            else
                MoveSelection(1);
        }

        private void CheckAnswer()
        {
            bool correct = true;
            for (int i = 0; i < correctOrder.Length; i++)
            {
                if (slotFragmentId[placedSeq[i]] != correctOrder[i]) { correct = false; break; }
            }
            if (correct)
                endingCo = StartCoroutine(EndingSequence());
            else
                StartCoroutine(WrongThenReset());
        }

        private IEnumerator WrongThenReset()
        {
            inputLocked = true;
            // 배치한 단어들을 붉게 흔들어 보여준 뒤 처음부터 다시
            foreach (int idx in placedSeq)
                if (wordSlots[idx] != null) wordSlots[idx].color = wrongColor;
            if (sentenceLine != null)
            {
                string prev = sentenceLine.text;
                sentenceLine.color = wrongColor;
                sentenceLine.text = "아직 문장이 완성되지 않았다…";
            }
            yield return new WaitForSeconds(1.2f);
            ResetArrangement();
            inputLocked = false;
        }

        private void ResetArrangement()
        {
            for (int i = 0; i < slotPlaced.Length; i++)
            {
                slotPlaced[i] = false;
                if (wordSlots[i] != null) wordSlots[i].color = normalColor;
            }
            placedSeq.Clear();
            placedCount = 0;
            selected = 0;
            if (sentenceLine != null) { sentenceLine.color = normalColor; sentenceLine.text = ""; }
            Highlight();
        }

        private void Undo()
        {
            if (placedCount == 0) return;
            int lastIdx = placedSeq[placedSeq.Count - 1];
            placedSeq.RemoveAt(placedSeq.Count - 1);
            slotPlaced[lastIdx] = false;
            placedCount--;
            if (wordSlots[lastIdx] != null) wordSlots[lastIdx].color = normalColor;
            selected = lastIdx;
            AppendToSentence();
            Highlight();
        }

        private void AppendToSentence()
        {
            if (sentenceLine == null) return;
            var mem = GameManager.Instance.Memories;
            string s = "";
            foreach (int idx in placedSeq)
            {
                var m = FindById(mem, slotFragmentId[idx]);
                if (m != null) s += (s.Length > 0 ? " " : "") + m.text;
            }
            sentenceLine.color = normalColor;
            sentenceLine.text = s;
        }

        private IEnumerator FlashWrong(int idx)
        {
            wordSlots[idx].color = wrongColor;
            yield return new WaitForSeconds(0.35f);
            wordSlots[idx].color = (idx == selected) ? selectColor : normalColor;
        }

        private void Highlight()
        {
            for (int i = 0; i < slotFragmentId.Length; i++)
            {
                if (wordSlots[i] == null) continue;
                if (slotPlaced[i]) { wordSlots[i].color = placedColor; continue; }
                wordSlots[i].color = (i == selected) ? selectColor : normalColor;
            }
        }

        // =============================================================
        // 엔딩 시퀀스: 완성 문장 → 페이드 블랙 → 스토리 → 크레딧
        // =============================================================
        private IEnumerator EndingSequence()
        {
            finished = true;
            inputLocked = true;
            Debug.Log("[EndingController] 문장 완성 — 엔딩 시퀀스 시작");

            if (skipButton != null) skipButton.SetActive(true);

            yield return new WaitForSeconds(beforeFade);

            // 페이드 블랙
            if (blackOverlay != null)
            {
                float t = 0f;
                while (t < fadeDuration)
                {
                    t += Time.deltaTime;
                    SetOverlayAlpha(Mathf.Clamp01(t / fadeDuration));
                    yield return null;
                }
            }
            if (assemblyPanel != null) assemblyPanel.SetActive(false);

            // BGM (ending.mp3)
            if (endingBgm != null) { endingBgm.loop = true; endingBgm.Play(); }

            // (선택) 스토리 설명 — storyContent가 비어 있으면 건너뛴다(요청: 검정 배경에 크레딧만).
            if (storyText != null && !string.IsNullOrEmpty(storyContent))
            {
                storyText.text = storyContent;
                storyText.gameObject.SetActive(true);
                yield return new WaitForSeconds(storyHold);
                yield return FadeText(storyText, 1f, 0f, 0.6f);
                storyText.gameObject.SetActive(false);
            }

            // 1) 엔딩 크레딧 — 아무것도 없는 검정 배경에 흰 글자만 위로 스크롤
            if (creditsRoot != null && creditsText != null)
            {
                creditsText.text = creditsContent;
                creditsRoot.gameObject.SetActive(true);
                Vector2 pos = creditsRoot.anchoredPosition;
                while (pos.y < creditsEndY)
                {
                    pos.y += creditsSpeed * Time.deltaTime;
                    creditsRoot.anchoredPosition = pos;
                    yield return null;
                }
                creditsRoot.gameObject.SetActive(false);
            }

            // 2) 크레딧이 끝난 뒤, 마지막에 ending.png(할아버지와 에코) 페이드인
            if (endingImage != null)
            {
                endingImage.gameObject.SetActive(true);
                Color ic = endingImage.color; ic.a = 0f; endingImage.color = ic;
                float e = 0f;
                while (e < 1.0f)
                {
                    e += Time.deltaTime;
                    ic.a = Mathf.Clamp01(e / 1.0f); endingImage.color = ic;
                    yield return null;
                }
                if (endingImageHold > 0f) yield return new WaitForSeconds(endingImageHold);
            }

            if (skipButton != null) skipButton.SetActive(false);
            if (returnButton != null) returnButton.SetActive(true);
            endingCo = null;
            OnComplete?.Invoke();
        }

        private IEnumerator FadeText(TMP_Text t, float from, float to, float dur)
        {
            float e = 0f;
            Color c = t.color;
            while (e < dur)
            {
                e += Time.deltaTime;
                c.a = Mathf.Lerp(from, to, e / dur);
                t.color = c;
                yield return null;
            }
            c.a = to; t.color = c;
        }

        private void SetOverlayAlpha(float a)
        {
            if (blackOverlay == null) return;
            Color c = blackOverlay.color; c.a = a; blackOverlay.color = c;
        }

        /// <summary>엔딩 후 타이틀로 돌아가기 (버튼 OnClick 연결용).</summary>
        public void ReturnToTitle()
        {
            if (GameManager.Instance != null) GameManager.Instance.LoadTitle();
            else UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
        }
    }
}
