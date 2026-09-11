using System.Collections.Generic;
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

    // 화면 D-pad 에서 포인터(손가락/마우스)별로 잡고 있는 방향. 마지막에 잡은 것이 우선한다. 키보드 입력이 없을 때만 쓴다.
    //
    // 방향이 아니라 포인터 ID 로 기록하는 이유: PointerUp 은 처음 누른 버튼에만 오므로, 슬라이드로 들어간 버튼은
    // 자기 Up 을 받지 못한다. 같은 포인터 ID 를 지우면 어느 버튼에 잡혔든 함께 풀린다.
    // 손가락마다 ID 가 다르므로 한 손가락을 떼도 다른 손가락이 잡은 방향은 남는다.
    readonly List<(int pointerId, Vector2Int direction)> heldDirections = new(4);

    void Update()
    {
        ReadInput();
    }

    public void SetTouchDirection(int pointerId, Vector2Int direction)
    {
        ClearTouchDirection(pointerId);
        heldDirections.Add((pointerId, direction));
    }

    public void ClearTouchDirection(int pointerId)
    {
        for (int i = heldDirections.Count - 1; i >= 0; i--)
        {
            if (heldDirections[i].pointerId == pointerId)
            {
                heldDirections.RemoveAt(i);
            }
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
            InputDirection = heldDirections.Count > 0 ? heldDirections[^1].direction : Vector2Int.zero;
        }
    }
}
