using System.Collections;
using UnityEngine;
using TMPro;

namespace Echoes
{
    /// <summary>
    /// 「지강이에게」 신규 퍼즐 — 숫자 암호 입력 퍼즐(재사용형).
    /// 멈춘 시계탑(1자리 "7")과 낡은 계산기(4자리 "1254")에 공용으로 사용한다.
    ///
    /// 흐름:
    ///  1) 플레이어가 접근([E] 표시) → E로 시작.
    ///  2) promptLines(NPC가 규칙/단서를 설명) 대화 출력 → 입력 모드 진입.
    ///  3) 키보드 숫자(0~9)로 코드 입력 / Backspace 지우기 / H 힌트 / Enter 제출 / Esc 취소.
    ///  4) 정답이면 successCutscene 대화 출력 → 기억 조각·열쇠 지급 → 보상 오브젝트(문) 활성화
    ///     + 사운드/시계탑 연출. 오답이면 입력 초기화 후 재시도.
    /// </summary>
    public class NumberLockPuzzle : MonoBehaviour
    {
        [Header("화자 / 단서 대화")]
        [SerializeField] private string speakerName = "???";
        [Tooltip("퍼즐 시작 시 출력할 규칙/단서 대사")]
        [SerializeField] private DialogueLine[] promptLines;
        [Tooltip("H 키 힌트 대사 한 줄")]
        [TextArea(2, 3)] [SerializeField] private string hintText = "";

        [Header("암호")]
        [Tooltip("정답 자릿수")]
        [SerializeField] private int digitCount = 1;
        [Tooltip("정답 코드(숫자 문자열). 예: 시계탑 \"7\", 계산기 \"1254\"")]
        [SerializeField] private string correctCode = "7";

        [Header("입력 표시")]
        [Tooltip("현재 입력을 보여줄 TMP (선택)")]
        [SerializeField] private TMP_Text inputDisplay;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color wrongColor = new Color(1f, 0.3f, 0.3f);

        [Header("정답 연출")]
        [Tooltip("정답 시 출력할 컷신 대사(검정 배경 텍스트 대용)")]
        [SerializeField] private DialogueLine[] successCutscene;
        [Tooltip("정답 직후 활성화할 보상 오브젝트(열쇠/출구 문 등)")]
        [SerializeField] private GameObject rewardObject;
        [Tooltip("정답 시 움직이게 할 오브젝트(시계탑 등, 선택)")]
        [SerializeField] private Transform animateOnSolve;
        [SerializeField] private Vector3 animateOffset = Vector3.zero;
        [SerializeField] private float animateDuration = 1.5f;

        [Header("오디오")]
        [Tooltip("정답 시 1회 재생(종소리 등)")]
        [SerializeField] private AudioSource solveSfx;
        [Tooltip("정답 시 켤 루프 BGM(오르골/째깍 등)")]
        [SerializeField] private AudioSource solveBgm;

        [Header("보상")]
        [SerializeField] private int rewardMemoryId = 0;
        [SerializeField] private int rewardKeys = 1;
        [Tooltip("정답 시 문 대신 곧장 이동할 씬 이름(계산기 텔레포트용). 비우면 rewardObject(문) 사용")]
        [SerializeField] private string nextSceneOnSolve = "";
        [Tooltip("정답 컷신 종료 후 씬 이동까지 대기(초)")]
        [SerializeField] private float teleportDelay = 0.6f;

        [Header("UI")]
        [Tooltip("접근 시 표시할 [E] 인디케이터")]
        [SerializeField] private GameObject interactIndicator;

        [Header("NPC 표정/방향 (선택)")]
        [Tooltip("이 퍼즐을 진행하는 NPC(시계지기 등)의 SpriteRenderer")]
        [SerializeField] private SpriteRenderer npcRenderer;
        [SerializeField] private Sprite frontSprite;
        [Tooltip("말을 걸 때 보여줄 옆모습(왼쪽 기준)")]
        [SerializeField] private Sprite sideSprite;
        [Tooltip("켜면 말을 걸 때도 항상 시작(플레이어 생성) 방향을 바라본다 — 시계지기처럼 고정 시선용")]
        [SerializeField] private bool faceSpawnAlways = false;

        [Header("도어락 키패드 UI (선택)")]
        [Tooltip("입력 중 표시할 키패드 패널")]
        [SerializeField] private GameObject keypadPanel;
        [Tooltip("키패드의 코드 디스플레이(LCD)")]
        [SerializeField] private TMP_Text keypadDisplay;

        [Header("종이 노트 (계산기 단서용, 선택)")]
        [Tooltip("상호작용 시 펼쳐지는 종이 노트 패널. 설정하면 대화 대신 노트로 단서를 보여준다")]
        [SerializeField] private GameObject notePanel;
        [Tooltip("노트에 적힌 단서 본문")]
        [TextArea(3, 10)] [SerializeField] private string noteContent = "";
        [SerializeField] private TMP_Text noteText;

