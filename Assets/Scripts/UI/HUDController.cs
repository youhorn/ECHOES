using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Echoes
{
    /// <summary>
    /// 인게임 HUD. HP바 · 열쇠 수 · 기억 카운터(3/5) · 스테이지 표시를 갱신한다.
    /// PlayerHealth와 GameManager의 이벤트를 구독하여 자동 반영한다.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("HP")]
        [Tooltip("HP바 채움 이미지 (Image Type = Filled)")]
        [SerializeField] private Image hpFill;
        [SerializeField] private TMP_Text hpText; // "70/100"

        [Header("열쇠 / 기억")]
        [SerializeField] private TMP_Text keyText;     // "x 1"
        [SerializeField] private TMP_Text memoryText;  // "기억 3 / 5"

        [Header("스테이지 (선택)")]
        [SerializeField] private TMP_Text stageNumberText;
        [SerializeField] private TMP_Text stageNameText;

        private PlayerHealth playerHealth;

        private void Start()
        {
            // 플레이어 HP 구독
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerHealth = p.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealth;
                UpdateHealth(playerHealth.CurrentHP, playerHealth.MaxHP);
            }

            // GameManager 이벤트 구독 (named 핸들러로 등록해 OnDisable에서 정확히 해제)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnKeyChanged += UpdateKeys;
                GameManager.Instance.OnMemoryCollected += OnMemoryCollectedHandler;
                UpdateKeys(GameManager.Instance.KeyCount);
                UpdateMemory();
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null) playerHealth.OnHealthChanged -= UpdateHealth;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnKeyChanged -= UpdateKeys;
                GameManager.Instance.OnMemoryCollected -= OnMemoryCollectedHandler;
            }
        }

        private void OnMemoryCollectedHandler(MemorySentence _) => UpdateMemory();

        private void UpdateHealth(int current, int max)
        {
            if (hpFill != null) hpFill.fillAmount = max > 0 ? (float)current / max : 0f;
            if (hpText != null) hpText.text = $"{current}/{max}";
        }

        private void UpdateKeys(int count)
        {
            if (keyText != null) keyText.text = $"x {count}";
        }

        private void UpdateMemory()
        {
            if (memoryText == null || GameManager.Instance == null) return;
            int got = GameManager.Instance.CollectedCount;
            int total = GameManager.Instance.Memories.Count;
            memoryText.text = $"기억 {got} / {total}";
        }

        /// <summary>스테이지 표시 갱신 (스테이지 씬 시작 시 호출).</summary>
        public void SetStage(int number, string stageName)
        {
            if (stageNumberText != null) stageNumberText.text = number.ToString();
            if (stageNameText != null) stageNameText.text = stageName;
        }
    }
}
