using System.Collections.Generic;
using UnityEngine;

namespace Qix
{
    // 플레이어가 미확보 영역을 가로지르는 동안의 궤적을 기록한다.
    // 궤적이 자기 자신과 겹치면 사망 처리를 위해 false 를 반환한다.
    public class QixTrail
    {
        readonly List<Vector2Int> points = new();
        readonly HashSet<Vector2Int> pointSet = new();

        public bool IsDrawing { get; private set; }
        public IReadOnlyList<Vector2Int> Points => points;

        public void Begin(Vector2Int startCell)
        {
            points.Clear();
            pointSet.Clear();
            points.Add(startCell);
            pointSet.Add(startCell);
            IsDrawing = true;
        }

        public bool TryAddPoint(Vector2Int cell)
        {
            if (!IsDrawing || cell == points[^1])
            {
                return true;
            }

            if (pointSet.Contains(cell))
            {
                return false;
            }

            points.Add(cell);
            pointSet.Add(cell);
            return true;
        }

        // Clear 는 내부 배열 용량을 유지하므로, 궤적을 반복해도 재할당이 일어나지 않는다.
        public void Cancel()
        {
            points.Clear();
            pointSet.Clear();
            IsDrawing = false;
        }
    }
}
