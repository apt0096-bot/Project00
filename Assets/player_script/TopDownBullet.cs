using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TopDownBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float speed = 28f;
    [SerializeField] private float lifeTime = 2.5f;
    [SerializeField] private int damage = 25;
    [SerializeField] private bool lockYVelocity = true;

    private float _spawnY;
    private float _aliveTimer;
    private GameObject _owner;

    public void Initialize(GameObject owner, float customSpeed = -1f, int customDamage = -1)
    {
        _owner = owner;
        if (customSpeed > 0f) speed = customSpeed;
        if (customDamage >= 0) damage = customDamage;

        _spawnY = transform.position.y;
        _aliveTimer = lifeTime;
    }

    private void OnEnable()
    {
        _spawnY = transform.position.y;
        _aliveTimer = lifeTime;
    }

    private void Update()
    {
        _aliveTimer -= Time.deltaTime;
        if (_aliveTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 nextPos = transform.position + transform.forward * (speed * Time.deltaTime);
        if (lockYVelocity)
        {
            nextPos.y = _spawnY;
        }

        transform.position = nextPos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // ไม่ชนเจ้าของกระสุนหรือลูกของตัวละครผู้ยิง
        if (_owner != null && (other.gameObject == _owner || other.transform.IsChildOf(_owner.transform)))
        {
            return;
        }

        // ตัวอย่างจุดต่อยอด: ถ้าวัตถุที่โดนมี Component รับดาเมจสามารถเรียกใช้ตรงนี้ได้เลย
        // IDamageable damageable = other.GetComponentInParent<IDamageable>();
        // damageable?.TakeDamage(damage);

        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_owner != null &&
            (collision.gameObject == _owner || collision.transform.IsChildOf(_owner.transform)))
        {
            return;
        }

        Destroy(gameObject);
    }
}