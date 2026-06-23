using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// NPC 상호작용. 플레이어가 접근하면 [E] 표시, E로 대화를 시작한다.
    /// 단순 대화(S1) 또는 선택지 분기 대화(S2)를 지원하며,
    /// 대화/선택 성공 시 기억 문장과 열쇠를 지급한다.
    /// </summary>
    public class NPCInteraction : MonoBehaviour
    {
        [Header("대화 내용")]
        [SerializeField] private string speakerName = "기억지기";
        [SerializeField] private DialogueLine[] lines;

        [Header("선택지 (S2처럼 분기 대화일 때만 사용. 비우면 단순 대화)")]
        [SerializeField] private DialogueChoice[] choices;
        [Tooltip("틀린 선택 시 보상 없이 다시 시도하게 할지")]
        [SerializeField] private bool allowRetryOnWrong = true;
        [Tooltip("틀렸을 때 보여줄 대사")]
        [SerializeField] private DialogueLine[] wrongLines;
        [Tooltip("정답/대화 성공 후(보상 지급 뒤) 이어서 출력할 대사. 선택지 responseText 다음에 나온다")]
        [SerializeField] private DialogueLine[] successLines;

        [Header("보상")]
        [Tooltip("지급할 기억 문장 id (0이면 없음)")]
        [SerializeField] private int rewardMemoryId = 0;
        [Tooltip("지급할 열쇠 수")]
        [SerializeField] private int rewardKeys = 1;

        [Header("수집 조건 (Stage1 영혼 게이트, 선택)")]
        [Tooltip("설정하면 이 카운터가 완료될 때까지 보상을 막고 '부족' 안내를 띄운다")]
        [SerializeField] private SoulLightCounter requiredSouls;
        [Tooltip("아직 부족할 때 보여줄 대사. 본문의 {n}은 남은 개수로 치환된다")]
        [SerializeField] private DialogueLine[] notEnoughLines;

        [Header("UI")]
        [Tooltip("접근 시 표시할 [E] 인디케이터 (obj_npc_indicator)")]
        [SerializeField] private GameObject interactIndicator;

        [Header("표정/방향 (선택)")]
        [Tooltip("NPC의 SpriteRenderer. 평소엔 정면, 말을 걸면 옆모습으로 바뀐다")]
        [SerializeField] private SpriteRenderer npcRenderer;
        [Tooltip("평소(정면) 스프라이트")]
        [SerializeField] private Sprite frontSprite;
        [Tooltip("말을 걸 때 보여줄 옆모습 스프라이트(왼쪽 기준)")]
        [SerializeField] private Sprite sideSprite;

        [Header("이벤트")]
        [Tooltip("대화를 성공적으로 마쳤을 때(보상 지급 시점) 호출. 예: 보스 전투 시작 연결")]
        [SerializeField] private UnityEngine.Events.UnityEvent onDialogueSuccess;

        private bool playerInRange;
        private bool rewardGranted;
        private Transform playerTransform;

        private void Awake()
        {
            if (interactIndicator != null) interactIndicator.SetActive(false);
        }

        private void Start()
        {
            // 대기 상태에서도 플레이어가 스폰되는 방향(들어오는 쪽)을 바라보게 한다.
            FaceSpawn();
        }

        /// <summary>플레이어가 스폰되는 방향(시작 위치)을 옆모습으로 바라본다.</summary>
        private void FaceSpawn()
        {
            if (npcRenderer == null || sideSprite == null) return;
            npcRenderer.sprite = sideSprite;
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            // 옆모습 원본은 왼쪽을 본다. 스폰 지점이 오른쪽이면 뒤집어 그쪽을 향한다.
            if (p != null) npcRenderer.flipX = p.transform.position.x > transform.position.x;
        }

        /// <summary>플레이어 쪽으로 옆모습을 향하게 한다(말을 걸 때 호출).</summary>
        private void FaceToPlayer()
        {
            if (npcRenderer == null || sideSprite == null) return;
            npcRenderer.sprite = sideSprite;
            // 옆모습 원본은 왼쪽을 보고 있다. 플레이어가 오른쪽이면 뒤집어 마주본다.
            if (playerTransform != null)
                npcRenderer.flipX = playerTransform.position.x > transform.position.x;
        }

        private void Update()
        {
            if (!playerInRange || rewardGranted) return;
            if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsActive) return;

            if (Input.GetKeyDown(KeyCode.E))
                StartConversation();
        }

        private void StartConversation()
        {
            if (DialogueSystem.Instance == null)
            {
                Debug.LogError("[NPCInteraction] DialogueSystem이 씬에 없습니다.");
                return;
            }

            if (interactIndicator != null) interactIndicator.SetActive(false);
            FaceToPlayer();

            // 영혼 게이트: 아직 부족하면 '부족' 안내만 띄우고 보상은 막는다.
            if (requiredSouls != null && !requiredSouls.IsComplete)
            {
                int n = requiredSouls.Remaining;
                DialogueLine[] msg;
                if (notEnoughLines != null && notEnoughLines.Length > 0)
                {
                    msg = new DialogueLine[notEnoughLines.Length];
                    for (int i = 0; i < notEnoughLines.Length; i++)
                        msg[i] = new DialogueLine
                        {
                            speaker = string.IsNullOrEmpty(notEnoughLines[i].speaker) ? speakerName : notEnoughLines[i].speaker,
                            text = (notEnoughLines[i].text ?? "").Replace("{n}", n.ToString())
                        };
                }
                else
                {
                    msg = new[] { new DialogueLine { speaker = speakerName, text = $"아직 영혼 오브젝트 {n}개가 부족해." } };
                }
                DialogueSystem.Instance.Begin(msg, null, (_, __) => RefreshIndicator());
                return;
            }

            // 화자 이름을 각 줄에 채워준다(비어 있으면 기본 화자명 사용)
            foreach (var l in lines)
                if (string.IsNullOrEmpty(l.speaker)) l.speaker = speakerName;

            DialogueSystem.Instance.Begin(lines, choices, OnDialogueComplete);
        }

        private void OnDialogueComplete(bool success, int chosenIndex)
        {
            bool hasChoices = choices != null && choices.Length > 0;

            if (hasChoices && !success)
            {
                // 틀린 선택: 보상 없음. allowRetryOnWrong가 꺼져 있으면 재시도 잠금.
                if (!allowRetryOnWrong) rewardGranted = true;

                if (wrongLines != null && wrongLines.Length > 0)
                    DialogueSystem.Instance.Begin(wrongLines, null, (_, __) => RefreshIndicator());
                else
                    RefreshIndicator();
                return;
            }

            // 성공 (단순 대화 완료 또는 정답 선택)
            GrantReward();
            RefreshIndicator();

            // 성공 후 이어지는 대사가 있으면 출력한 뒤 이벤트 발생
            if (successLines != null && successLines.Length > 0 && DialogueSystem.Instance != null)
            {
                foreach (var l in successLines)
                    if (string.IsNullOrEmpty(l.speaker)) l.speaker = speakerName;
                DialogueSystem.Instance.Begin(successLines, null, (_, __) => onDialogueSuccess?.Invoke());
            }
            else
            {
                onDialogueSuccess?.Invoke();
            }
        }

        private void GrantReward()
        {
            if (rewardGranted) return;
            rewardGranted = true;

            if (GameManager.Instance != null)
            {
                if (rewardMemoryId > 0) GameManager.Instance.CollectMemory(rewardMemoryId);
                if (rewardKeys > 0) GameManager.Instance.AddKey(rewardKeys);
            }
        }

        private void RefreshIndicator()
        {
            // 보상까지 끝났으면 더 띄울 필요 없음. 아직이고 범위 안이면 다시 표시.
            if (interactIndicator != null)
                interactIndicator.SetActive(playerInRange && !rewardGranted);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            playerInRange = true;
            playerTransform = other.transform;
            if (interactIndicator != null && !rewardGranted) interactIndicator.SetActive(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            playerInRange = false;
            if (interactIndicator != null) interactIndicator.SetActive(false);
        }
    }
}
