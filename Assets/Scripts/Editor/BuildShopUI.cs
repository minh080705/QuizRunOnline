using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildKit;

/// <summary>
/// Menu Tools/UI/Build Shop UI: dựng giao diện ShopScene dạng dọc (thiết kế 1170x2532):
/// thanh trên (quay lại, số xu), hai tab, bộ chọn nhân vật cho tab Màu da, lưới thẻ 2 cột cuộn dọc,
/// dòng trạng thái và hộp xác nhận mua.
///
/// Cách làm giống BuildLobbyUI: tạo MỚI các object đuôi " (Generated)" dưới Canvas, gán lại các field của ShopUI
/// có sẵn trong scene (component ShopUI giữ nguyên), rồi TẮT (không xóa) các UI cũ mà ShopUI từng tham chiếu.
/// Prefab thẻ mới lưu ở Assets/Prefabs/UI/Generated/ShopCard_Generated.prefab, không đè prefab cũ.
/// Hỗ trợ Undo cho phần trong scene.
/// </summary>
public static class BuildShopUI
{
    private const string Suffix = " (Generated)";
    private const string PrefabFolder = "Assets/Prefabs/UI/Generated";

    private static readonly Color TabActiveText = Hex("3A2A00");

    // Các field ShopUI trỏ tới UI cũ, dùng để tìm và tắt chúng.
    private static readonly string[] OldFields =
    {
        "coinText", "statusText", "backButton", "characterTabButton", "skinTabButton",
        "skinCharacterSelector", "confirmPanel"
    };

    [MenuItem("Tools/UI/Build Shop UI")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Build Shop UI", "Hãy thoát Play mode trước.", "OK");
            return;
        }

        ShopUI ui = Object.FindObjectOfType<ShopUI>(true);
        if (ui == null)
        {
            EditorUtility.DisplayDialog("Build Shop UI",
                "Không tìm thấy ShopUI trong scene. Hãy mở ShopScene trước.", "OK");
            return;
        }

