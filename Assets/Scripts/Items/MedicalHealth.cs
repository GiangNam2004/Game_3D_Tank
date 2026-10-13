using UnityEngine;
using Photon.Pun;

public class MedicalHealth : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    public float startingHealth = 60f;
    public float dropChance = 0.75f;

    private float currentHealth;
    private bool destroyed;

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
        ItemCollisionUtility.ConfigureSolidObstacle(gameObject);
        ItemScaleUtility.MatchTankSize(gameObject, 2f);
    }

    private void OnEnable()
    {
        currentHealth = startingHealth;
        destroyed = false;
    }

    public void TakeDamage(float amount)
    {
        if (destroyed || amount <= 0f)
            return;

        if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID != 0)
        {
            photonView.RPC("RPC_TakeDamage", RpcTarget.All, amount);
        }
        else
        {
            ApplyDamage(amount);
        }
    }

    [PunRPC]
    private void RPC_TakeDamage(float amount)
    {
        ApplyDamage(amount);
    }

    private void ApplyDamage(float amount)
    {
        if (destroyed || amount <= 0f)
            return;

        currentHealth -= amount;
        if (currentHealth <= 0f)
            DestroyMedical();
    }

    private void DestroyMedical()
    {
        if (destroyed)
            return;

        destroyed = true;
        float groundHeight = ItemScaleUtility.GetBottomHeight(gameObject);
        if (float.IsNaN(groundHeight))
            groundHeight = transform.position.y;

        if (PhotonNetwork.InRoom)
        {
            // Chỉ MasterClient thực hiện thả item và xóa hòm thuốc qua mạng
            if (PhotonNetwork.IsMasterClient)
            {
                ItemDropSystem.TryDrop(transform.position, dropChance, groundHeight);
                PhotonNetwork.Destroy(gameObject);
            }
        }
        else
        {
            ItemDropSystem.TryDrop(transform.position, dropChance, groundHeight);
            Destroy(gameObject);
        }
    }
}
