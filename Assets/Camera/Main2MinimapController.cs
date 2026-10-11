using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class Main2MinimapController : MonoBehaviour
{
    [SerializeField] private float panelSize = 110f;
    [SerializeField] private float screenPadding = 20f;
    [SerializeField] private float worldRadius = 60f;
    [SerializeField] private float markerSize = 8f;

    private readonly List<RectTransform> opponentMarkers = new List<RectTransform>();
    private Transform player;
    private RectTransform radarRect;
    private Texture2D radarTexture;
    private Sprite radarSprite;
    private Texture2D arrowTexture;
    private Sprite arrowSprite;

    private void Awake()
    {
        CreateMinimapUI();
    }

    private void LateUpdate()
    {
        if (player == null)
            FindPlayer();

        if (player != null)
            UpdateOpponentMarkers();
    }

    private void CreateMinimapUI()
    {
        GameObject canvasObject = new GameObject("Main 2 Minimap Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject radarObject = new GameObject("Main 2 Circular Minimap");
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

        CreateMarker("Player Marker", Color.green, markerSize + 3f, radarRect, false);
    }

    private void UpdateOpponentMarkers()
    {
        PhotonView[] views = FindObjectsOfType<PhotonView>();
        int markerIndex = 0;
        float radarRadius = Mathf.Max(1f, panelSize * 0.5f - markerSize * 0.5f);

        for (int i = 0; i < views.Length; i++)
        {
            PhotonView view = views[i];
            TankMovement tank = view.GetComponent<TankMovement>();
            if (view.IsMine || tank == null || !view.gameObject.activeInHierarchy)
                continue;

            RectTransform marker = GetOpponentMarker(markerIndex++);
            Vector3 offset = view.transform.position - player.position;
            Vector2 radarPosition = new Vector2(offset.x, offset.z);
            radarPosition *= radarRadius / Mathf.Max(0.01f, worldRadius);
            if (radarPosition.sqrMagnitude > radarRadius * radarRadius)
                radarPosition = radarPosition.normalized * radarRadius;

            marker.localPosition = radarPosition;
            marker.localRotation = Quaternion.Euler(0f, 0f, -view.transform.eulerAngles.y);
        }

        for (int i = markerIndex; i < opponentMarkers.Count; i++)
            opponentMarkers[i].gameObject.SetActive(false);
    }

    private RectTransform GetOpponentMarker(int index)
    {
        if (index < opponentMarkers.Count)
        {
            opponentMarkers[index].gameObject.SetActive(true);
            return opponentMarkers[index];
        }

        return CreateMarker("Opponent Marker", Color.red, markerSize, radarRect, true, opponentMarkers);
    }

    private RectTransform CreateMarker(
        string markerName,
        Color color,
        float size,
        Transform parent,
        bool useArrow,
        List<RectTransform> markerList = null)
    {
        GameObject markerObject = new GameObject(markerName);
        markerObject.transform.SetParent(parent, false);
        SetUILayer(markerObject);

        Image markerImage = markerObject.AddComponent<Image>();
        markerImage.sprite = useArrow ? CreateArrowSprite() : CreateCircularSprite();
        markerImage.color = color;
        markerImage.raycastTarget = false;

        RectTransform markerRect = markerImage.rectTransform;
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        markerRect.sizeDelta = new Vector2(size, size);

        if (markerList != null)
            markerList.Add(markerRect);

        return markerRect;
    }

    private void FindPlayer()
    {
        PhotonView[] views = FindObjectsOfType<PhotonView>();
        for (int i = 0; i < views.Length; i++)
        {
            PhotonView view = views[i];
            if (view.IsMine &&
                view.GetComponent<TankMovement>() != null &&
                view.gameObject.activeInHierarchy)
            {
                player = view.transform;
                return;
            }
        }
    }

    private Sprite CreateArrowSprite()
    {
        if (arrowSprite != null)
            return arrowSprite;

        arrowTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        arrowTexture.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(15.5f, 15.5f);
        for (int y = 0; y < arrowTexture.height; y++)
        {
            for (int x = 0; x < arrowTexture.width; x++)
            {
                float top = center.y + 11f - Mathf.Abs(x - center.x) * 0.75f;
                float bottom = center.y - 10f + Mathf.Abs(x - center.x) * 0.45f;
                bool filled = y <= top && y >= bottom;
                arrowTexture.SetPixel(x, y, filled ? Color.white : Color.clear);
            }
        }
        arrowTexture.Apply();
        arrowSprite = Sprite.Create(
            arrowTexture,
            new Rect(0f, 0f, arrowTexture.width, arrowTexture.height),
            new Vector2(0.5f, 0.5f));
        return arrowSprite;
    }

    private Sprite CreateCircularSprite()
    {
        if (radarSprite != null)
            return radarSprite;

        radarTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
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
        radarSprite = Sprite.Create(radarTexture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f));
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
        if (arrowSprite != null)
            Destroy(arrowSprite);
        if (arrowTexture != null)
            Destroy(arrowTexture);
    }
}
