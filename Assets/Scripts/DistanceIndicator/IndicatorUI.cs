using TMPro;
using UnityEngine;

public class IndicatorUI : MonoBehaviour
{
    [SerializeField] private GameObject circleIcon;
    [SerializeField] private GameObject deadIcon;
    [SerializeField] private TMP_Text distanceText;

    public RectTransform RectTransform => (RectTransform)transform;

    public void SetAlive(float distance)
    {
        circleIcon.SetActive(true);
        deadIcon.SetActive(false);
        distanceText.gameObject.SetActive(true);
        distanceText.text = $"{distance:F1}m";
    }

    public void SetDead()
    {
        circleIcon.SetActive(false);
        deadIcon.SetActive(true);
        distanceText.gameObject.SetActive(false);
    }
}