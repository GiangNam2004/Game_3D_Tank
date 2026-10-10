using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    [SerializeField] private float panelSize = 110f;
    [SerializeField] private float screenPadding = 20f;
    [SerializeField] private float worldRadius = 60f;
    [SerializeField] private float markerSize = 8f;

    private readonly List<RectTransform> enemyMarkers = new List<RectTransform>();
    private Transform player;
    private RectTransform radarRect;
    private Texture2D radarTexture;
    private Sprite radarSprite;

    private void Awake()
    {
        CreateMinimapUI();
    }

    private void LateUpdate()
    {
        if (player == null)
            FindPlayer();

        if (player != null)
            UpdateEnemyMarkers();
    }

    private void CreateMinimapUI()
    {
        GameObject canvasObject = new GameObject("Minimap Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject radarObject = new GameObject("Circular Minimap");
        radarObject.transform.SetParent(canvasObject.transform, false);
        SetUILayer(canvasObject);
        SetUILayer(radarObject);

        Image radarImage = radarObject.AddComponent<Image>();
        radarImage.sprite = CreateCircularSprite();
        radarImage.color = new Color(0.03f, 0.05f, 0.07f, 0.42f);
        radarImage.raycastTarget = false;

        radarRect = radarImage.rectTransform;
        radarRect.anchorMin = new Vector2(1f, 1f);
        radarRect.anchorMax = new Vector2(1f, 1f);
        radarRect.pivot = new Vector2(0.5f, 0.5f);
        radarRect.sizeDelta = new Vector2(panelSize, panelSize);
        radarRect.anchoredPosition = new Vector2(
            -screenPadding - panelSize * 0.5f,
            -screenPadding - panelSize * 0.5f);

        CreateMarker("Player Marker", Color.green, Vector2.zero, markerSize + 3f, radarRect);
    }

    private void UpdateEnemyMarkers()
    {
        TankMovement[] tanks = FindObjectsOfType<TankMovement>();
        int markerIndex = 0;
        float radarRadius = Mathf.Max(1f, panelSize * 0.5f - markerSize * 0.5f);

        for (int i = 0; i < tanks.Length; i++)
        {
            if (tanks[i].m_PlayerNumber < 2 || !tanks[i].isActiveAndEnabled)
                continue;

            RectTransform marker = GetEnemyMarker(markerIndex++);
            Vector3 offset = tanks[i].transform.position - player.position;
            Vector2 worldDirection = new Vector2(offset.x, offset.z);
            Vector2 radarPosition = worldDirection * (radarRadius / Mathf.Max(0.01f, worldRadius));
            float maxDistance = radarRadius * radarRadius;
            if (radarPosition.sqrMagnitude > maxDistance)
                radarPosition = radarPosition.normalized * radarRadius;

            marker.localPosition = radarPosition;
        }

        for (int i = markerIndex; i < enemyMarkers.Count; i++)
            enemyMarkers[i].gameObject.SetActive(false);
    }

    private RectTransform GetEnemyMarker(int index)
    {
        if (index < enemyMarkers.Count)
        {
            enemyMarkers[index].gameObject.SetActive(true);
            return enemyMarkers[index];
        }

        return CreateMarker(
            "Enemy Marker",
            Color.red,
            Vector2.zero,
            markerSize,
            radarRect,
            enemyMarkers);
    }

    private RectTransform CreateMarker(
        string markerName,
        Color color,
        Vector2 position,
        float size,
        Transform parent,
        List<RectTransform> markerList = null)
    {
        GameObject markerObject = new GameObject(markerName);
        markerObject.transform.SetParent(parent, false);
        SetUILayer(markerObject);

        Image markerImage = markerObject.AddComponent<Image>();
        markerImage.sprite = CreateCircularSprite();
        markerImage.color = color;
        markerImage.raycastTarget = false;

        RectTransform markerRect = markerImage.rectTransform;
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        markerRect.sizeDelta = new Vector2(size, size);
        markerRect.localPosition = position;

        if (markerList != null)
            markerList.Add(markerRect);

        return markerRect;
    }

    private void FindPlayer()
    {
        TankMovement[] tanks = FindObjectsOfType<TankMovement>();
        for (int i = 0; i < tanks.Length; i++)
        {
            if (tanks[i].m_PlayerNumber == 1 && tanks[i].isActiveAndEnabled)
            {
                player = tanks[i].transform;
                return;
            }
        }
    }

    private Sprite CreateCircularSprite()
    {
        if (radarSprite != null)
            return radarSprite;

        radarTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        radarTexture.name = "Circular Minimap Texture";
        radarTexture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(31.5f, 31.5f);
        for (int y = 0; y < radarTexture.height; y++)
        {
            for (int x = 0; x < radarTexture.width; x++)
            {
                float alpha = Vector2.Distance(new Vector2(x, y), center) <= 31.5f ? 1f : 0f;
                radarTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        radarTexture.Apply();
        radarSprite = Sprite.Create(
            radarTexture,
            new Rect(0f, 0f, radarTexture.width, radarTexture.height),
            new Vector2(0.5f, 0.5f));
        return radarSprite;
    }

    private void SetUILayer(GameObject target)
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
            target.layer = uiLayer;
    }

    private void OnDestroy()
    {
        if (radarSprite != null)
            Destroy(radarSprite);
        if (radarTexture != null)
            Destroy(radarTexture);
    }
}
