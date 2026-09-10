using UnityEngine;

// 플레이어의 입력 처리와 이동을 전담한다.
// 위치를 바꾸는 코드는 반드시 이 스크립트를 거치도록 하고, 외부에서 transform 을 직접 건드리지 않는다.
//
// 어디로 갈 수 있는지는 판단하지 않는다. 그리드를 아는 QixController 가 목표 꼭짓점을 정해주면
// 이 스크립트는 거기까지 이동만 한다.
public class Player : MonoBehaviour
{
    public Sprite safeSprite;
    public Sprite drawSprite;
    public Sprite hitSprite;

    public SpriteRenderer spriteRenderer;
    
    public float moveSpeed = 5f;

    public Vector2Int InputDirection { get; private set; }

    // 화면 D-pad 가 넣어 주는 방향. 키보드 입력이 없을 때만 쓴다.
    Vector2Int touchDirection;

    void Update()
    {
        ReadInput();
    }

    public void SetTouchDirection(Vector2Int direction)
    {
        touchDirection = direction;
    }

    // 지금 눌려 있는 방향이 맞을 때만 지운다. 버튼 사이를 미끄러질 때 새 방향이 덮어써진 뒤 옛 버튼의 해제가 와도 안전하다.
    public void ClearTouchDirection(Vector2Int direction)
    {
        if (touchDirection == direction)
        {
            touchDirection = Vector2Int.zero;
        }
    }

    public void SetSafeSprite()
    {
        spriteRenderer.sprite = safeSprite;
    }

    public void SetDrawSprite()
    {
        spriteRenderer.sprite = drawSprite;
    }

    public void SetHitSprite()
    {
        spriteRenderer.sprite = hitSprite;
    }

    // 목표 좌표를 향해 한 프레임만큼 이동한다. 도달했으면 true.
    public bool MoveTowards(Vector2 target)
    {
        var current = (Vector2)transform.position;
        if (current == target)
        {
            return true;
        }

        var next = Vector2.MoveTowards(current, target, moveSpeed * Time.deltaTime);
        transform.position = new Vector3(next.x, next.y, transform.position.z);

        return next == target;
    }

    // 시작 위치 정렬, 사망 복귀처럼 위치를 강제로 옮길 때 사용한다.
    public void MoveTo(Vector2 position)
    {
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    void ReadInput()
    {
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            InputDirection = Vector2Int.up;
        }
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            InputDirection = Vector2Int.down;
        }
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            InputDirection = Vector2Int.left;
        }
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            InputDirection = Vector2Int.right;
        }
        else
        {
            InputDirection = touchDirection;
        }
    }
}