        Canvas canvas = ResolveCanvas(ui);
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Build Shop UI", "Không tìm thấy Canvas trong scene.", "OK");
            return;
        }

        if (!ConfirmBuild(canvas, "Build Shop UI",
                $"Dựng giao diện Shop mới dưới Canvas \"{canvas.name}\"?\n" +
                "Các UI cũ mà ShopUI đang tham chiếu sẽ bị tắt (không xóa).\n" +
                "Prefab thẻ mới được tạo ở " + PrefabFolder + ".",
                out bool changeScaler))
            return;

        // Đã dựng trước đó: xóa bản cũ rồi dựng lại.
        var generated = new List<GameObject>();
        foreach (Transform child in canvas.transform)
            if (child.name.EndsWith(Suffix)) generated.Add(child.gameObject);

        if (generated.Count > 0)
        {
            if (!EditorUtility.DisplayDialog("Build Shop UI",
                    $"Đã có {generated.Count} object \"{Suffix.Trim()}\" từ lần dựng trước. Xóa và dựng lại? " +
                    "(Mọi chỉnh sửa tay trên chúng sẽ mất)", "Dựng lại", "Hủy"))
                return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build Shop UI");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (GameObject g in generated) Undo.DestroyObjectImmediate(g);
        if (changeScaler) ApplyScaler(canvas);

        Init(ResolveFont(canvas));

        // Gom UI cũ trước khi gán lại field.
        var oldObjects = new List<GameObject>();
        var uiSo = new SerializedObject(ui);
        foreach (string field in OldFields)
        {
            Object value = uiSo.FindProperty(field)?.objectReferenceValue;
            GameObject go = value is Component c ? c.gameObject : value as GameObject;
            if (go != null) oldObjects.Add(go);
        }
        Transform oldGrid = uiSo.FindProperty("grid")?.objectReferenceValue as Transform;
        if (oldGrid != null)
        {
            // Lưới cũ thường nằm trong ScrollView: tắt cả ScrollView cho gọn.
            ScrollRect oldScroll = oldGrid.GetComponentInParent<ScrollRect>(true);
            oldObjects.Add(oldScroll != null ? oldScroll.gameObject : oldGrid.gameObject);
        }

        // ---------------- Prefab ----------------
        EnsureFolder(PrefabFolder);
        TrackUndo = false;
        ShopItemCard cardPrefab = BuildCardPrefab();
        TrackUndo = true;

        Transform ct = canvas.transform;

        // ---------------- Nền ----------------
        RectTransform bg = NewUI("ShopBackground" + Suffix, ct);
        Fill(bg, 0, 0, 0, 0);
        bg.gameObject.AddComponent<Image>().color = Bg;

        // ---------------- Panel chính ----------------
        RectTransform root = NewUI("ShopPanel" + Suffix, ct);
        Fill(root, 0, 0, 0, 0);
        RectTransform safe = MakeSafeArea(root);

        // Thanh trên: quay lại, tiêu đề, số xu
        RectTransform topBar = NewUI("TopBar", safe);
        TopRow(topBar, 90, 90, 40, 120);

        Button backButton = MakeButton(topBar, "BackButton", "<", 64, ButtonSoft, Color.white, 36, true);
        LeftMid((RectTransform)backButton.transform, 0, 120, 120);

        TextMeshProUGUI title = Label(topBar, "Title", "Cửa hàng", 64, Color.white, TextAlignmentOptions.MidlineLeft, true);
        Anchor(title.rectTransform, Vector2.zero, Vector2.one, new Vector2(156, 0), new Vector2(-460, 0));

        RectTransform coinChip = NewUI("CoinChip", topBar);
        Round(coinChip.gameObject.AddComponent<Image>(), Card, 50);
        RightMid(coinChip, 0, 400, 100);

        TextMeshProUGUI coinUnit = Label(coinChip, "CoinUnit", "Xu", 40, TextSecondary, TextAlignmentOptions.MidlineLeft, false);
        Anchor(coinUnit.rectTransform, Vector2.zero, Vector2.one, new Vector2(40, 0), new Vector2(-250, 0));

        TextMeshProUGUI coinText = Label(coinChip, "CoinText", "0", 56, Yellow, TextAlignmentOptions.MidlineRight, true);
        Anchor(coinText.rectTransform, Vector2.zero, Vector2.one, new Vector2(120, 0), new Vector2(-40, 0));

        // Vùng nội dung xếp dọc: tab, bộ chọn nhân vật (ẩn ở tab Nhân vật thì tự co lại), lưới thẻ
        RectTransform content = NewUI("Content", safe);
        Fill(content, 90, 90, 220, 150);
        VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 30;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        RectTransform tabs = NewUI("Tabs", content);
        tabs.gameObject.AddComponent<LayoutElement>().preferredHeight = 130;
        HorizontalLayoutGroup tabsLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabsLayout.spacing = 30;
        tabsLayout.childControlWidth = true;
        tabsLayout.childControlHeight = true;
        tabsLayout.childForceExpandWidth = true;
        tabsLayout.childForceExpandHeight = true;

        Button characterTab = MakeButton(tabs, "CharacterTabButton", "Nhân vật", 54, Yellow, TabActiveText, 40, true);
        Button skinTab = MakeButton(tabs, "SkinTabButton", "Màu da", 54, Card, TextSecondary, 40, true);

        RectTransform selector = NewUI("SkinCharacterSelector", content);
        Round(selector.gameObject.AddComponent<Image>(), Card, 40);
        selector.gameObject.AddComponent<LayoutElement>().preferredHeight = 130;

        Button prevButton = MakeButton(selector, "PrevCharacterButton", "<", 52, ButtonSoft, Color.white, 28, true);
        LeftMid((RectTransform)prevButton.transform, 24, 90, 90);
        Button nextButton = MakeButton(selector, "NextCharacterButton", ">", 52, ButtonSoft, Color.white, 28, true);
        RightMid((RectTransform)nextButton.transform, 24, 90, 90);
        TextMeshProUGUI skinCharacterName = Label(selector, "SkinCharacterNameText", "", 52, Color.white, TextAlignmentOptions.Center, true);
        Fill(skinCharacterName.rectTransform, 140, 140, 0, 0);

        // Lưới thẻ 2 cột: ô 480x620, khe 30 -> vừa 990 px.
        RectTransform scroll = MakeScroll(content, "CardScroll", true, 30, 0, out RectTransform grid,
            gridColumns: 2, gridCell: new Vector2(480, 620));
        LayoutElement scrollLayout = scroll.gameObject.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1;
        scrollLayout.minHeight = 300;

        TextMeshProUGUI statusText = Label(safe, "StatusText", "", 46, Green, TextAlignmentOptions.Center, false);
        BottomRow(statusText.rectTransform, 90, 90, 60, 64);

        // ---------------- Hộp xác nhận mua ----------------
        RectTransform confirmRoot = NewUI("ShopConfirm" + Suffix, ct);
        Fill(confirmRoot, 0, 0, 0, 0);
        confirmRoot.gameObject.AddComponent<Image>().color = new Color(Dim.r, Dim.g, Dim.b, 0.8f); // chặn bấm xuyên xuống

        RectTransform confirmCard = NewUI("Card", confirmRoot);
        Round(confirmCard.gameObject.AddComponent<Image>(), Card, 60);
        Anchor(confirmCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-495, -280), new Vector2(495, 280));

        TextMeshProUGUI confirmText = Label(confirmCard, "ConfirmText", "", 58, Color.white, TextAlignmentOptions.Center, false, true);
        TopRow(confirmText.rectTransform, 70, 70, 80, 230);

        Button noButton = MakeButton(confirmCard, "ConfirmNoButton", "Hủy", 52, ButtonSoft, TextSoft, 44, false);
        Anchor((RectTransform)noButton.transform, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(70, 70), new Vector2(-15, 230));

        Button yesButton = MakeButton(confirmCard, "ConfirmYesButton", "Mua", 52, Green, GreenDark, 44, true);
        Anchor((RectTransform)yesButton.transform, new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(15, 70), new Vector2(-70, 230));

        // ---------------- Gán field cho ShopUI ----------------
        Wire(ui,
            "coinText", coinText,
            "statusText", statusText,
            "backButton", backButton,
            "grid", grid,
            "cardPrefab", cardPrefab,
            "characterTabButton", characterTab,
            "skinTabButton", skinTab,
            "skinCharacterSelector", selector.gameObject,
            "prevCharacterButton", prevButton,
            "nextCharacterButton", nextButton,
            "skinCharacterNameText", skinCharacterName,
            "confirmPanel", confirmRoot.gameObject,
            "confirmText", confirmText,
            "confirmYesButton", yesButton,
            "confirmNoButton", noButton,
            "highlightActiveTab", true);

        // ---------------- Trạng thái ban đầu ----------------
        selector.gameObject.SetActive(false);    // chỉ hiện ở tab Màu da
        confirmRoot.gameObject.SetActive(false); // chỉ hiện khi bấm Mua

        // ---------------- Tắt UI cũ (không xóa) ----------------
        var disabled = new List<string>();
        foreach (GameObject go in oldObjects) DisableOld(go, canvas, ui, disabled);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = root.gameObject;
        AssetDatabase.SaveAssets();

        Debug.Log("[BuildShopUI] Đã dựng giao diện Shop và gán field cho ShopUI. " +
                  (disabled.Count > 0 ? "Đã tắt UI cũ: " + string.Join(", ", disabled) + " (xóa tay khi hài lòng). " : "") +
                  "Xem tab Màu da: bật \"SkinCharacterSelector\"; xem hộp xác nhận: bật \"ShopConfirm" + Suffix + "\". " +
                  "Đặt Game view 1170x2532 và nhớ Ctrl+S để lưu scene.");
    }

    // ================= Prefab =================

    // Thẻ shop 480x620: icon, tên, giá, nút. Dùng script ShopItemCard có sẵn.
    private static ShopItemCard BuildCardPrefab()
    {
        RectTransform root = NewUI("ShopCard" + Suffix, null);
        root.sizeDelta = new Vector2(480, 620);
        Round(root.gameObject.AddComponent<Image>(), Card, 48);

        RectTransform iconBg = NewUI("IconBg", root);
        Round(iconBg.gameObject.AddComponent<Image>(), Soft, 32);
        TopCenter(iconBg, 20, 440, 330);

        // Ảnh icon là Image riêng, trắng, không tô màu; ShopItemCard.Setup tự bật/tắt khi có/không có sprite.
        RectTransform iconRt = NewUI("Icon", iconBg);
        Image icon = iconRt.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;
        Fill(iconRt, 10, 10, 10, 10);

        TextMeshProUGUI nameText = Label(root, "Name", "Tên", 50, Color.white, TextAlignmentOptions.Center, true);
        TopRow(nameText.rectTransform, 10, 10, 366, 64);

        TextMeshProUGUI priceText = Label(root, "Price", "", 42, Yellow, TextAlignmentOptions.Center, false);
        TopRow(priceText.rectTransform, 10, 10, 436, 56);

        Button button = MakeButton(root, "Button", "Mua", 46, Green, GreenDark, 32, true);
        BottomRow((RectTransform)button.transform, 20, 20, 20, 100);

        ShopItemCard card = root.gameObject.AddComponent<ShopItemCard>();
        Wire(card,
            "icon", icon,
            "nameText", nameText,
            "priceText", priceText,
            "button", button,
            "buttonText", button.GetComponentInChildren<TMP_Text>());

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabFolder + "/ShopCard_Generated.prefab");
        Object.DestroyImmediate(root.gameObject);
        return prefab.GetComponent<ShopItemCard>();
    }

    // ================= Tiện ích =================

    private static Canvas ResolveCanvas(ShopUI ui)
    {
        Canvas canvas = ui.GetComponentInParent<Canvas>(true);
        if (canvas != null) return canvas.rootCanvas;

        var so = new SerializedObject(ui);
        foreach (string field in new[] { "backButton", "grid", "coinText", "characterTabButton" })
        {
            Object value = so.FindProperty(field)?.objectReferenceValue;
            Component c = value as Component;
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

    private static void DisableOld(GameObject go, Canvas canvas, ShopUI ui, List<string> log)
    {
        if (go == null || !go.activeSelf) return;
        if (go.name.EndsWith(Suffix)) return;
        // Không tắt Canvas, object chứa các UI vừa dựng, hay object chứa chính ShopUI (tắt sẽ mất Awake/Start).
        if (go == canvas.gameObject || canvas.transform.IsChildOf(go.transform)) return;
        if (ui.transform.IsChildOf(go.transform)) return;

        Undo.RecordObject(go, "Disable old Shop UI");
        go.SetActive(false);
        log.Add(go.name.Trim());
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
