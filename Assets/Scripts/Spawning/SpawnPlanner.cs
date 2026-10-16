using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Lập kế hoạch spawn kiểu Subway Surfers. Logic thuần (không MonoBehaviour, không mạng): ObstacleSpawnPointGenerator
/// (chỉ Master) gọi PlanNextRow lặp lại, mỗi lần được các SpawnEntry của một "hàng vật cản" cùng coin/item trong
/// khoảng trống phía sau nó. Dùng UnityEngine.Random (chỉ Master gọi nên kết quả không cần giống nhau giữa các máy).
///
/// Nguyên tắc:
/// - Độ khó tăng theo tiến độ p: hàng dày hơn (rowGapSeconds giảm), mở khóa dần các mẫu khó.
/// - Khoảng cách giữa hai hàng = thời gian × tốc độ DỰ KIẾN tại vị trí đó (càng xa càng nhanh, nên cùng thời gian né
///   ứng với số mét lớn hơn).
/// - Luôn có đường đi: từ MỌI làn qua được của hàng trước phải tới được một làn qua được của hàng sau trong thời gian
///   còn lại (mỗi lần đổi 1 làn mất laneChangeTime, cộng reactionMargin). Không thỏa thì bỏ hàng (hàng trống).
/// - Coin/item chỉ đặt vào làn trống ở CẢ hàng trước lẫn hàng sau, không bao giờ nằm trong vật cản.
/// </summary>
public static class SpawnPlanner
{
    private enum RowPattern { Empty, Single, Double, Staggered, LowWall, Overhead, Mixed }

    public static PlannerState InitState(SpawnProfile profile, float trackStartZ)
    {
        int all = (1 << profile.LaneCount) - 1;
        return new PlannerState
        {
            Initialized = true,
            HasPrevRow = false,
            NextRowZ = trackStartZ + profile.firstRowOffset,
            LastRowZ = trackStartZ,
            LastRowLength = 0f,
            LastFreeMask = all,
            LastPassableMask = all,
            LastRowAction = false,
            ChainRemaining = 0,
            NextBuffZ = trackStartZ + profile.buffEveryMeters * 0.6f,
            NextQuestionZ = trackStartZ + profile.questionEveryMeters.Evaluate(0f) * 0.6f,
            NextId = 0,
        };
    }

