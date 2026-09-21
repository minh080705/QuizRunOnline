using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DistanceIndicatorManager : MonoBehaviour
{
    [SerializeField] private IndicatorUI indicatorPrefab;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private float edgePadding = 60f;

    [Header("Co giãn theo khoảng cách")]
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 50f;
    [SerializeField] private float maxScale = 1f;
    [SerializeField] private float minScale = 0.35f;
    [Header("Vị trí hiển thị")]
    [SerializeField] private float heightOffset = 2f; // chiều cao phía trên đầu nhân vật, đơn vị world (mét)
    private Dictionary<Transform, IndicatorUI> _indicators = new();

    void Update()
    {
        Transform localPlayerTransform = PlayerContext.Instance?.LocalPlayer;
        if (localPlayerTransform == null) return;

        var allPlayers = FindObjectsOfType<PlayerStats>();
        var currentTargets = new HashSet<Transform>();
        Camera cam = Camera.main;
        if (cam == null) return;

        foreach (var p in allPlayers)
        {
            if (p.transform == localPlayerTransform) continue;
            currentTargets.Add(p.transform);

            if (!_indicators.TryGetValue(p.transform, out var indicator))
            {
                indicator = Instantiate(indicatorPrefab, canvasRect);
                _indicators[p.transform] = indicator;
            }

            UpdateIndicator(indicator, p, localPlayerTransform, cam);
        }

        CleanupMissingIndicators(currentTargets);
    }

    void UpdateIndicator(IndicatorUI indicator, PlayerStats targetStats, Transform localPlayer, Camera cam)
    {
        Transform target = targetStats.transform;
        RectTransform rt = indicator.RectTransform;

        Vector3 worldPosWithOffset = target.position + Vector3.up * heightOffset; 
        Vector3 screenPos = cam.WorldToScreenPoint(worldPosWithOffset); 

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 halfBounds = new Vector2(Screen.width / 2f - edgePadding, Screen.height / 2f - edgePadding);

        bool behindCamera = screenPos.z < 0;

        Vector2 rawPos = behindCamera
            ? screenCenter - (new Vector2(screenPos.x, screenPos.y) - screenCenter)
            : new Vector2(screenPos.x, screenPos.y);

        bool isOffscreen = behindCamera
            || rawPos.x < edgePadding || rawPos.x > Screen.width - edgePadding
            || rawPos.y < edgePadding || rawPos.y > Screen.height - edgePadding;

        Vector2 finalPos;
        if (isOffscreen)
        {
            Vector2 dirFromCenter = rawPos - screenCenter;
            float tx = dirFromCenter.x != 0 ? halfBounds.x / Mathf.Abs(dirFromCenter.x) : float.MaxValue;
            float ty = dirFromCenter.y != 0 ? halfBounds.y / Mathf.Abs(dirFromCenter.y) : float.MaxValue;
            float t = Mathf.Min(tx, ty);
            finalPos = screenCenter + dirFromCenter * t;
        }
        else
        {
            finalPos = rawPos;
        }

        rt.position = new Vector3(finalPos.x, finalPos.y, 0f);

        float distance = Vector3.Distance(localPlayer.position, target.position);
        float t01 = Mathf.InverseLerp(minDistance, maxDistance, distance);
        float scale = Mathf.Lerp(maxScale, minScale, t01);
        rt.localScale = Vector3.one * scale;

        if (targetStats.isDead)
            indicator.SetDead();
        else
            indicator.SetAlive(distance);
    }

    void CleanupMissingIndicators(HashSet<Transform> currentTargets)
    {
        var toRemove = _indicators.Keys.Where(k => k == null || !currentTargets.Contains(k)).ToList();
        foreach (var key in toRemove)
        {
            if (_indicators[key] != null) Destroy(_indicators[key].gameObject);
            _indicators.Remove(key);
        }
    }
}