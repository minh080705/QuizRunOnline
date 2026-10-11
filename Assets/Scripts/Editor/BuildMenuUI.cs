using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildKit;

/// <summary>
/// Menu Tools/UI/Build Menu UI: dựng giao diện MenuScene dạng dọc (thiết kế 1170x2532):
/// chip tên và xu ở trên, bốn nút xếp dọc (Chơi online, Cửa hàng, Tài khoản, Thoát game) ở giữa màn hình.
///
/// Cách làm giống BuildLobbyUI/BuildShopUI: tạo MỚI các object đuôi " (Generated)" dưới Canvas, gán lại các field của
/// MenuUI có sẵn trong scene (component MenuUI giữ nguyên), rồi TẮT (không xóa) các nút cũ mà MenuUI từng tham chiếu.
/// Hỗ trợ Undo cho phần trong scene.
/// </summary>
public static class BuildMenuUI
{
    private const string Suffix = " (Generated)";

    private static readonly string[] OldFields = { "playOnlineButton", "accountButton", "shopButton", "quitButton" };

    [MenuItem("Tools/UI/Build Menu UI")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Build Menu UI", "Hãy thoát Play mode trước.", "OK");
            return;
        }

        MenuUI ui = Object.FindObjectOfType<MenuUI>(true);
        if (ui == null)
        {
            EditorUtility.DisplayDialog("Build Menu UI",
                "Không tìm thấy MenuUI trong scene. Hãy mở MenuScene trước.", "OK");
            return;
        }

