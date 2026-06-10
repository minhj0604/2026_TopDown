using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
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
    private PlayerCombat combat;          // 추가: 공격 상태 참조용

    private Vector2 input;
    private Vector2 velocity;

    private Sprite[] currentSprites;
    private int frameIndex = 0;
    private float timer = 0f;

    // 마지막으로 바라본 방향 (0:Down, 1:Up, 2:Left, 3:Right)
    private int lastDir = 0;
    private bool isMoving = false;

    // 공격 중 여부를 안전하게 가져오는 프로퍼티
    private bool IsAttacking => combat != null && combat.IsAttacking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        combat = GetComponent<PlayerCombat>();   // 같은 오브젝트에 PlayerCombat이 있으면 자동 연결

        currentSprites = idleDown;
        sr.sprite = currentSprites[0];
    }

    public void OnMove(InputValue value)
    {
        input = value.Get<Vector2>();
        velocity = input.normalized * moveSpeed;

        // 공격 중에는 방향/스프라이트 갱신을 막는다 (입력값 자체는 저장해 둠)
        if (IsAttacking) return;

        UpdateFacing();
    }

    // 입력값(input) 기준으로 방향과 스프라이트 세트를 갱신
    private void UpdateFacing()
    {
        if (input.sqrMagnitude > 0.01f)
        {
            isMoving = true;

            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                if (input.x > 0) { lastDir = 3; ChangeSprites(spriteRight); }
                else { lastDir = 2; ChangeSprites(spriteLeft); }
            }
            else
            {
                if (input.y > 0) { lastDir = 1; ChangeSprites(spriteUp); }
                else { lastDir = 0; ChangeSprites(spriteDown); }
            }
        }
        else
        {
            isMoving = false;
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
        // 공격 중에는 Animator가 스프라이트를 제어하므로 여기서 손대지 않는다.
        if (IsAttacking) return;

        if (currentSprites == null || currentSprites.Length == 0) return;

        float currentFrameTime = isMoving ? frameTime : idleFrameTime;

        timer += Time.deltaTime;
        if (timer >= currentFrameTime)
        {
            timer = 0f;
            frameIndex++;
            if (frameIndex >= currentSprites.Length)
                frameIndex = 0;

            sr.sprite = currentSprites[frameIndex];
        }
    }

    private void FixedUpdate()
    {
        // 공격 중에는 이동 잠금
        if (IsAttacking)
            return;

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

    /// <summary>
    /// PlayerCombat이 공격 종료 시 호출해 주면, 키를 떼지 않아도 즉시 이동/idle 상태로 복귀한다.
    /// (OnMove가 입력 변화 때만 호출되는 문제를 보완)
    /// </summary>
    public void RefreshAfterAttack()
    {
        UpdateFacing();
    }
}