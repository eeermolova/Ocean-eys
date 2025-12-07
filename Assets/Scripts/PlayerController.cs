using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Параметры персонажа")]
    public InputSystem_Actions actions;

    public float speed;

    public float jumpForce;

    public Transform groundCheckTransform;

    public float groundCheckRadius;

    public LayerMask groundLayer;

    bool isGrounded;

    float move;

    private Rigidbody2D rb;
    private SimpleCombat combat;

    private void Awake()
    {
        actions = new InputSystem_Actions();
        combat = GetComponentInChildren<SimpleCombat>();
    }

    private void OnEnable()
    {
        actions.Player.Enable();
        actions.Player.Move.performed += Movement;
        actions.Player.Jump.performed += Jumping;

        actions.Player.Move.canceled += Movement;
        actions.Player.Jump.canceled += Jumping;
    }
    void OnDisable()
    {
        actions.Player.Disable();
        actions.Player.Move.performed -= Movement;
        actions.Player.Jump.performed -= Jumping;
    }

    void Movement(InputAction.CallbackContext ctx)
    {
        move = ctx.ReadValue<Vector2>().x;
    }

    void Jumping(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            if(isGrounded)
            {
                rb.linearVelocityY = jumpForce;
            }
            
        }
    }
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheckTransform.position, groundCheckRadius, groundLayer);
        rb.linearVelocityX = move * speed;

        Debug.Log($"Направление: {move}");
        if (move < 0)
        {
            combat.attackPoint.transform.position = new Vector2(transform.position.x - 0.5f, combat.attackPoint.transform.position.y);
        }
        else if (move > 0)
        {
            combat.attackPoint.transform.position = new Vector2(transform.position.x + 0.5f, combat.attackPoint.transform.position.y);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheckTransform.position, groundCheckRadius);
    }

 
}
