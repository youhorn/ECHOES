using System;
using UnityEngine;

namespace Echoes
{
    /// <summary>대화 한 줄 (말하는 이 + 내용).</summary>
    [Serializable]
    public class DialogueLine
    {
        public string speaker;
        [TextArea(2, 4)] public string text;
    }

    /// <summary>선택지 한 개 (Stage 2 분기 대화용).</summary>
    [Serializable]
    public class DialogueChoice
    {
        [Tooltip("선택지 버튼에 표시할 문구")]
        public string text;
        [Tooltip("정답 선택지인지 (보상 지급 조건)")]
        public bool isCorrect;
        [TextArea(2, 3)]
        [Tooltip("이 선택지를 골랐을 때 NPC의 반응 대사")]
        public string responseText;
    }
}
