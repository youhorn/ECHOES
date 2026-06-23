using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// 피격 가능한 대상의 공통 인터페이스. 플레이어 · 보스가 함께 구현한다.
    /// 공격 측은 대상의 구체 타입을 몰라도 이 인터페이스로 데미지를 가할 수 있다.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>살아 있는지 여부.</summary>
        bool IsAlive { get; }

        /// <summary>
        /// 데미지를 입힌다.
        /// </summary>
        /// <param name="amount">데미지 양</param>
        /// <param name="sourcePosition">공격이 발생한 위치 (넉백 방향 계산용)</param>
        void TakeDamage(int amount, Vector2 sourcePosition);
    }
}
