using System.Collections;
using UnityEngine;

public class PlayerAnimEvents : MonoBehaviour
{
    [SerializeField] private GameObject deathMenuStub; // панель-затычка
    [SerializeField] private float freezeDelay = 0.05f;

    private Animator animator;
    private Health health;
    private bool deathHandled;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();

        // чтобы анимация смерти играла даже при Time.timeScale = 0
        if (animator != null)
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        if (deathMenuStub != null)
            deathMenuStub.SetActive(false);
    }

    private void OnEnable()
    {
        if (health == null) return;
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        if (health == null) return;
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    private void OnDamaged()
    {
        if (deathHandled) return;
        if (animator != null) animator.SetTrigger("Hit");
    }

    private void OnDied()
    {
        if (deathHandled) return;
        deathHandled = true;

        if (animator != null) animator.SetBool("IsDead", true);

        // отключаем управление
        var pc = GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;

        StartCoroutine(FreezeAndShowMenu());
    }

    private IEnumerator FreezeAndShowMenu()
    {
        yield return new WaitForSecondsRealtime(freezeDelay);

        if (deathMenuStub != null) deathMenuStub.SetActive(true);
        Time.timeScale = 0f;
    }
}
