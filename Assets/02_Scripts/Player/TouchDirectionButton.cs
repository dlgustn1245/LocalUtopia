using UnityEngine;
using UnityEngine.EventSystems;

// 화면 D-pad 의 방향 버튼 하나. 누르고 있는 동안 Player 에 방향을 넘기고, 떼거나 벗어나면 지운다.
// 버튼 사이로 손가락을 미끄러뜨려도 끊기지 않게, 눌린 상태로 들어오는 것도 누름으로 본다.
public class TouchDirectionButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Player player;
    public Vector2Int direction;

    public void OnPointerDown(PointerEventData eventData)
    {
        Press();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerPress != null)
        {
            Press();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    void OnDisable()
    {
        Release();
    }

    void Press()
    {
        player.SetTouchDirection(direction);
    }

    void Release()
    {
        // 다른 버튼으로 옮겨 간 뒤 이 버튼의 Exit 가 늦게 오면 새 방향을 지우면 안 된다.
        player.ClearTouchDirection(direction);
    }
}
