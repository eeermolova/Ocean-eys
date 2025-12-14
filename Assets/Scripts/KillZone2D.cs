using UnityEngine;

public class KillZone2D : MonoBehaviour
{
    [SerializeField] private float killDamage = 99999f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // берём корень, чтобы сработало даже если коллайдер на child-объекте игрока
        Transform root = other.transform.root;

        if (!root.CompareTag("Player"))
            return;

        Health h = root.GetComponent<Health>();
        if (h != null)
        {
            h.TakeDamage(killDamage);
        }
    }
}
