using UnityEngine;

public class ElementalShell : MonoBehaviour
{
    public enum ElementType { Ice, Toxic }
    public ElementType bulletType; 
    
    void OnTriggerEnter(Collider other)
    {
        // Trúng xe tăng địch
        if (other.CompareTag("Player") || other.GetComponent<TankHealth>() != null)
        {
            if (bulletType == ElementType.Ice)
            {
                other.gameObject.AddComponent<IceEffect>();
            }
            else if (bulletType == ElementType.Toxic)
            {
                other.gameObject.AddComponent<ToxicEffect>();
            }
        }
        // Nổ xong thì xóa đạn
        Destroy(gameObject);
    }
}