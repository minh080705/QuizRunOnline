using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class CoinCaculator : MonoBehaviour
{
    public int multiplier = 1 ;
    public static CoinCaculator Instance { get; private set; }
    int tol = 0;
    int savedCoins = 0; // phần của tol đã ghi lên hồ sơ

    public static event Action<int> OnCoinChanged;
    // Không dùng DontDestroyOnLoad: GameObject này (GameManager) còn chứa Speedometer và các script khác của
    // RaceScene, giữ nó lại qua scene làm chúng trỏ vào UI/player của ván trước đã bị hủy.
    // Coin đã được lưu lên hồ sơ nên không cần sống qua scene; mỗi ván tự có bộ đếm mới.
    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        RaceResultManager.OnRaceFinish += OnRaceFinished;
    }

    void OnDisable()
    {
        RaceResultManager.OnRaceFinish -= OnRaceFinished;
    }

    private async void OnRaceFinished()
    {
        await FlushToProfileAsync();
    }

    /// <summary>
    /// Cộng phần coin chưa lưu của ván vào hồ sơ (Firestore). tol giữ nguyên để HUD không đổi.
    /// Không có hồ sơ (chưa đăng nhập) thì bỏ qua, coin chưa lưu vẫn còn để lưu lần sau.
    /// </summary>
    public async Task FlushToProfileAsync()
    {
        int earned = tol - savedCoins;
        if (earned <= 0) return;
        if (UserProfileService.Instance == null || UserProfileService.Instance.Current == null) return;

        savedCoins += earned; // chặn gọi trùng khi đang chờ Firestore
        if (!await UserProfileService.Instance.AddCoinsAsync(earned))
            savedCoins -= earned;
    }

    /// <summary>Xóa bộ đếm coin của phiên hiện tại (gọi khi đăng xuất để user sau không bị dính coin).</summary>
    public void ResetSession()
    {
        tol = 0;
        savedCoins = 0;
        multiplier = 1;
        OnCoinChanged?.Invoke(tol);
    }
    public void UpdateTotalCoin(int coinValue)
    {
        tol += coinValue * multiplier;
        OnCoinChanged?.Invoke(tol); 
        Debug.Log("Caculator: Total coins: " + GetTotalCoin());
    }
    public int GetTotalCoin()
    {
        return tol;
    }
}
