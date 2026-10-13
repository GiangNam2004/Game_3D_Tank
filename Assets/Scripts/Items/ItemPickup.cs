using UnityEngine;
using Photon.Pun; 

public enum PickupType
{
    ToolChest,
    FuelTank,
    ReflectShield
}

public class ItemPickup : MonoBehaviourPun 
{
    public PickupType type;
    public float healAmount = 35f;
    public float speedMultiplier = 1.5f;
    public float speedDuration = 8f;

    private bool collected;

    private void Awake()
    {
        Collider[] itemColliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < itemColliders.Length; i++)
            itemColliders[i].isTrigger = true;

        if (itemColliders.Length == 0)
        {
            SphereCollider pickupCollider = gameObject.AddComponent<SphereCollider>();
            pickupCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        TankHealth tank = other.GetComponentInParent<TankHealth>();
        if (tank == null || !tank.isActiveAndEnabled)
            return;

        // CHỐT CHẶN 1: Bỏ qua kiểm tra quyền mạng nếu xe tăng là Bot Offline (ViewID = 0)
        if (PhotonNetwork.InRoom)
        {
            PhotonView tankPv = tank.GetComponent<PhotonView>();
            if (tankPv != null && tankPv.ViewID != 0 && !tankPv.IsMine) 
                return;
        }

        if (!tank.CanApplyPickup(type))
            return;

        if (tank.ApplyPickup(type, healAmount, speedMultiplier, speedDuration))
        {
            collected = true;

            // CHỐT CHẶN 2: Chỉ gửi lệnh xóa qua mạng nếu vật phẩm có ID mạng
            if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID != 0)
            {
                photonView.RPC("RPC_DestroyPickup", RpcTarget.MasterClient);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    [PunRPC]
    private void RPC_DestroyPickup()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}