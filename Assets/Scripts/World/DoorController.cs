using System.Collections;
using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// 잠긴 문. 플레이어가 열쇠를 보유한 채 상호작용(E)하면 문이 열리고
    /// 잠시 후 다음 스테이지로 씬이 전환된다.
    /// 열쇠는 GameManager가 전역 관리하며, 문 해제 시 1개 소모된다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DoorController : MonoBehaviour
    {
        [Header("문 상태 스프라이트")]
        [SerializeField] private Sprite lockedSprite;   // obj_door_locked
        [SerializeField] private Sprite openSprite;     // obj_door_open

        [Header("씬 전환")]
        [Tooltip("켜면 GameManager의 다음 스테이지로 이동, 끄면 아래 씬 이름으로 이동")]
        [SerializeField] private bool goToNextStage = true;
        [Tooltip("goToNextStage가 꺼져 있을 때 이동할 씬 이름")]
        [SerializeField] private string targetScene = "";
        [Tooltip("문이 열린 뒤 씬 전환까지 대기 시간(초)")]
        [SerializeField] private float transitionDelay = 0.8f;

        [Header("열쇠 없을 때 안내")]
        [Tooltip("열쇠 없이 시도하면 표시할 안내 (선택)")]
        [SerializeField] private GameObject lockedHint;

        private SpriteRenderer sr;
        private bool playerInRange;
        private bool isOpen;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr != null && lockedSprite != null) sr.sprite = lockedSprite;
            if (lockedHint != null) lockedHint.SetActive(false);
        }

        private void Update()
        {
            if (!playerInRange || isOpen) return;
            if (Input.GetKeyDown(KeyCode.E))
                TryOpen();
        }

        private void TryOpen()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("[DoorController] GameManager가 씬에 없습니다.");
                return;
            }

            if (gm.UseKey())
            {
                Open();
            }
            else
            {
                // 열쇠 없음
                if (lockedHint != null) StartCoroutine(ShowHint());
                Debug.Log("[DoorController] 열쇠가 없습니다.");
            }
        }

        private void Open()
        {
            isOpen = true;
            if (sr != null && openSprite != null) sr.sprite = openSprite;
            if (lockedHint != null) lockedHint.SetActive(false);
            StartCoroutine(TransitionRoutine());
        }

        private IEnumerator TransitionRoutine()
        {
            yield return new WaitForSeconds(transitionDelay);

            GameManager gm = GameManager.Instance;
            if (goToNextStage)
                gm.LoadNextStage();
            else if (!string.IsNullOrEmpty(targetScene))
                gm.LoadSceneByName(targetScene);
            else
                Debug.LogWarning("[DoorController] 이동할 씬이 지정되지 않았습니다.");
        }

        private IEnumerator ShowHint()
        {
            lockedHint.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            if (!isOpen) lockedHint.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player")) playerInRange = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = false;
                if (lockedHint != null) lockedHint.SetActive(false);
            }
        }
    }
}
