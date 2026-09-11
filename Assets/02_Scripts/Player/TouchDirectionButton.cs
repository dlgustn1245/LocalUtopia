using UnityEngine;
using UnityEngine.EventSystems;

// 화면 D-pad 의 방향 버튼 하나. 포인터가 누르고 있는 동안 Player 에 방향을 넘기고, 떼거나 벗어나면 지운다.
// 버튼 사이로 손가락을 미끄러뜨려도 끊기지 않게, 눌린 상태로 들어오는 것도 누름으로 본다.
// 잡고 있는 상태는 Player 가 포인터 ID 별로 관리하므로 이 버튼은 상태를 갖지 않는다.
public class TouchDirectionButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Player player;
    public Vector2Int direction;

    public void OnPointerDown(PointerEventData eventData)
    {
        player.SetTouchDirection(eventData.pointerId, direction);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerPress != null)
        {
            player.SetTouchDirection(eventData.pointerId, direction);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        player.ClearTouchDirection(eventData.pointerId);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        player.ClearTouchDirection(eventData.pointerId);
    }
}
