using System.Collections.Generic;
using UnityEngine;

namespace Qix
{
    // 플레이어가 미확보 영역을 가로지르는 동안 지나온 꼭짓점을 순서대로 기록한다.
    // 이미 지나온 꼭짓점에 다시 닿으면(자기 교차) 사망 처리를 위해 false 를 반환한다.
    public class QixTrail
    {
        readonly List<Vector2Int> points = new();
        readonly HashSet<Vector2Int> pointSet = new();

        public bool IsDrawing { get; private set; }
        public IReadOnlyList<Vector2Int> Points => points;

        public void Begin(Vector2Int startVertex)
        {
            points.Clear();
            pointSet.Clear();
            points.Add(startVertex);
            pointSet.Add(startVertex);
            IsDrawing = true;
        }

        public bool TryAddPoint(Vector2Int vertex)
        {
            if (!IsDrawing || vertex == points[^1])
            {
                return true;
            }

            if (pointSet.Contains(vertex))
            {
                return false;
            }

            points.Add(vertex);
            pointSet.Add(vertex);
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
