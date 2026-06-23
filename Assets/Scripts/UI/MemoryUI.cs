using UnityEngine;
using TMPro;

namespace Echoes
{
    /// <summary>
    /// 기억 인벤토리 패널(ui_memory_panel). 획득한 기억 문장 목록을 표시한다.
    /// 토글 키(기본 Tab)로 열고 닫으며, 열려 있는 동안 플레이어 입력을 잠근다.
    /// Stage 5의 엔딩 조합 선택 UI는 별도 EndingController가 담당한다.
    /// </summary>
    public class MemoryUI : MonoBehaviour
    {
        [Header("패널")]
        [SerializeField] private GameObject panel;
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

        [Header("문장 슬롯 (5개, 순서대로)")]
        [Tooltip("각 슬롯의 문장 텍스트")]
        [SerializeField] private TMP_Text[] entryTexts;
        [Tooltip("각 슬롯의 획득 체크 표시 (선택)")]
        [SerializeField] private GameObject[] entryChecks;

        [Header("기타")]
        [SerializeField] private TMP_Text counterText; // "3 / 5 COLLECTED"
        [Tooltip("미획득 문장에 표시할 문구")]
        [SerializeField] private string lockedText = "???";

        private bool isOpen;
        private PlayerController playerController;
        private PlayerAttack playerAttack;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                Toggle();
        }

        public void Toggle()
        {
            if (isOpen) Close();
            else Open();
        }

        public void Open()
        {
            isOpen = true;
            Refresh();
            if (panel != null) panel.SetActive(true);
            LockPlayer(true);
        }

        public void Close()
        {
            isOpen = false;
            if (panel != null) panel.SetActive(false);
            LockPlayer(false);
        }

        private void Refresh()
        {
            if (GameManager.Instance == null) return;
            var memories = GameManager.Instance.Memories;

            for (int i = 0; i < entryTexts.Length; i++)
            {
                bool exists = i < memories.Count;
                var m = exists ? memories[i] : null;

                if (entryTexts[i] != null)
                    entryTexts[i].text = (m != null && m.collected) ? $"\"{m.text}\"" : lockedText;

                if (entryChecks != null && i < entryChecks.Length && entryChecks[i] != null)
                    entryChecks[i].SetActive(m != null && m.collected);
            }

            if (counterText != null)
            {
                int got = GameManager.Instance.CollectedCount;
                int total = memories.Count;
                counterText.text = $"{got} / {total} COLLECTED";
            }
        }

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