        [Header("힌트 팝업 (선택, 닫기 버튼 포함)")]
        [Tooltip("힌트 버튼을 누르면 뜨는 팝업. 설정하면 대화창 대신 이 팝업으로 힌트를 보여준다")]
        [SerializeField] private GameObject hintPopup;
        [SerializeField] private TMP_Text hintPopupText;

        private bool playerInRange;
        private bool solved;
        private bool inputMode;
        private string buffer = "";
        private PlayerController playerController;

        private Transform playerTransform;
        private bool spawnFlipX;
        private bool spawnFlipCaptured;

        private void Awake()
        {
            if (interactIndicator != null) interactIndicator.SetActive(false);
            if (npcRenderer != null && frontSprite != null) npcRenderer.sprite = frontSprite;
        }

        private void FaceToPlayer()
        {
            if (npcRenderer == null || sideSprite == null) return;
            npcRenderer.sprite = sideSprite;
            // 시계지기처럼 시선을 고정해야 하는 경우: 시작 방향(스폰 쪽)을 계속 바라본다.
            if (faceSpawnAlways && spawnFlipCaptured) { npcRenderer.flipX = spawnFlipX; return; }
            if (playerTransform != null)
                npcRenderer.flipX = playerTransform.position.x > transform.position.x;
        }

        private void Start()
        {
            UpdateDisplay();
            FaceSpawn();
        }

        /// <summary>대기 상태에서 플레이어가 스폰되는 방향을 옆모습으로 바라본다.</summary>
        private void FaceSpawn()
        {
            if (npcRenderer == null || sideSprite == null) return;
            npcRenderer.sprite = sideSprite;
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) npcRenderer.flipX = p.transform.position.x > transform.position.x;
            // 시작 시점의 방향(스폰 쪽)을 기억해 둔다 — faceSpawnAlways일 때 계속 이 방향을 유지.
            spawnFlipX = npcRenderer.flipX;
            spawnFlipCaptured = true;
        }

        private void Update()
        {
            if (solved) return;

            if (!inputMode)
            {
                if (!playerInRange) return;
                if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsActive) return;
                if (Input.GetKeyDown(KeyCode.E)) BeginPuzzle();
                return;
            }

