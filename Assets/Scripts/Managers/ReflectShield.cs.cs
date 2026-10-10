using System.Collections.Generic;
using UnityEngine;

public class ReflectShield : MonoBehaviour
{
    public float shieldDuration = 5f;
    private Transform tank;
    private Transform glow;
    private Vector3 glowBaseScale;
    private readonly HashSet<int> reflectedProjectiles = new HashSet<int>();

    public void AttachToTank(Transform tankTransform)
    {
        tank = tankTransform;
        Collider shieldCollider = GetComponent<Collider>();
        if (shieldCollider == null)
            shieldCollider = gameObject.AddComponent<SphereCollider>();
        shieldCollider.isTrigger = true;
        SphereCollider sphereCollider = shieldCollider as SphereCollider;
        if (sphereCollider != null)
        {
            Bounds tankBounds = GetTankColliderBounds();
            sphereCollider.center = transform.InverseTransformPoint(tankBounds.center);
            sphereCollider.radius = GetTankShieldRadius(tankBounds);
        }
        CreateGlowEffect();
        Destroy(gameObject, shieldDuration);
    }

    private void Update()
    {
        if (glow == null)
            return;

        float pulse = 1f + Mathf.Sin(Time.time * 7f) * 0.08f;
        glow.localScale = glowBaseScale * pulse;
        glow.Rotate(Vector3.up, 90f * Time.deltaTime, Space.Self);
    }

    private void CreateGlowEffect()
    {
        GameObject glowObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glowObject.name = "Reflect Shield Glow";
        glowObject.transform.SetParent(transform, false);
        glow = glowObject.transform;
        glow.localScale = Vector3.one * 2.4f;
        glowBaseScale = glow.localScale;

        Collider glowCollider = glowObject.GetComponent<Collider>();
        if (glowCollider != null)
            Destroy(glowCollider);

        Renderer glowRenderer = glowObject.GetComponent<Renderer>();
        Shader glowShader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (glowShader == null)
            glowShader = Shader.Find("Standard");
        if (glowShader == null)
        {
            Destroy(glowObject);
            glow = null;
            Debug.LogError("Could not create ReflectShield glow material.");
            return;
        }

        Material glowMaterial = new Material(glowShader);
        glowMaterial.color = new Color(0.05f, 0.8f, 1f, 0.28f);
        glowRenderer.material = glowMaterial;
    }

    private Bounds GetTankColliderBounds()
    {
        Collider[] colliders = tank.GetComponentsInChildren<Collider>(true);
        Bounds bounds = new Bounds(tank.position, Vector3.zero);
        bool hasBounds = false;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null || colliders[i].isTrigger)
                continue;

            if (!hasBounds)
            {
                bounds = colliders[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(colliders[i].bounds);
            }
        }

        if (hasBounds)
            return bounds;

        Renderer[] renderers = tank.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return hasBounds ? bounds : new Bounds(tank.position, Vector3.one);
    }

    private float GetTankShieldRadius(Bounds tankBounds)
    {
        return Mathf.Max(tankBounds.extents.x, tankBounds.extents.z) + 0.1f;
    }

    private void OnTriggerEnter(Collider other)
    {
        ReflectProjectile(other);
    }

    public void ReflectProjectile(Collider projectileCollider)
    {
        Rigidbody shellRb = projectileCollider.attachedRigidbody;
        if (shellRb == null || shellRb.transform == tank)
            return;

        int projectileId = shellRb.GetInstanceID();
        if (!reflectedProjectiles.Add(projectileId))
            return;

        ShellExplosion shell = projectileCollider.GetComponentInParent<ShellExplosion>();
        HomingMissile missile = projectileCollider.GetComponentInParent<HomingMissile>();
        if (shell == null && missile == null)
            return;

        if (shellRb.velocity.sqrMagnitude > 0.01f)
        {
            shellRb.velocity = -shellRb.velocity;
            projectileCollider.transform.forward = -projectileCollider.transform.forward;
        }

        if (missile != null)
            missile.ReflectFrom(tank);
    }
}