using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qix
{
    // Qix 핵심 알고리즘: 미확보(Empty) 영역을 선(EdgeState.Boundary)을 벽 삼아 flood fill 로 나눈 뒤,
    // 적이 있는 영역은 남기고 나머지를 확보(Claimed) 처리한다.
    // 적이 하나도 없을 때는(테스트/초기 상태) 가장 넓은 영역을 플레이 필드로 남기고 나머지만 확보한다.
    //
    // 벽 판정을 칸이 아니라 변으로 하는 것이 핵심이다. 궤적은 칸을 차지하지 않고 칸 사이를 지나므로,
    // 칸만 봐서는 새로 그린 선이 영역을 갈랐다는 사실을 알 수 없다.
    // 호출 전에 궤적을 Boundary 로 승격시켜 두어야 한다.
    //
    // 확보 판정은 플레이 내내 반복되므로 호출마다 영역별 List 를 만들지 않는다.
    // 칸마다 영역 번호만 기록하고 크기는 따로 세어, 작업용 버퍼를 재사용한다.
    // static 클래스로 두면 버퍼가 씬을 벗어나도 살아남으므로 인스턴스로 유지할 것.
    public class QixCaptureService
    {
        // 영역에 속하지 않은 칸(확보 완료 또는 아직 번호를 매기지 않은 칸).
        const int NoRegion = 0;

        static readonly Vector2Int[] Directions4 =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        // 그리드 크기가 그대로면 계속 재사용하는 작업용 버퍼.
        readonly List<int> regionSizes = new();
        readonly HashSet<int> regionsToKeep = new();
        readonly Queue<Vector2Int> floodQueue = new();
        int[,] regionIds;

        // 새로 확보된 칸 수를 반환한다.
        public int Capture(QixGrid grid, IReadOnlyList<Vector2Int> enemyCells)
        {
            int regionCount = AssignRegionIds(grid);
            if (regionCount <= 1)
            {
                // 궤적이 영역을 둘로 가르지 못했다면(경계에 다시 닿기만 한 경우) 확보할 것이 없다.
                return 0;
            }

            SelectRegionsToKeep(grid, enemyCells);
            return ClaimUnkeptRegions(grid);
        }

        // 미확보 칸마다 영역 번호를 매기고 영역 개수를 반환한다.
        int AssignRegionIds(QixGrid grid)
        {
            PrepareBuffer(grid);

            regionSizes.Clear();
            regionSizes.Add(0); // NoRegion 자리. 이후 인덱스가 곧 영역 번호가 된다.

            int regionCount = 0;
            for (int x = 0; x < grid.Columns; x++)
            {
                for (int y = 0; y < grid.Rows; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (regionIds[x, y] != NoRegion || grid.GetState(cell) != CellState.Empty)
                    {
                        continue;
                    }

                    regionCount++;
                    regionSizes.Add(FloodFill(grid, cell, regionCount));
                }
            }

            return regionCount;
        }

        void PrepareBuffer(QixGrid grid)
        {
            if (regionIds == null || regionIds.GetLength(0) != grid.Columns || regionIds.GetLength(1) != grid.Rows)
            {
                // 새 배열은 이미 0(NoRegion)으로 초기화되어 있다.
                regionIds = new int[grid.Columns, grid.Rows];
                return;
            }

            Array.Clear(regionIds, 0, regionIds.Length);
        }

        // 영역에 속한 칸 수를 반환한다.
        int FloodFill(QixGrid grid, Vector2Int start, int regionId)
        {
            floodQueue.Clear();
            floodQueue.Enqueue(start);
            regionIds[start.x, start.y] = regionId;

            int size = 0;
            while (floodQueue.Count > 0)
            {
                var current = floodQueue.Dequeue();
                size++;

                foreach (var direction in Directions4)
                {
                    var next = current + direction;
                    if (!grid.IsInBounds(next) || regionIds[next.x, next.y] != NoRegion)
                    {
                        continue;
                    }

                    if (grid.GetState(next) != CellState.Empty)
                    {
                        continue;
                    }

                    // 두 칸 사이에 선이 놓였으면 넘어갈 수 없다. 이 판정이 영역을 가른다.
                    if (grid.IsBoundaryBetweenCells(current, next))
                    {
                        continue;
                    }

                    regionIds[next.x, next.y] = regionId;
                    floodQueue.Enqueue(next);
                }
            }

            return size;
        }

        // 적이 있는 영역이 하나라도 있으면 그 영역들을 모두 남기고, 하나도 없으면 가장 넓은 영역만 남긴다.
        void SelectRegionsToKeep(QixGrid grid, IReadOnlyList<Vector2Int> enemyCells)
        {
            regionsToKeep.Clear();

            if (enemyCells != null)
            {
                for (int i = 0; i < enemyCells.Count; i++)
                {
                    var enemy = enemyCells[i];
                    if (!grid.IsInBounds(enemy))
                    {
                        continue;
                    }

                    int regionId = regionIds[enemy.x, enemy.y];
                    if (regionId != NoRegion)
                    {
                        regionsToKeep.Add(regionId);
                    }
                }
            }

            if (regionsToKeep.Count > 0)
            {
                return;
            }

            int largestRegionId = NoRegion;
            for (int regionId = 1; regionId < regionSizes.Count; regionId++)
            {
                if (largestRegionId == NoRegion || regionSizes[regionId] > regionSizes[largestRegionId])
                {
                    largestRegionId = regionId;
                }
            }

            regionsToKeep.Add(largestRegionId);
        }

        int ClaimUnkeptRegions(QixGrid grid)
        {
            int claimedCount = 0;

            for (int x = 0; x < grid.Columns; x++)
            {
                for (int y = 0; y < grid.Rows; y++)
                {
                    int regionId = regionIds[x, y];
                    if (regionId == NoRegion || regionsToKeep.Contains(regionId))
                    {
                        continue;
                    }

                    grid.SetState(new Vector2Int(x, y), CellState.Claimed);
                    claimedCount++;
                }
            }

            return claimedCount;
        }
    }
}