            HandleInput();
        }

        // ---------------------------------------------------------------
        private void BeginPuzzle()
        {
            if (interactIndicator != null) interactIndicator.SetActive(false);
            FaceToPlayer();

            // 종이 노트가 있으면(계산기): 대화 대신 노트로 단서를 펼치고 바로 입력 모드.
            if (notePanel != null)
            {
                if (noteText != null && !string.IsNullOrEmpty(noteContent)) noteText.text = noteContent;
                notePanel.SetActive(true);
                EnterInputMode();
                return;
            }

            // speaker가 null이면 기본 화자명, ""(빈 문자열)이면 나레이션(독백)으로 둔다.
            if (promptLines != null)
                foreach (var l in promptLines)
                    if (l.speaker == null) l.speaker = speakerName;

            if (DialogueSystem.Instance != null && promptLines != null && promptLines.Length > 0)
                DialogueSystem.Instance.Begin(promptLines, null, (_, __) => EnterInputMode());
            else
                EnterInputMode();
        }

        private void EnterInputMode()
        {
            inputMode = true;
            buffer = "";
            if (keypadPanel != null) keypadPanel.SetActive(true);
            UpdateDisplay();
            LockPlayer(true);
        }

        private void CloseInputUI()
        {
            if (keypadPanel != null) keypadPanel.SetActive(false);
            if (notePanel != null) notePanel.SetActive(false);
            if (hintPopup != null) hintPopup.SetActive(false);
        }

        // ===== 도어락 버튼 onClick 연결용 공개 메서드 =====
        public void PressDigit(int n)
        {
            if (!inputMode || solved) return;
            if (n < 0 || n > 9) return;
            if (buffer.Length < digitCount) { buffer += n.ToString(); UpdateDisplay(); }
        }
        public void PressBackspace()
        {
            if (!inputMode || solved) return;
            if (buffer.Length > 0) { buffer = buffer.Substring(0, buffer.Length - 1); UpdateDisplay(); }
        }
        public void PressEnter()
        {
            if (!inputMode || solved) return;
            if (buffer.Length == digitCount) Submit();
        }
        public void PressHint() { ShowHint(); }
        public void PressCancel()
        {
            if (!inputMode || solved) return;
            inputMode = false;
            CloseInputUI();
            LockPlayer(false);
            RefreshIndicator();
        }

        private void HandleInput()
        {
            // 숫자 입력
            for (int n = 0; n <= 9; n++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + n) || Input.GetKeyDown(KeyCode.Keypad0 + n))
                {
                    if (buffer.Length < digitCount)
                    {
                        buffer += n.ToString();
                        UpdateDisplay();
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Backspace) && buffer.Length > 0)
            {
                buffer = buffer.Substring(0, buffer.Length - 1);
                UpdateDisplay();
            }

            if (Input.GetKeyDown(KeyCode.H)) { ShowHint(); return; }

            if (Input.GetKeyDown(KeyCode.Escape)) { PressCancel(); return; }

            if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                && buffer.Length == digitCount)
            {
                Submit();
            }
        }

        private void ShowHint()
        {
            if (string.IsNullOrEmpty(hintText)) return;

            // 팝업이 있으면 닫기 버튼 포함 팝업으로 표시(입력 모드 유지 → 닫으면 바로 계속).
            if (hintPopup != null)
            {
                if (hintPopupText != null) hintPopupText.text = hintText;
                hintPopup.SetActive(true);
                return;
            }

            // 폴백: 대화창(키로 진행)
            inputMode = false;
            var hint = new[] { new DialogueLine { speaker = speakerName, text = hintText } };
            if (DialogueSystem.Instance != null)
                DialogueSystem.Instance.Begin(hint, null, (_, __) => EnterInputMode());
            else
                EnterInputMode();
        }

        /// <summary>힌트 팝업 닫기 버튼 OnClick 연결용.</summary>
        public void CloseHint()
        {
            if (hintPopup != null) hintPopup.SetActive(false);
        }

        private void Submit()
        {
            if (buffer == correctCode)
            {
                Solve();
            }
            else
            {
                if (inputDisplay != null || keypadDisplay != null) StartCoroutine(FlashWrong());
                buffer = "";
            }
        }

        private IEnumerator FlashWrong()
        {
            if (inputDisplay != null) { inputDisplay.color = wrongColor; inputDisplay.text = "X"; }
            if (keypadDisplay != null) { keypadDisplay.color = wrongColor; keypadDisplay.text = "X"; }
            yield return new WaitForSeconds(0.6f);
            UpdateDisplay();
        }

        private void Solve()
        {
            solved = true;
            inputMode = false;
            CloseInputUI();
            if (inputDisplay != null)
            {
                inputDisplay.color = normalColor;
                inputDisplay.text = correctCode;
            }

            if (solveSfx != null) solveSfx.Play();
            if (solveBgm != null) { solveBgm.loop = true; solveBgm.Play(); }
            if (animateOnSolve != null) StartCoroutine(AnimateMove());

            // speaker가 null이면 기본 화자명, ""(빈 문자열)이면 나레이션(독백)으로 둔다.
            if (successCutscene != null)
                foreach (var l in successCutscene)
                    if (l.speaker == null) l.speaker = speakerName;

            if (DialogueSystem.Instance != null && successCutscene != null && successCutscene.Length > 0)
                DialogueSystem.Instance.Begin(successCutscene, null, (_, __) => GrantAndOpen());
            else
                GrantAndOpen();
        }

        private void GrantAndOpen()
        {
            LockPlayer(false);
            if (GameManager.Instance != null)
            {
                if (rewardMemoryId > 0) GameManager.Instance.CollectMemory(rewardMemoryId);
                if (rewardKeys > 0) GameManager.Instance.AddKey(rewardKeys);
            }
            if (!string.IsNullOrEmpty(nextSceneOnSolve))
            {
                StartCoroutine(TeleportNext());
            }
            else if (rewardObject != null) rewardObject.SetActive(true);
            Debug.Log($"[NumberLockPuzzle] 정답({correctCode}) — 조각 {rewardMemoryId} 지급");
        }

        private IEnumerator TeleportNext()
        {
            if (teleportDelay > 0f) yield return new WaitForSeconds(teleportDelay);
            // "NEXT" 면 스테이지 순서대로 다음 씬(인덱스 갱신)으로, 아니면 지정 씬으로.
            if (nextSceneOnSolve == "NEXT")
            {
                if (GameManager.Instance != null) GameManager.Instance.LoadNextStage();
            }
            else if (GameManager.Instance != null) GameManager.Instance.LoadSceneByName(nextSceneOnSolve);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneOnSolve);
        }

        private IEnumerator AnimateMove()
        {
            Vector3 from = animateOnSolve.position;
            Vector3 to = from + animateOffset;
            float t = 0f;
            while (t < animateDuration)
            {
                t += Time.deltaTime;
                animateOnSolve.position = Vector3.Lerp(from, to, t / animateDuration);
                yield return null;
            }
            animateOnSolve.position = to;
        }

        private void UpdateDisplay()
        {
            string s = "";
            for (int i = 0; i < digitCount; i++)
            {
                s += (i < buffer.Length) ? buffer[i].ToString() : "_";
                if (i < digitCount - 1) s += " ";
            }
            if (inputDisplay != null) { inputDisplay.color = normalColor; inputDisplay.text = s; }
            if (keypadDisplay != null) { keypadDisplay.color = normalColor; keypadDisplay.text = s; }
        }

        private void LockPlayer(bool locked)
        {
            if (playerController == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerController = p.GetComponent<PlayerController>();
            }
            if (playerController != null) playerController.SetControlsEnabled(!locked);
        }

        private void RefreshIndicator()
        {
            if (interactIndicator != null)
                interactIndicator.SetActive(playerInRange && !solved);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            playerInRange = true;
            playerTransform = other.transform;
            if (interactIndicator != null && !solved && !inputMode) interactIndicator.SetActive(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            playerInRange = false;
            if (interactIndicator != null) interactIndicator.SetActive(false);
        }
    }
}
// pdf-revision reload trigger
