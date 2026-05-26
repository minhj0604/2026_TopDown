using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    [Header("Move Sprites")]
    public Sprite[] spriteUp;
    public Sprite[] spriteDown;
    public Sprite[] spriteLeft;
    public Sprite[] spriteRight;

    [Header("Idle Sprites")]
    public Sprite[] idleUp;
    public Sprite[] idleDown;
    public Sprite[] idleLeft;
    public Sprite[] idleRight;

    [Header("Animation")]
    public float frameTime = 0.15f;
    public float idleFrameTime = 0.3f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 input;
    private Vector2 velocity;

    private Sprite[] currentSprites;
    private int frameIndex = 0;
    private float timer = 0f;

    // 마지막으로 바라본 방향 기억 (0:Down, 1:Up, 2:Left, 3:Right)
    private int lastDir = 0;
    private bool isMoving = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        currentSprites = idleDown;
        sr.sprite = currentSprites[0];
    }

    public void OnMove(InputValue value)
    {
        input = value.Get<Vector2>();
        velocity = input.normalized * moveSpeed;

        if (input.sqrMagnitude > 0.01f)
        {
            isMoving = true;

            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                if (input.x > 0)
                {
                    lastDir = 3;
                    ChangeSprites(spriteRight);
                }
                else
                {
                    lastDir = 2;
                    ChangeSprites(spriteLeft);
                }
            }
            else
            {
                if (input.y > 0)
                {
                    lastDir = 1;
                    ChangeSprites(spriteUp);
                }
                else
                {
                    lastDir = 0;
                    ChangeSprites(spriteDown);
                }
            }
        }
        else
        {
            isMoving = false;
            // 정지 시 마지막 방향의 idle 스프라이트로 전환
            ChangeSprites(GetIdleSprites(lastDir));
        }
    }

    private Sprite[] GetIdleSprites(int dir)
    {
        switch (dir)
        {
            case 1: return idleUp;
            case 2: return idleLeft;
            case 3: return idleRight;
            default: return idleDown;
        }
    }

    private void Update()
    {
        if (currentSprites == null || currentSprites.Length == 0) return;

        // 이동 중인지에 따라 프레임 속도 결정
        float currentFrameTime = isMoving ? frameTime : idleFrameTime;

        timer += Time.deltaTime;
        if (timer >= currentFrameTime)
        {
            timer = 0f;
            frameIndex++;
            if (frameIndex >= currentSprites.Length)
            {
                frameIndex = 0;
            }
            sr.sprite = currentSprites[frameIndex];
        }
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    private void ChangeSprites(Sprite[] newSprites)
    {
        if (newSprites == null || newSprites.Length == 0) return;
        if (currentSprites == newSprites) return;

        currentSprites = newSprites;
        frameIndex = 0;
        timer = 0f;
        sr.sprite = currentSprites[frameIndex];
    }
}