        Canvas canvas = ResolveCanvas(ui);
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Build Menu UI", "Không tìm thấy Canvas trong scene.", "OK");
            return;
        }

        if (!ConfirmBuild(canvas, "Build Menu UI",
                $"Dựng giao diện Menu mới dưới Canvas \"{canvas.name}\"?\n" +
                "Các nút cũ mà MenuUI đang tham chiếu sẽ bị tắt (không xóa).",
                out bool changeScaler))
            return;

        // Đã dựng trước đó: xóa bản cũ rồi dựng lại.
        var generated = new List<GameObject>();
        foreach (Transform child in canvas.transform)
            if (child.name.EndsWith(Suffix)) generated.Add(child.gameObject);

        if (generated.Count > 0)
        {
            if (!EditorUtility.DisplayDialog("Build Menu UI",
                    $"Đã có {generated.Count} object \"{Suffix.Trim()}\" từ lần dựng trước. Xóa và dựng lại? " +
                    "(Mọi chỉnh sửa tay trên chúng sẽ mất)", "Dựng lại", "Hủy"))
                return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build Menu UI");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (GameObject g in generated) Undo.DestroyObjectImmediate(g);
        if (changeScaler) ApplyScaler(canvas);

        Init(ResolveFont(canvas));

        // Gom nút cũ trước khi gán lại field.
        var oldObjects = new List<GameObject>();
        var uiSo = new SerializedObject(ui);
        foreach (string field in OldFields)
        {
            Component old = uiSo.FindProperty(field)?.objectReferenceValue as Component;
            if (old != null) oldObjects.Add(old.gameObject);
        }

        Transform ct = canvas.transform;

        // ---------------- Nền ----------------
        RectTransform bg = NewUI("MenuBackground" + Suffix, ct);
        Fill(bg, 0, 0, 0, 0);
        bg.gameObject.AddComponent<Image>().color = Bg;

        // ---------------- Panel chính ----------------
        RectTransform root = NewUI("MenuPanel" + Suffix, ct);
        Fill(root, 0, 0, 0, 0);
        RectTransform safe = MakeSafeArea(root);

        // Hàng trên: tên người chơi (trái), số xu (phải)
        RectTransform infoRow = NewUI("InfoRow", safe);
        TopRow(infoRow, 90, 90, 40, 100);

        RectTransform nameChip = NewUI("PlayerNameChip", infoRow);
        Round(nameChip.gameObject.AddComponent<Image>(), Card, 50);
        LeftMid(nameChip, 0, 520, 100);

        RectTransform nameDot = NewUI("AvatarDot", nameChip);
        Image dotImage = nameDot.gameObject.AddComponent<Image>();
        dotImage.sprite = CircleSprite;
        dotImage.color = Soft;
        LeftMid(nameDot, 28, 56, 56);

        TextMeshProUGUI playerNameText = Label(nameChip, "PlayerNameText", "", 50, Color.white, TextAlignmentOptions.MidlineLeft, true);
        Anchor(playerNameText.rectTransform, Vector2.zero, Vector2.one, new Vector2(110, 0), new Vector2(-30, 0));

        RectTransform coinChip = NewUI("CoinChip", infoRow);
        Round(coinChip.gameObject.AddComponent<Image>(), Card, 50);
        RightMid(coinChip, 0, 360, 100);

        TextMeshProUGUI coinUnit = Label(coinChip, "CoinUnit", "Xu", 40, TextSecondary, TextAlignmentOptions.MidlineLeft, false);
        Anchor(coinUnit.rectTransform, Vector2.zero, Vector2.one, new Vector2(40, 0), new Vector2(-220, 0));

        TextMeshProUGUI coinText = Label(coinChip, "CoinText", "0", 54, Yellow, TextAlignmentOptions.MidlineRight, true);
        Anchor(coinText.rectTransform, Vector2.zero, Vector2.one, new Vector2(110, 0), new Vector2(-40, 0));

        // Bốn nút xếp dọc, rộng bằng nhau (990 px), căn giữa màn hình rồi dịch xuống 100 px cho dễ bấm bằng ngón cái.
        // Tọa độ tính từ tâm vùng an toàn (y dương = lên): Chơi online cao 220, ba nút còn lại cao 180, khe 40.
        Button playButton = MakeButton(safe, "PlayOnlineButton", "Chơi online", 72, Green, GreenDark, 56, true);
        CenterBox((RectTransform)playButton.transform, -495, 120, 495, 340);

        Button shopButton = MakeButton(safe, "ShopButton", "Cửa hàng", 60, ButtonSoft, Color.white, 52, true);
        CenterBox((RectTransform)shopButton.transform, -495, -100, 495, 80);

        Button accountButton = MakeButton(safe, "AccountButton", "Tài khoản", 60, ButtonSoft, Color.white, 52, true);
        CenterBox((RectTransform)accountButton.transform, -495, -320, 495, -140);

        Button quitButton = MakeButton(safe, "QuitButton", "Thoát game", 54, Card, TextSecondary, 52, false);
        CenterBox((RectTransform)quitButton.transform, -495, -540, 495, -360);

        // ---------------- Gán field cho MenuUI ----------------
        Wire(ui,
            "playOnlineButton", playButton,
            "accountButton", accountButton,
            "shopButton", shopButton,
            "quitButton", quitButton,
            "playerNameText", playerNameText,
            "coinText", coinText);

        // ---------------- Tắt nút cũ (không xóa) ----------------
        var disabled = new List<string>();
        foreach (GameObject go in oldObjects) DisableOld(go, canvas, ui, disabled);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = root.gameObject;

        Debug.Log("[BuildMenuUI] Đã dựng giao diện Menu và gán field cho MenuUI. " +
                  (disabled.Count > 0 ? "Đã tắt nút cũ: " + string.Join(", ", disabled) + " (xóa tay khi hài lòng). " : "") +
                  "Tên và xu chỉ hiện đúng khi chạy (cần đã đăng nhập qua AccountScene). " +
                  "Đặt Game view 1170x2532 và nhớ Ctrl+S để lưu scene.");
    }

    // ================= Tiện ích =================

    // Hộp cố định neo vào TÂM của cha; tọa độ tính từ tâm (y dương = lên trên).
    private static void CenterBox(RectTransform rt, float left, float bottom, float right, float top)
    {
        Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(left, bottom), new Vector2(right, top));
    }

    private static Canvas ResolveCanvas(MenuUI ui)
    {
        Canvas canvas = ui.GetComponentInParent<Canvas>(true);
        if (canvas != null) return canvas.rootCanvas;

        var so = new SerializedObject(ui);
        foreach (string field in OldFields)
        {
            Component c = so.FindProperty(field)?.objectReferenceValue as Component;
            if (c == null) continue;

            canvas = c.GetComponentInParent<Canvas>(true);
            if (canvas != null) return canvas.rootCanvas;
        }
        return Object.FindObjectOfType<Canvas>();
    }

    // Lấy font của một chữ TMP đã có trong Canvas (đã hiển thị được tiếng Việt); không có thì font mặc định của TMP.
    private static TMP_FontAsset ResolveFont(Canvas canvas)
    {
        foreach (TMP_Text t in canvas.GetComponentsInChildren<TMP_Text>(true))
            if (t.font != null && !t.gameObject.name.EndsWith(Suffix)) return t.font;
        return TMP_Settings.defaultFontAsset;
    }

    private static void DisableOld(GameObject go, Canvas canvas, MenuUI ui, List<string> log)
    {
        if (go == null || !go.activeSelf) return;
        if (go.name.EndsWith(Suffix)) return;
        // Không tắt Canvas, object chứa các UI vừa dựng, hay object chứa chính MenuUI (tắt sẽ mất Start).
        if (go == canvas.gameObject || canvas.transform.IsChildOf(go.transform)) return;
        if (ui.transform.IsChildOf(go.transform)) return;

        Undo.RecordObject(go, "Disable old Menu UI");
        go.SetActive(false);
        log.Add(go.name.Trim());
    }
}
