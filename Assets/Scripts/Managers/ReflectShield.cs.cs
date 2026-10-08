using UnityEngine;

public class ReflectShield : MonoBehaviour
{
    public float shieldDuration = 5f; 

    void Start()
    {
        Destroy(gameObject, shieldDuration); // Khiên tồn tại 5s
    }

    void OnTriggerEnter(Collider other)
    {
        Rigidbody shellRb = other.GetComponent<Rigidbody>();
        // Nếu chạm phải đạn (hoặc tên lửa)
        if (shellRb != null && (other.name.Contains("Shell") || other.name.Contains("Rocket")))
        {
            // Bật ngược hướng bay
            shellRb.velocity = -shellRb.velocity;
            other.transform.forward = -other.transform.forward;
        }
    }
}