namespace Qix
{
    // 게임판의 각 칸이 가질 수 있는 상태.
    // 칸은 채우기 판정과 렌더링에만 쓰이고, 플레이어의 이동 가능 여부는 EdgeState 가 결정한다.
    public enum CellState
    {
        // 미확보 영역. 플레이어가 궤적을 그려 나눌 수 있는 대상이다.
        // flood fill 로 영역을 나눌 때 탐색 대상이 되는 유일한 상태다.
        Empty,

        // 확보한 영역. 한 번 확보되면 다시 Empty 로 돌아가지 않는다.
        // 내부는 통과할 수 없고, 둘레의 선(EdgeState.Boundary)만 다닐 수 있다.
        Claimed
    }
}
