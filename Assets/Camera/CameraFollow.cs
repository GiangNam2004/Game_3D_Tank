using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    // Tọa độ này khớp với cài đặt X=0, Y=8, Z=-12 của bạn
    public Vector3 offset = new Vector3(0, 8f, -12f);

    private void LateUpdate()
    {
        if (target != null)
        {
            // Chỉ đi theo vị trí, không lấy góc xoay của xe
            transform.position = target.position + offset;
        }
    }
}