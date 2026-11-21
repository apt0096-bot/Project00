using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TopDownGunShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TopDownBullet bulletPrefab;
    [SerializeField] private Transform firePoint; // จุดปากกระบอกปืน (ถ้าไม่ใส่จะใช้ตำแหน่งตัวละคร)
    [SerializeField] private Transform aimRoot;   // ตัวอ้างอิงทิศทางหันหน้า (ค่าเริ่มต้นคือ transform ของ Player)

    [Header("Firing Settings")]
    [SerializeField] private float fireCooldown = 0.15f;
    [SerializeField] private float bulletSpeed = 28f;
    [SerializeField] private int bulletDamage = 25;
    [SerializeField] private bool automaticFire = false; // false = กดคลิกซ้ายทีละนัด, true = กดค้างยิงรัว

    private float _nextFireTime;

    private void Awake()
    {
        if (aimRoot == null)
        {
            aimRoot = transform;
        }
        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    private void Update()
    {
        bool triggerInput = automaticFire ? ReadFireHeld() : ReadFirePressed();

        if (triggerInput && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + fireCooldown;
            Shoot();
        }
    }

    public void Shoot()
    {
        if (bulletPrefab == null) return;

        // คำนวณทิศทางบนระนาบแนวนอน (X, Z) ตามที่ตัวละครหันหน้าอยู่
        Vector3 forwardDir = aimRoot.forward;
        forwardDir.y = 0f;
        if (forwardDir.sqrMagnitude < 0.0001f)
        {
            forwardDir = Vector3.forward;
        }

        Quaternion shotRotation = Quaternion.LookRotation(forwardDir.normalized, Vector3.up);
        Vector3 spawnPos = firePoint.position;

        TopDownBullet bullet = Instantiate(bulletPrefab, spawnPos, shotRotation);
        bullet.Initialize(aimRoot.gameObject, bulletSpeed, bulletDamage);
    }

    private bool ReadFirePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }
#endif
        return false;
    }

    private bool ReadFireHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButton(0))
        {
            return true;
        }
#endif
        return false;
    }
}