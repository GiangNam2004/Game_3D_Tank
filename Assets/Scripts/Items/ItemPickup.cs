using UnityEngine;
using Photon.Pun; 

public enum PickupType
{
    ToolChest,
    FuelTank,
    ReflectShield
}

public class ItemPickup : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    public PickupType type;
    public float healAmount = 35f;
    public float speedMultiplier = 1.5f;
    public float speedDuration = 8f;

    private bool collected;

    private void Awake()
    {
        AutoConfigure();
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        AutoConfigure();
    }

    private void AutoConfigure()
    {
        // Nếu được gắn làm khiên trên xe tăng, không xử lý như vật phẩm nhặt
        if (transform.parent != null && transform.parent.GetComponentInParent<TankHealth>() != null)
            return;

        // Tự động nhận diện loại vật phẩm dựa trên tên GameObject nếu chưa thiết lập
        string objName = gameObject.name;
        if (objName.Contains("FuelTank"))
            type = PickupType.FuelTank;
        else if (objName.Contains("ReflectShield"))
            type = PickupType.ReflectShield;
        else if (objName.Contains("ToolChest"))
            type = PickupType.ToolChest;

        Collider[] itemColliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < itemColliders.Length; i++)
            itemColliders[i].isTrigger = true;

        if (itemColliders.Length == 0)
        {
            SphereCollider pickupCollider = gameObject.AddComponent<SphereCollider>();
            pickupCollider.isTrigger = true;
        }

        ItemScaleUtility.MatchTankSize(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        // Nếu đây là khiên đang gắn trên xe tăng, bỏ qua
        if (transform.parent != null && transform.parent.GetComponentInParent<TankHealth>() != null)
            return;

        TankHealth tank = other.GetComponentInParent<TankHealth>();
        if (tank == null || !tank.isActiveAndEnabled)
            return;

        // CHỐT CHẶN 1: Bỏ qua kiểm tra quyền mạng nếu xe tăng là Bot Offline (ViewID = 0)
        // Khi Online: Chỉ có người sở hữu xe tăng chạm vào vật phẩm mới kích hoạt nhặt trên máy của họ
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

            // Phát hiệu ứng nhặt vật phẩm trên tất cả các máy
            if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID != 0)
            {
                photonView.RPC("RPC_PlayPickupEffect", RpcTarget.All, transform.position, (int)type);
            }
            else
            {
                PlayPickupEffectAtPosition(transform.position, type);
            }

            // Ẩn vật phẩm ngay lập tức trên máy cục bộ để phản hồi mượt mà
            HidePickup();

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

    private void HidePickup()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = false;
    }

    [PunRPC]
    public void RPC_PlayPickupEffect(Vector3 pos, int pickupType)
    {
        PlayPickupEffectAtPosition(pos, (PickupType)pickupType);
    }

    private static void PlayPickupEffectAtPosition(Vector3 pos, PickupType pickupType)
    {
        GameObject effectObj = new GameObject("PickupVFX");
        effectObj.transform.position = pos + Vector3.up * 0.5f;

        ParticleSystem ps = effectObj.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = 0.6f;
        main.startSpeed = 4f;
        main.startSize = 0.35f;
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.Destroy;

        Color effectColor = Color.white;
        if (pickupType == PickupType.ToolChest) effectColor = Color.green;
        else if (pickupType == PickupType.FuelTank) effectColor = Color.yellow;
        else if (pickupType == PickupType.ReflectShield) effectColor = Color.cyan;
        main.startColor = effectColor;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        ParticleSystemRenderer psRenderer = effectObj.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (particleShader == null) particleShader = Shader.Find("Sprites/Default");
        if (particleShader != null)
        {
            psRenderer.material = new Material(particleShader);
        }

        ps.Play();
        Destroy(effectObj, 1.2f);
    }

    [PunRPC]
    private void RPC_DestroyPickup()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (gameObject != null)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}