    /// <summary>
    /// Sinh hàng vật cản tại st.NextRowZ (+ coin/item trong khoảng trống phía sau nó) vào output rồi tiến st.NextRowZ.
    /// Người gọi phải bảo đảm đủ chỗ cho output.Count entry; nếu không đủ thì bỏ st và output, KHÔNG dùng kết quả.
    /// </summary>
    public static void PlanNextRow(SpawnProfile profile, ref PlannerState st, SpeedModel speed,
        float trackStartZ, float trackLength, List<SpawnEntry> output)
    {
        int laneCount = profile.LaneCount;
        int all = (1 << laneCount) - 1;

        float z = st.NextRowZ;
        float dist = Mathf.Max(0f, z - trackStartZ);
        float p = trackLength > 0f ? Mathf.Clamp01(dist / trackLength) : 0f;
        float v = Mathf.Max(1f, speed.At(dist));

        bool hasPrev = st.HasPrevRow;
        float timeGap = hasPrev ? (z - st.LastRowZ) / v : 99f;
        int maxShift = Mathf.Clamp(
            Mathf.FloorToInt((timeGap - profile.reactionMargin) / Mathf.Max(0.01f, profile.laneChangeTime)),
            0, laneCount - 1);
        bool actionOk = !hasPrev || !st.LastRowAction || timeGap >= profile.ActionLockTime;

        RowPattern pattern;
        bool force = false;
        if (st.ChainRemaining > 0)
        {
            pattern = RowPattern.Single; // chuỗi so le: mỗi hàng ép phải đổi làn
            force = true;
            st.ChainRemaining--;
        }
        else
        {
            pattern = PickPattern(profile, p, actionOk);
        }

        int freeMask = all;
        int passableMask = all;
        float rowLength = 0f;
        bool action = false;

        switch (pattern)
        {
            case RowPattern.Single:
            case RowPattern.Staggered:
                if (!TryBlockRow(profile, ref st, laneCount, all, 1, force, maxShift, z, output,
                        out freeMask, out passableMask, out rowLength))
                    pattern = RowPattern.Empty;
                break;

            case RowPattern.Double:
                if (!TryBlockRow(profile, ref st, laneCount, all, 2, force, maxShift, z, output,
                        out freeMask, out passableMask, out rowLength))
                    pattern = RowPattern.Empty;
                break;

            case RowPattern.LowWall:
                BuildActionRow(profile, ref st, laneCount, ObstacleKind.Low, z, output, out rowLength);
                freeMask = 0; passableMask = all; action = true;
                break;

            case RowPattern.Overhead:
                BuildActionRow(profile, ref st, laneCount, ObstacleKind.Overhead, z, output, out rowLength);
                freeMask = 0; passableMask = all; action = true;
                break;

            case RowPattern.Mixed:
                if (TryMixedRow(profile, ref st, laneCount, all, maxShift, z, output,
                        out freeMask, out passableMask, out rowLength))
                    action = true;
                else
                    pattern = RowPattern.Empty;
                break;
        }

        // Chuỗi so le chỉ bắt đầu khi hàng đầu của chuỗi dựng được.
        if (pattern == RowPattern.Staggered)
            st.ChainRemaining = Random.Range(2, 4);

        if (hasPrev)
        {
            float zStart = st.LastRowZ + st.LastRowLength * 0.5f + profile.coinPadding;
            float zEnd = z - rowLength * 0.5f - profile.coinPadding;
            FillGap(profile, ref st, p, v, laneCount, all, st.LastFreeMask & freeMask, z, zStart, zEnd, output);
        }

        // Vòng cung coin trên không phía trên hàng vật cản thấp (người chơi nhảy qua sẽ nhặt được).
        if (pattern == RowPattern.LowWall && Random.value < profile.coinChance.Evaluate(p))
        {
            float arcLength = Mathf.Min(v * profile.jumpDuration, profile.maxArcLength);
            // Vòng cung kéo dài ±arcLength/2 quanh hàng này: không để chạm tới hàng vật cản phía trước.
            if (hasPrev)
                arcLength = Mathf.Min(arcLength, (z - st.LastRowZ - st.LastRowLength * 0.5f - rowLength * 0.5f) * 0.8f);
            arcLength = Mathf.Max(arcLength, 4f);
            int count = Mathf.Clamp(Mathf.RoundToInt(arcLength / Mathf.Max(0.5f, profile.coinSpacing)) + 1, 5, 15);
            int lane = Random.Range(0, laneCount);
            AddEntry(output, ref st, SpawnKind.Coin, 0, CoinPattern.JumpArc, count, profile.laneX[lane], z, arcLength);
        }

        float gap = Mathf.Max(profile.minRowGap, profile.rowGapSeconds.Evaluate(p) * v);
        if (action)
            gap = Mathf.Max(gap, profile.ActionLockTime * v); // hồi lại sau khi nhảy/trượt
        if (st.ChainRemaining > 0)
            gap = Mathf.Max(gap * 0.85f, (profile.laneChangeTime + profile.reactionMargin) * v * 1.1f);
        gap = Mathf.Max(gap, rowLength + 2f * profile.coinPadding);

        st.LastRowZ = z;
        st.HasPrevRow = true;
        st.LastRowLength = rowLength;
        st.LastFreeMask = freeMask;
        st.LastPassableMask = passableMask;
        st.LastRowAction = action;
        st.NextRowZ = z + gap;
    }

    private static RowPattern PickPattern(SpawnProfile profile, float p, bool actionOk)
    {
        bool hasBlock = profile.HasKind(ObstacleKind.BlockLane, LaneSpan.SingleLane);
        // Hàng nhảy/trượt dựng được bằng vật một làn (mỗi làn một cái) hoặc một vật lớn phủ cả đường.
        bool hasLowSingle = profile.HasKind(ObstacleKind.Low, LaneSpan.SingleLane);
        bool hasLow = hasLowSingle || profile.HasKind(ObstacleKind.Low, LaneSpan.FullWidth);
        bool hasOver = profile.HasKind(ObstacleKind.Overhead, LaneSpan.SingleLane)
                       || profile.HasKind(ObstacleKind.Overhead, LaneSpan.FullWidth);

        float dbl = p >= profile.doubleUnlock ? Mathf.Clamp01(profile.doubleLaneChance.Evaluate(p)) : 0f;

        float wSingle = hasBlock ? Mathf.Max(0.05f, 1f - dbl) : 0f;
        float wDouble = hasBlock ? dbl : 0f;
        float wStaggered = hasBlock && p >= profile.staggeredUnlock ? profile.staggeredWeight : 0f;
        float wLow = actionOk && hasLow && p >= profile.lowWallUnlock ? profile.lowWallWeight : 0f;
        float wOver = actionOk && hasOver && p >= profile.overheadUnlock ? profile.overheadWeight : 0f;
        // Mẫu hỗn hợp đặt vật thấp ở MỘT làn nên cần prefab Low một làn (vật phủ cả đường không dùng được ở đây).
        float wMixed = actionOk && hasLowSingle && hasBlock && p >= profile.mixedUnlock ? profile.mixedWeight : 0f;

        float total = wSingle + wDouble + wStaggered + wLow + wOver + wMixed;
        if (total <= 0f) return RowPattern.Empty;

        float roll = Random.Range(0f, total);
        if ((roll -= wSingle) < 0f) return RowPattern.Single;
        if ((roll -= wDouble) < 0f) return RowPattern.Double;
        if ((roll -= wStaggered) < 0f) return RowPattern.Staggered;
        if ((roll -= wLow) < 0f) return RowPattern.LowWall;
        if ((roll -= wOver) < 0f) return RowPattern.Overhead;
        return RowPattern.Mixed;
    }

