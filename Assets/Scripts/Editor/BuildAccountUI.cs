using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildKit;

/// <summary>
/// Menu Tools/UI/Build Account Profile UI: dựng panel hồ sơ cho AccountScene (màn dọc, thiết kế 1170x2532)
/// rồi gán vào AccountUI. Chỉ tạo object mới tên "ProfilePanel (Generated)"; panel hồ sơ cũ chỉ bị TẮT (không xóa),
/// panel đăng nhập không bị đụng. Hỗ trợ Undo (Ctrl+Z).
/// </summary>
public static class BuildAccountUI
{
    private const string PanelName = "ProfilePanel (Generated)";

    [MenuItem("Tools/UI/Build Account Profile UI")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Build Account UI", "Hãy thoát Play mode trước.", "OK");
            return;
        }

        AccountUI ui = Object.FindObjectOfType<AccountUI>();
        if (ui == null)
        {
            EditorUtility.DisplayDialog("Build Account UI",
                "Không tìm thấy AccountUI trong scene. Hãy mở AccountScene trước.", "OK");
            return;
        }

        SerializedObject so = new SerializedObject(ui);
        Canvas canvas = ResolveCanvas(so);
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Build Account UI", "Không tìm thấy Canvas trong scene.", "OK");
            return;
        }

        if (!ConfirmBuild(canvas, "Build Account UI",
                $"Dựng panel hồ sơ mới dưới Canvas \"{canvas.name}\"?\nPanel hồ sơ cũ sẽ bị tắt (không xóa).",
                out bool changeScaler))
            return;

        // Đã dựng trước đó: thay bằng bản mới.
        Transform existing = canvas.transform.Find(PanelName);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Build Account UI",
                    $"Đã có \"{PanelName}\". Xóa và dựng lại? (Mọi chỉnh sửa tay trên panel này sẽ mất)", "Dựng lại", "Hủy"))
                return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build Account Profile UI");
        int undoGroup = Undo.GetCurrentGroup();

        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);
        if (changeScaler) ApplyScaler(canvas);

        Init(ResolveFont(so));

        GameObject oldPanel = so.FindProperty("profilePanel")?.objectReferenceValue as GameObject;
        TMP_Text oldStatus = so.FindProperty("statusText")?.objectReferenceValue as TMP_Text;
        bool reuseStatus = oldStatus != null && (oldPanel == null || !oldStatus.transform.IsChildOf(oldPanel.transform));

        // ---------------- Cây UI ----------------
        RectTransform root = NewUI(PanelName, canvas.transform);
        Fill(root, 0, 0, 0, 0);
        if (oldPanel != null) root.SetSiblingIndex(oldPanel.transform.GetSiblingIndex() + 1);
        Image bg = root.gameObject.AddComponent<Image>();
        bg.color = Bg;

        RectTransform safe = MakeSafeArea(root);

        // Thanh trên: nút quay về menu + tiêu đề
        RectTransform topBar = NewUI("TopBar", safe);
        TopRow(topBar, 90, 90, 40, 120);
        Button menuButton = MakeButton(topBar, "MenuButton", "<", 64, ButtonSoft, Color.white, 36, true);
        LeftMid((RectTransform)menuButton.transform, 0, 120, 120);
        TextMeshProUGUI title = Label(topBar, "Title", "Tài khoản", 64, Color.white, TextAlignmentOptions.MidlineLeft, true);
        Anchor(title.rectTransform, Vector2.zero, Vector2.one, new Vector2(156, 0), Vector2.zero);

        // Thẻ số xu: nhãn "Số xu" bám trái, số xu bám phải
        RectTransform coinCard = NewUI("CoinCard", safe);
        Round(coinCard.gameObject.AddComponent<Image>(), Card, 48);
        TopRow(coinCard, 90, 90, 240, 180);

        TextMeshProUGUI coinLabel = Label(coinCard, "CoinLabel", "Số xu", 44, TextSecondary, TextAlignmentOptions.MidlineLeft, false);
        TopRow(coinLabel.rectTransform, 50, 50, 50, 80);

        TextMeshProUGUI coinText = Label(coinCard, "CoinText", "Coin: 0", 60, Yellow, TextAlignmentOptions.MidlineRight, true);
        TopRow(coinText.rectTransform, 50, 50, 50, 80);

        // Thẻ tên nhân vật
        RectTransform nameCard = NewUI("NameCard", safe);
        Round(nameCard.gameObject.AddComponent<Image>(), Card, 48);
        TopRow(nameCard, 90, 90, 460, 320);

        TextMeshProUGUI nameLabel = Label(nameCard, "NameLabel", "Tên nhân vật", 40, TextSecondary, TextAlignmentOptions.MidlineLeft, false);
        TopRow(nameLabel.rectTransform, 50, 50, 50, 50);

        TMP_InputField nameInput = MakeInput(nameCard, "NameInput", "Nhập tên nhân vật");
        Anchor((RectTransform)nameInput.transform, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(50, -280), new Vector2(-224, -130)); // cao 150, chừa bên phải cho nút Lưu

        Button saveButton = MakeButton(nameCard, "SaveNameButton", "Lưu", 48, Green, GreenDark, 36, true);
        TopRight((RectTransform)saveButton.transform, 50, 130, 150, 150);

        // Cụm dưới: trạng thái, Vào chơi, Đăng xuất (vùng ngón cái)
        Button signOutButton = MakeButton(safe, "SignOutButton", "Đăng xuất", 54, ButtonSoft, TextSoft, 48, false);
        BottomRow((RectTransform)signOutButton.transform, 90, 90, 60, 150);

        Button playButton = MakeButton(safe, "PlayButton", "Vào chơi", 72, Green, GreenDark, 56, true);
        BottomRow((RectTransform)playButton.transform, 90, 90, 246, 200);

        TextMeshProUGUI statusText = null;
        if (!reuseStatus)
        {
            statusText = Label(safe, "StatusText", "", 46, Green, TextAlignmentOptions.Center, false);
            BottomRow(statusText.rectTransform, 90, 90, 482, 64);
        }

        // ---------------- Gán vào AccountUI ----------------
        Wire(ui,
            "profilePanel", root.gameObject,
            "nameInput", nameInput,
            "saveNameButton", saveButton,
            "coinText", coinText,
            "signOutButton", signOutButton,
            "playButton", playButton,
            "menuButton", menuButton);
        if (statusText != null) Wire(ui, "statusText", statusText);

        // Tắt panel cũ (không xóa) để tránh đè lên panel mới trong Editor và lúc chạy.
        if (oldPanel != null && oldPanel != root.gameObject)
        {
            Undo.RecordObject(oldPanel, "Disable old profile panel");
            oldPanel.SetActive(false);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        Selection.activeGameObject = root.gameObject;

        Debug.Log("[BuildAccountUI] Đã dựng \"" + PanelName + "\" và gán vào AccountUI. " +
                  (oldPanel != null ? $"Panel cũ \"{oldPanel.name}\" đã bị tắt (xóa tay khi hài lòng). " : "") +
                  "Để xem: chọn Game view tỉ lệ 1170x2532, tắt loginPanel tạm thời. Nhớ Ctrl+S để lưu scene.");
    }

    private static Canvas ResolveCanvas(SerializedObject so)
    {
        GameObject login = so.FindProperty("loginPanel")?.objectReferenceValue as GameObject;
        if (login != null)
        {
            Canvas c = login.GetComponentInParent<Canvas>();
            if (c != null) return c.rootCanvas;
        }
        return Object.FindObjectOfType<Canvas>();
    }

    // Lấy font của chữ đang có trong AccountUI (đã hiển thị được tiếng Việt), không có thì dùng font mặc định của TMP.
    private static TMP_FontAsset ResolveFont(SerializedObject so)
    {
        foreach (string field in new[] { "coinText", "statusText" })
        {
            TMP_Text t = so.FindProperty(field)?.objectReferenceValue as TMP_Text;
            if (t != null && t.font != null) return t.font;
        }
        return TMP_Settings.defaultFontAsset;
    }
}
