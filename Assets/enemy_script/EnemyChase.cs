    using UnityEngine;

public class EnemyChase : MonoBehaviour
{
    public Transform target;       // ลากตัว Player มาใส่ช่องนี้
    public float moveSpeed = 3.5f; // ความเร็วในการเดินไล่
    public float stoppingDistance = 1.2f; // ระยะหยุดไม่ให้เดินทับตัว Player เป๊ะ

    void Start()
    {
        // เผื่อลืมลาก target ใน Inspector ให้หาอัตโนมัติจาก Tag "Player"
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }
    }

    void Update()
    {
        if (target == null) return;

        // คำนวณทิศทางบนระนาบพื้น (ล็อกแกน Y ไม่ให้ก้มหรือเงย)
        Vector3 targetPosFlat = new Vector3(target.position.x, transform.position.y, target.position.z);
        Vector3 direction = targetPosFlat - transform.position;
        float distance = direction.magnitude;

        if (distance > stoppingDistance)
        {
            direction.Normalize();

            // หันหน้าเข้าหา Player
            transform.rotation = Quaternion.LookRotation(direction);

            // เคลื่อนที่ไปข้างหน้าตามทิศที่หัน
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }
}
