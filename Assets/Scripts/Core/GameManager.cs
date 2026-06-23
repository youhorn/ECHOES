using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes
{
    /// <summary>
    /// 게임 전역 상태 관리자(싱글톤). 씬이 바뀌어도 유지된다(DontDestroyOnLoad).
    /// - 기억 문장 5개의 마스터 데이터 및 획득 상태 보관
    /// - 열쇠 보유 수 관리
    /// - 스테이지 씬 전환
    /// 문/대화/퍼즐/보스/엔딩 시스템이 이 매니저를 통해 진행 데이터를 공유한다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("스테이지 씬 (Build Settings 등록 순서대로)")]
        [Tooltip("타이틀 화면 씬 이름")]
        [SerializeField] private string titleScene = "Title";
        [Tooltip("Stage 1~5 씬 이름 (순서대로)")]
        [SerializeField] private string[] stageScenes =
            { "Stage1", "Stage2", "Stage3", "Stage4", "Stage5" };

        // --- 진행 상태 ---
        private readonly List<MemorySentence> memories = new List<MemorySentence>();
        private int keyCount;
        private int currentStageIndex = -1; // -1 = 타이틀

        // --- 이벤트 ---
        /// <summary>기억 문장을 획득했을 때 (획득한 문장).</summary>
        public event Action<MemorySentence> OnMemoryCollected;
        /// <summary>열쇠 수가 바뀌었을 때 (현재 열쇠 수).</summary>
        public event Action<int> OnKeyChanged;

        // --- 조회 프로퍼티 ---
        public IReadOnlyList<MemorySentence> Memories => memories;
        public int KeyCount => keyCount;
        public bool HasKey => keyCount > 0;
        public int CollectedCount
        {
            get
            {
                int n = 0;
                foreach (var m in memories) if (m.collected) n++;
                return n;
            }
        }
        public bool HasAllMemories => CollectedCount >= memories.Count;
        public int CurrentStageIndex => currentStageIndex;

        private void Awake()
        {
            // 싱글톤 보장
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            InitMemoryData();
        }

        /// <summary>
        /// 7개 영어 단어 조각 마스터 데이터 초기화.
        /// (「지강이에게」 기획 수정 — 수집 순서대로 id 1~7, text=영어 단어 조각)
        /// 모두 모아 올바른 순서로 배열하면: "I have always loved you, my Echo."
        /// </summary>
        private void InitMemoryData()
        {
            memories.Clear();
            memories.Add(new MemorySentence(1, "have", false));   // Stage1 대화
            memories.Add(new MemorySentence(2, "Echo.", false));  // Stage2 선택지
            memories.Add(new MemorySentence(3, "I", false));      // ClockTower 멈춘 시계탑
            memories.Add(new MemorySentence(4, "my", false));     // Calculator 낡은 계산기
            memories.Add(new MemorySentence(5, "always", false)); // Stage3 시간의 틈
            memories.Add(new MemorySentence(6, "you,", false));   // Stage4 보스
            memories.Add(new MemorySentence(7, "loved", false));  // Stage4 보스
        }

        // ---------------------------------------------------------------
        // 기억 문장
        // ---------------------------------------------------------------

        /// <summary>id에 해당하는 기억 문장을 획득 처리. 이미 획득했으면 무시.</summary>
        public void CollectMemory(int id)
        {
            MemorySentence m = memories.Find(x => x.id == id);
            if (m == null)
            {
                Debug.LogWarning($"[GameManager] 존재하지 않는 기억 문장 id={id}");
                return;
            }
            if (m.collected) return;

            m.collected = true;
            OnMemoryCollected?.Invoke(m);
        }

        public bool IsMemoryCollected(int id)
        {
            MemorySentence m = memories.Find(x => x.id == id);
            return m != null && m.collected;
        }

        // ---------------------------------------------------------------
        // 열쇠
        // ---------------------------------------------------------------

        public void AddKey(int amount = 1)
        {
            keyCount += amount;
            OnKeyChanged?.Invoke(keyCount);
        }

        /// <summary>열쇠 1개 소모. 성공하면 true.</summary>
        public bool UseKey()
        {
            if (keyCount <= 0) return false;
            keyCount--;
            OnKeyChanged?.Invoke(keyCount);
            return true;
        }

        // ---------------------------------------------------------------
        // 씬 전환
        // ---------------------------------------------------------------

        /// <summary>새 게임 시작: 진행 데이터 초기화 후 Stage 1 로드.</summary>
        public void StartNewGame()
        {
            ResetProgress();
            LoadStage(0);
        }

        /// <summary>스테이지 인덱스(0=Stage1)로 이동.</summary>
        public void LoadStage(int index)
        {
            if (index < 0 || index >= stageScenes.Length)
            {
                Debug.LogError($"[GameManager] 잘못된 스테이지 인덱스 {index}");
                return;
            }
            currentStageIndex = index;
            SceneManager.LoadScene(stageScenes[index]);
        }

        /// <summary>다음 스테이지로 이동. 마지막이면 무시.</summary>
        public void LoadNextStage()
        {
            int next = currentStageIndex + 1;
            if (next >= stageScenes.Length)
            {
                Debug.Log("[GameManager] 마지막 스테이지입니다.");
                return;
            }
            LoadStage(next);
        }

        public void LoadTitle()
        {
            currentStageIndex = -1;
            SceneManager.LoadScene(titleScene);
        }

        /// <summary>씬 이름으로 직접 이동 (문/엔딩 등에서 사용).</summary>
        public void LoadSceneByName(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>진행 데이터 전체 초기화 (새 게임).</summary>
        public void ResetProgress()
        {
            keyCount = 0;
            currentStageIndex = -1;
            InitMemoryData();
            OnKeyChanged?.Invoke(keyCount);
        }
    }
}
