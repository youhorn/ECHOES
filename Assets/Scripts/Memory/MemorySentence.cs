using System;

namespace Echoes
{
    /// <summary>
    /// 기억 조각 한 개의 데이터.
    /// (「지강이에게」 기획 수정 — 한 조각 = 영어 단어 한 토큰. 총 7개를 모아
    ///  엔딩에서 올바른 순서로 배열하면 "I have always loved you, my Echo." 가 된다.)
    /// </summary>
    [Serializable]
    public class MemorySentence
    {
        /// <summary>조각 번호 = 수집 순서 (1~7).</summary>
        public int id;
        /// <summary>조각 내용(영어 단어 토큰).</summary>
        public string text;
        /// <summary>(미사용) 과거 거짓 문장 플래그. 항상 false.</summary>
        public bool isFalse;
        /// <summary>플레이어가 획득했는지.</summary>
        public bool collected;

        public MemorySentence(int id, string text, bool isFalse)
        {
            this.id = id;
            this.text = text;
            this.isFalse = isFalse;
            this.collected = false;
        }
    }
}
