using UnityEngine;
using UnityEngine.UI;

// 장식용 UI 이미지를 흘려보낸다. 부모 Rect 가운데 앵커(0.5, 0.5) 기준으로 동작한다.
// 가로로 화면을 완전히 벗어나면 반대편에서 다시 들어오거나(구름), 끝에서 되돌아오며 좌우를 뒤집는다(걷는 캐릭터).
// 프레임이 2장 이상이면 순환시킨다.
public class Drifter : MonoBehaviour
{
    public Vector2 velocity = new Vector2(30f, 0f);
    public bool bounceHorizontal;
    // 화면 밖으로 나갔을 때 되돌아오는 점프 거리 = 부모 폭 + 이 값. 자기 폭을 쓰면 폭이 다른 행렬의 간격이 틀어진다.
    // 가장 넓은 장식(고양이 250px)보다 크게 두어 재등장 순간이 화면 밖이 되게 한다.
    public float wrapPadding = 300f;
    public float bobAmplitude;
    public float bobFrequency = 0.5f;
    public Sprite[] frames;
    public float frameInterval = 0.4f;

    RectTransform rect;
    RectTransform parentRect;
    Image image;
    float baseY;
    float bobTime;
    int frameIndex;
    float frameTimer;

    void Awake()
    {
        rect = (RectTransform)transform;
        parentRect = (RectTransform)rect.parent;
        image = GetComponent<Image>();
        baseY = rect.anchoredPosition.y;
    }

    void Update()
    {
        var pos = rect.anchoredPosition;
        pos.x += velocity.x * Time.deltaTime;
        baseY += velocity.y * Time.deltaTime;

        float halfParent = parentRect.rect.width * 0.5f;
        float halfSelf = rect.rect.width * 0.5f;

        if (bounceHorizontal)
        {
            if (pos.x + halfSelf > halfParent || pos.x - halfSelf < -halfParent)
            {
                velocity.x = -velocity.x;
                pos.x = Mathf.Clamp(pos.x, -halfParent + halfSelf, halfParent - halfSelf);
                var scale = rect.localScale;
                scale.x = -scale.x;
                rect.localScale = scale;
            }
        }
        else if (velocity.x > 0f && pos.x - halfSelf > halfParent)
        {
            pos.x -= parentRect.rect.width + wrapPadding;
        }
        else if (velocity.x < 0f && pos.x + halfSelf < -halfParent)
        {
            pos.x += parentRect.rect.width + wrapPadding;
        }

        bobTime += Time.deltaTime;
        pos.y = baseY + (bobAmplitude > 0f ? Mathf.Sin(bobTime * bobFrequency * Mathf.PI * 2f) * bobAmplitude : 0f);
        rect.anchoredPosition = pos;

        if (frames == null || frames.Length < 2)
        {
            return;
        }

        frameTimer += Time.deltaTime;
        if (frameTimer >= frameInterval)
        {
            frameTimer -= frameInterval;
            frameIndex = (frameIndex + 1) % frames.Length;
            image.sprite = frames[frameIndex];
        }
    }
}
