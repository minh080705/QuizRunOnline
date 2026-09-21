using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private float shakeTimer = 0f;
    private float shakeMagnitude = 0f;

    // Camera Follow sẽ đọc giá trị này để cộng vào vị trí gốc
    public Vector3 ShakeOffset { get; private set; }

    public void Shake(float duration, float magnitude)
    {
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }

    private void Update()
    {
        if (shakeTimer > 0f)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            ShakeOffset = new Vector3(x, y, 0f);

            shakeTimer -= Time.deltaTime;
        }
        else
        {
            ShakeOffset = Vector3.zero;
        }
    }
}