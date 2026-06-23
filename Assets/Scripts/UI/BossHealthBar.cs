using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Echoes
{
    /// <summary>
    /// 보스 HP바. BossController의 체력/페이즈/사망 이벤트를 구독해 갱신한다.
    /// 씬에 보스가 없거나 처치되면 바를 숨긴다.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [Tooltip("바 전체(이름+게이지). 보스 없음/처치 시 숨김용")]
        [SerializeField] private GameObject root;
        [Tooltip("게이지 채움 이미지 (Image Type = Filled)")]
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text nameText;
        [Tooltip("페이즈2 진입 시 게이지 색")]
        [SerializeField] private Color phase2Color = new Color(1f, 0.4f, 0.2f);

        private BossController boss;

        private void Start()
        {
            boss = FindObjectOfType<BossController>();
            if (boss == null)
            {
                if (root != null) root.SetActive(false);
                return;
            }

            boss.OnBossHealthChanged += UpdateHealth;
            boss.OnPhase2 += OnPhase2;
            boss.OnBossDefeated += OnDefeated;

            if (root != null) root.SetActive(true);
            if (fill != null) fill.fillAmount = 1f; // 보스는 풀피로 시작
        }

        private void OnDisable()
        {
            if (boss == null) return;
            boss.OnBossHealthChanged -= UpdateHealth;
            boss.OnPhase2 -= OnPhase2;
            boss.OnBossDefeated -= OnDefeated;
        }

        private void UpdateHealth(int current, int max)
        {
            if (fill != null) fill.fillAmount = max > 0 ? (float)current / max : 0f;
        }

        private void OnPhase2()
        {
            if (fill != null) fill.color = phase2Color;
        }

        private void OnDefeated()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
