using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "Bung" một SpawnEntry loại Coin thành vị trí từng đồng. Hàm xác định: cùng entry + cùng SpawnProfile
/// thì mọi máy ra đúng cùng một dãy vị trí (client không cần biết tốc độ, vì Length đã do Master tính sẵn).
/// Trả về vị trí (x, yOffset, z); yOffset được cộng vào độ cao gốc của prefab coin.
/// </summary>
public static class CoinPatternBuilder
{
    public static void Build(SpawnEntry e, SpawnProfile profile, List<Vector3> output)
    {
        output.Clear();

        int count = Mathf.Max(1, e.Count);
        float step = count > 1 ? e.Length / (count - 1) : 0f;

        switch ((CoinPattern)e.Pattern)
        {
            case CoinPattern.Line:
                for (int i = 0; i < count; i++)
                    output.Add(new Vector3(e.X, 0f, e.Z + i * step));
                break;

            case CoinPattern.JumpArc:
                // Vòng cung theo đúng quỹ đạo nhảy, tâm tại Z của vật cản thấp.
                for (int i = 0; i < count; i++)
                {
                    float u = count > 1 ? (float)i / (count - 1) : 0.5f;
                    float y = profile.jumpHeight * 0.9f * Mathf.Sin(Mathf.PI * u);
                    output.Add(new Vector3(e.X, y, e.Z - e.Length * 0.5f + u * e.Length));
                }
                break;

            case CoinPattern.AirWave:
                // Y đi 0 -> biên độ -> 0 -> ... : đồng ở đáy sóng chạm đất nhặt được, đồng ở đỉnh phải nhảy.
                for (int i = 0; i < count; i++)
                {
                    float u = count > 1 ? (float)i / (count - 1) : 0f;
                    float y = profile.waveAmplitude * (0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * profile.waveCycles * u));
                    output.Add(new Vector3(e.X, y, e.Z + u * e.Length));
                }
                break;

            case CoinPattern.ZigZag:
                int laneCount = profile.LaneCount;
                int lane = NearestLane(profile, e.X);
                int dir = e.Variant == 0 ? 1 : -1; // 0 = bắt đầu đi sang phải, 1 = sang trái
                for (int i = 0; i < count; i++)
                {
                    output.Add(new Vector3(profile.laneX[lane], 0f, e.Z + i * step));

                    // Đi qua lại giữa hai làn ngoài cùng (ping-pong); chạm biên thì đổi chiều.
                    if (lane + dir < 0 || lane + dir >= laneCount) dir = -dir;
                    lane = Mathf.Clamp(lane + dir, 0, laneCount - 1);
                }
                break;
        }
    }

    public static int NearestLane(SpawnProfile profile, float x)
    {
        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < profile.laneX.Length; i++)
        {
            float d = Mathf.Abs(profile.laneX[i] - x);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }
}
