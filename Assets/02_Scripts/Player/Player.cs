using UnityEngine;

// 플레이어의 입력 처리와 이동을 전담한다.
// 위치를 바꾸는 코드는 반드시 이 스크립트를 거치도록 하고, 외부에서 transform 을 직접 건드리지 않는다.
public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;

    Vector2 moveDirection;
    Vector2 boundsMin;
    Vector2 boundsMax;
    bool hasBounds;

    void Update()
    {
        ReadInput();
        Move();
    }

    // 이동 가능한 월드 영역을 지정한다. 그리드를 소유한 QixController 가 알려준다.
    public void SetBounds(Vector2 min, Vector2 max)
    {
        boundsMin = min;
        boundsMax = max;
        hasBounds = true;

        // 이미 영역 밖에 있을 수 있으므로 현재 위치도 즉시 보정한다.
        MoveTo(transform.position);
    }

    // 시작 위치 정렬, 사망 복귀처럼 외부에서 위치를 강제로 옮길 때 사용한다.
    public void MoveTo(Vector2 position)
    {
        if (hasBounds)
        {
            position.x = Mathf.Clamp(position.x, boundsMin.x, boundsMax.x);
            position.y = Mathf.Clamp(position.y, boundsMin.y, boundsMax.y);
        }

        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    void ReadInput()
    {
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            moveDirection = Vector2.up;
        }
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            moveDirection = Vector2.down;
        }
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            moveDirection = Vector2.left;
        }
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            moveDirection = Vector2.right;
        }
        else
        {
            moveDirection = Vector2.zero;
        }
    }

    void Move()
    {
        if (moveDirection == Vector2.zero)
        {
            return;
        }

        MoveTo((Vector2)transform.position + moveDirection * moveSpeed * Time.deltaTime);
    }
}