    /// <summary>Chặn blockCount làn bằng vật cản BlockLane, chọn tổ hợp mà từ hàng trước vẫn tới được.</summary>
    private static bool TryBlockRow(SpawnProfile profile, ref PlannerState st, int laneCount, int all, int blockCount,
        bool force, int maxShift, float z, List<SpawnEntry> output, out int freeMask, out int passableMask, out float rowLength)
    {
        freeMask = all;
        passableMask = all;
        rowLength = 0f;

        if (!profile.HasKind(ObstacleKind.BlockLane, LaneSpan.SingleLane)) return false;

        var masks = new List<int>(8);
        for (int m = 1; m < all; m++)
            if (PopCount(m) == blockCount) masks.Add(m);
        if (masks.Count == 0) return false;

        Shuffle(masks);

        // Chuỗi so le: ưu tiên tổ hợp chặn đúng làn người chơi đang có thể đứng, buộc họ đổi làn.
        if (force)
        {
            int prevPass = st.LastPassableMask;
            masks.Sort((a, b) => ((b & prevPass) != 0 ? 1 : 0).CompareTo((a & prevPass) != 0 ? 1 : 0));
        }

        foreach (int m in masks)
        {
            int free = all & ~m;
            if (!Reachable(st.LastPassableMask, free, maxShift, laneCount)) continue;

            for (int lane = 0; lane < laneCount; lane++)
            {
                if ((m & Bit(lane)) == 0) continue;

                int variant = profile.PickObstacle(ObstacleKind.BlockLane);
                AddEntry(output, ref st, SpawnKind.Obstacle, variant, CoinPattern.Line, 0, profile.laneX[lane], z, 0f);
                rowLength = Mathf.Max(rowLength, profile.ObstacleLength(variant));
            }

            freeMask = free;
            passableMask = free;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Hàng phủ cả đường bằng vật cản thấp (nhảy) hoặc trên cao (trượt): làn nào cũng qua được bằng hành động.
    /// Dựng bằng MỘT vật lớn FullWidth ở giữa đường (một entry) hoặc mỗi làn một vật SingleLane (nhiều entry).
    /// </summary>
    private static void BuildActionRow(SpawnProfile profile, ref PlannerState st, int laneCount, ObstacleKind kind,
        float z, List<SpawnEntry> output, out float rowLength)
    {
        rowLength = 0f;

        bool hasFull = profile.HasKind(kind, LaneSpan.FullWidth);
        bool hasSingle = profile.HasKind(kind, LaneSpan.SingleLane);
        if (hasFull && (!hasSingle || Random.value < profile.fullWidthChance))
        {
            int bigVariant = profile.PickObstacle(kind, LaneSpan.FullWidth);
            AddEntry(output, ref st, SpawnKind.Obstacle, bigVariant, CoinPattern.Line, 0, profile.CenterX, z, 0f);
            rowLength = profile.ObstacleLength(bigVariant);
            return;
        }

        for (int lane = 0; lane < laneCount; lane++)
        {
            int variant = profile.PickObstacle(kind);
            if (variant < 0) continue;

            AddEntry(output, ref st, SpawnKind.Obstacle, variant, CoinPattern.Line, 0, profile.laneX[lane], z, 0f);
            rowLength = Mathf.Max(rowLength, profile.ObstacleLength(variant));
        }
    }

    /// <summary>Một làn chặn hẳn, một làn thấp (nhảy qua), còn lại trống.</summary>
    private static bool TryMixedRow(SpawnProfile profile, ref PlannerState st, int laneCount, int all, int maxShift,
        float z, List<SpawnEntry> output, out int freeMask, out int passableMask, out float rowLength)
    {
        freeMask = all;
        passableMask = all;
        rowLength = 0f;

        if (!profile.HasKind(ObstacleKind.BlockLane, LaneSpan.SingleLane) || !profile.HasKind(ObstacleKind.Low, LaneSpan.SingleLane))
            return false;

        var lanes = new List<int>(laneCount);
        for (int i = 0; i < laneCount; i++) lanes.Add(i);
        Shuffle(lanes);

        foreach (int blockLane in lanes)
        {
            foreach (int lowLane in lanes)
            {
                if (lowLane == blockLane) continue;

                int free = all & ~Bit(blockLane) & ~Bit(lowLane);
                if (free == 0) continue; // luôn chừa ít nhất một làn hoàn toàn trống

                int passable = free | Bit(lowLane);
                if (!Reachable(st.LastPassableMask, passable, maxShift, laneCount)) continue;

                int blockVariant = profile.PickObstacle(ObstacleKind.BlockLane);
                int lowVariant = profile.PickObstacle(ObstacleKind.Low);
                AddEntry(output, ref st, SpawnKind.Obstacle, blockVariant, CoinPattern.Line, 0, profile.laneX[blockLane], z, 0f);
                AddEntry(output, ref st, SpawnKind.Obstacle, lowVariant, CoinPattern.Line, 0, profile.laneX[lowLane], z, 0f);

                rowLength = Mathf.Max(profile.ObstacleLength(blockVariant), profile.ObstacleLength(lowVariant));
                freeMask = free;
                passableMask = passable;
                return true;
            }
        }

        return false;
    }

    private static void FillGap(SpawnProfile profile, ref PlannerState st, float p, float v, int laneCount, int all,
        int freeMask, float rowZ, float zStart, float zEnd, List<SpawnEntry> output)
    {
        float usable = zEnd - zStart;
        if (usable < profile.minFillLength || freeMask == 0) return;

        float midZ = (zStart + zEnd) * 0.5f;

        int itemLane = -1;
        if (rowZ >= st.NextQuestionZ && profile.questionPrefabs != null && profile.questionPrefabs.Length > 0)
        {
            itemLane = RandomLane(freeMask, laneCount);
            AddEntry(output, ref st, SpawnKind.Question, Random.Range(0, profile.questionPrefabs.Length),
                CoinPattern.Line, 1, profile.laneX[itemLane], midZ, 0f);
            st.NextQuestionZ = rowZ + profile.questionEveryMeters.Evaluate(p) * Random.Range(0.8f, 1.2f);
        }
        else if (rowZ >= st.NextBuffZ && profile.buffPrefabs != null && profile.buffPrefabs.Length > 0)
        {
            itemLane = RandomLane(freeMask, laneCount);
            AddEntry(output, ref st, SpawnKind.Buff, Random.Range(0, profile.buffPrefabs.Length),
                CoinPattern.Line, 1, profile.laneX[itemLane], midZ, 0f);
            st.NextBuffZ = rowZ + profile.buffEveryMeters * Random.Range(0.8f, 1.2f);
        }

        if (profile.coinPrefab == null || Random.value >= profile.coinChance.Evaluate(p)) return;

        int coinMask = itemLane >= 0 ? freeMask & ~Bit(itemLane) : freeMask; // không đặt coin chồng lên item
        float spacing = Mathf.Max(0.5f, profile.coinSpacing);

        float wLine = coinMask != 0 ? profile.lineWeight : 0f;
        float wWave = coinMask != 0 ? profile.waveWeight : 0f;
        float zigStep = Mathf.Max(spacing * 2f, (profile.laneChangeTime + profile.reactionMargin) * v);
        // ZigZag đi qua mọi làn nên cần cả khoảng trống không có vật cản ở bất kỳ làn nào, và không có item.
        float wZig = (freeMask == all && itemLane < 0) ? profile.zigzagWeight : 0f;

        float total = wLine + wWave + wZig;
        if (total <= 0f) return;

        float roll = Random.Range(0f, total);
        int maxFit = Mathf.FloorToInt(usable / spacing) + 1;

        if ((roll -= wLine) < 0f)
        {
            int count = Mathf.Min(Random.Range(profile.minLineCoins, profile.maxLineCoins + 1), maxFit);
            if (count < 3) return;
            PlaceCoins(profile, ref st, output, CoinPattern.Line, count, spacing, RandomLane(coinMask, laneCount), 0, zStart, usable);
        }
        else if ((roll -= wWave) < 0f)
        {
            int count = Mathf.Min(Random.Range(profile.waveMinCoins, profile.waveMaxCoins + 1), maxFit);
            if (count < 6) return;
            PlaceCoins(profile, ref st, output, CoinPattern.AirWave, count, spacing, RandomLane(coinMask, laneCount), 0, zStart, usable);
        }
        else
        {
            int maxFitZig = Mathf.FloorToInt(usable / zigStep) + 1;
            int count = Mathf.Min(Random.Range(profile.zigzagMinCoins, profile.zigzagMaxCoins + 1), maxFitZig);
            if (count < 4) return;

            int startLane = Random.Range(0, laneCount);
            int dir = startLane == 0 ? 0 : startLane == laneCount - 1 ? 1 : Random.Range(0, 2);
            PlaceCoins(profile, ref st, output, CoinPattern.ZigZag, count, zigStep, startLane, dir, zStart, usable);
        }
    }

    // Đặt mẫu coin căn giữa trong khoảng trống [zStart, zStart + usable].
    private static void PlaceCoins(SpawnProfile profile, ref PlannerState st, List<SpawnEntry> output, CoinPattern pattern,
        int count, float step, int lane, int variant, float zStart, float usable)
    {
        float length = (count - 1) * step;
        float firstZ = zStart + (usable - length) * 0.5f;
        AddEntry(output, ref st, SpawnKind.Coin, variant, pattern, count, profile.laneX[lane], firstZ, length);
    }

    private static void AddEntry(List<SpawnEntry> output, ref PlannerState st, SpawnKind kind, int variant,
        CoinPattern pattern, int count, float x, float z, float length)
    {
        output.Add(new SpawnEntry
        {
            Id = st.NextId++,
            Kind = (byte)kind,
            Variant = (byte)Mathf.Clamp(variant, 0, 255),
            Pattern = (byte)pattern,
            Count = (byte)Mathf.Clamp(count, 0, 255),
            X = x,
            Z = z,
            Length = length,
            Active = true,
        });
    }

    private static int Bit(int lane) => 1 << lane;

    private static int PopCount(int mask)
    {
        int n = 0;
        while (mask != 0) { n += mask & 1; mask >>= 1; }
        return n;
    }

    private static int RandomLane(int mask, int laneCount)
    {
        var lanes = new List<int>(laneCount);
        for (int i = 0; i < laneCount; i++)
            if ((mask & Bit(i)) != 0) lanes.Add(i);
        return lanes.Count == 0 ? 0 : lanes[Random.Range(0, lanes.Count)];
    }

    private static void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Từ MỌI làn qua được của hàng trước phải có ít nhất một làn qua được của hàng sau trong phạm vi maxShift làn
    /// (mỗi lần đổi một làn). Người chơi có thể đang đứng ở bất kỳ làn nào trong số đó.
    /// </summary>
    public static bool Reachable(int prevMask, int nextMask, int maxShift, int laneCount)
    {
        if (prevMask == 0) return nextMask != 0;

        for (int a = 0; a < laneCount; a++)
        {
            if ((prevMask & Bit(a)) == 0) continue;

            bool ok = false;
            for (int b = 0; b < laneCount; b++)
            {
                if ((nextMask & Bit(b)) != 0 && Mathf.Abs(a - b) <= maxShift) { ok = true; break; }
            }
            if (!ok) return false;
        }
        return true;
    }

    private struct SimObstacle { public int lane; public float zMin; public float zMax; }

    /// <summary>
    /// Chạy lập kế hoạch rows hàng với một seed rồi kiểm tra bất biến: (1) luôn có đường đi giữa hai hàng liền nhau,
    /// (2) coin dưới đất không nằm trong vật cản. Trả về báo cáo văn bản; không đổi trạng thái Random của game.
    /// </summary>
    public static string Simulate(SpawnProfile profile, int seed, int rows, SpeedModel speed, float trackStartZ, float trackLength)
    {
        Random.State saved = Random.state;
        Random.InitState(seed);

        try
        {
            int laneCount = profile.LaneCount;
            PlannerState st = InitState(profile, trackStartZ);
            var output = new List<SpawnEntry>();
            var obstacles = new List<SimObstacle>();
            var coinPositions = new List<Vector3>();

            int violations = 0, obstacleCount = 0, coinEntries = 0, items = 0;
            var patternCounts = new int[4];
            float gapEarly = 0f, gapLate = 0f; int nEarly = 0, nLate = 0;
            var sb = new StringBuilder();

            for (int i = 0; i < rows; i++)
            {
                PlannerState before = st;
                output.Clear();
                PlanNextRow(profile, ref st, speed, trackStartZ, trackLength, output);

                float z = before.NextRowZ;
                float dist = Mathf.Max(0f, z - trackStartZ);
                float v = Mathf.Max(1f, speed.At(dist));

                // (1) khả năng đi tới
                if (before.HasPrevRow)
                {
                    float timeGap = (z - before.LastRowZ) / v;
                    int maxShift = Mathf.Clamp(
                        Mathf.FloorToInt((timeGap - profile.reactionMargin) / Mathf.Max(0.01f, profile.laneChangeTime)),
                        0, laneCount - 1);
                    if (!Reachable(before.LastPassableMask, st.LastPassableMask, maxShift, laneCount))
                    {
                        violations++;
                        sb.AppendLine($"  [VI PHẠM] hàng {i} z={z:F0}: không đi tới được (maxShift={maxShift}, trước={before.LastPassableMask}, sau={st.LastPassableMask})");
                    }
                }

                // (2) gom vật cản và coin của lần này
                foreach (SpawnEntry e in output)
                {
                    if (e.Kind == (byte)SpawnKind.Obstacle)
                    {
                        obstacleCount++;
                        float half = profile.ObstacleLength(e.Variant) * 0.5f;

                        // Vật phủ cả đường chiếm mọi làn; vật một làn chỉ chiếm làn gần nhất.
                        if (profile.IsFullWidth(e.Variant))
                        {
                            for (int l = 0; l < laneCount; l++)
                                obstacles.Add(new SimObstacle { lane = l, zMin = e.Z - half, zMax = e.Z + half });
                        }
                        else
                        {
                            int lane = CoinPatternBuilder.NearestLane(profile, e.X);
                            obstacles.Add(new SimObstacle { lane = lane, zMin = e.Z - half, zMax = e.Z + half });
                        }
                    }
                    else if (e.Kind == (byte)SpawnKind.Coin)
                    {
                        coinEntries++;
                        patternCounts[Mathf.Clamp(e.Pattern, 0, 3)]++;
                    }
                    else
                    {
                        items++;
                    }
                }

                foreach (SpawnEntry e in output)
                {
                    if (e.Kind != (byte)SpawnKind.Coin) continue;

                    CoinPatternBuilder.Build(e, profile, coinPositions);
                    foreach (Vector3 c in coinPositions)
                    {
                        if (c.y > 0.8f) continue; // coin trên không không vướng vật cản dưới đất
                        int lane = CoinPatternBuilder.NearestLane(profile, c.x);
                        foreach (SimObstacle o in obstacles)
                        {
                            if (o.lane == lane && c.z >= o.zMin - 0.5f && c.z <= o.zMax + 0.5f)
                            {
                                violations++;
                                sb.AppendLine($"  [VI PHẠM] coin ({c.x:F1},{c.z:F0}) nằm trong vật cản làn {lane} [{o.zMin:F0}..{o.zMax:F0}]");
                                break;
                            }
                        }
                    }
                }

                // thống kê độ khó: thời gian giữa hai hàng đầu / cuối
                float gapSeconds = (st.NextRowZ - z) / v;
                if (i < rows / 4) { gapEarly += gapSeconds; nEarly++; }
                else if (i >= rows * 3 / 4) { gapLate += gapSeconds; nLate++; }
            }

            string header = $"Simulate seed={seed}: {rows} hàng, {obstacleCount} vật cản, {coinEntries} mẫu coin " +
                            $"(thẳng {patternCounts[0]}, cung {patternCounts[1]}, sóng {patternCounts[2]}, zigzag {patternCounts[3]}), " +
                            $"{items} item. Giãn cách TB: đầu {(nEarly > 0 ? gapEarly / nEarly : 0f):F2}s, cuối {(nLate > 0 ? gapLate / nLate : 0f):F2}s. " +
                            $"Vi phạm: {violations}.";
            return violations == 0 ? header : header + "\n" + sb;
        }
        finally
        {
            Random.state = saved;
        }
    }
}
