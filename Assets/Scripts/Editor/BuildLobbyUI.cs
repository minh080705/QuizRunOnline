using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static UIBuildKit;

/// <summary>
/// Menu Tools/UI/Build Lobby UI: dựng giao diện LobbyScene dạng dọc (thiết kế 1170x2532):
/// danh sách phòng + tạo phòng, phòng chờ, popup chọn nhân vật (bottom sheet), hộp xác nhận.
///
/// Cách làm: tạo MỚI các object đuôi " (Generated)" dưới Canvas, kèm bản mới của RoomListUI, RoomPanelUI,
/// CharacterPickerPopup, UI_ConfirmDialog; gán lại LobbyNavigationUI; rồi TẮT (không xóa) các object cũ.
/// Ba prefab mới (dòng phòng, dòng người chơi, thẻ chọn) lưu ở Assets/Prefabs/UI/Generated, không đè prefab cũ.
/// Hỗ trợ Undo cho phần trong scene.
/// </summary>
public static class BuildLobbyUI
{
    private const string Suffix = " (Generated)";
    private const string PrefabFolder = "Assets/Prefabs/UI/Generated";

    private static readonly Color CardOnSheet = Hex("262B52");

    [MenuItem("Tools/UI/Build Lobby UI")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Build Lobby UI", "Hãy thoát Play mode trước.", "OK");
            return;
        }

        RoomListUI oldList = FindOriginal<RoomListUI>();
        RoomPanelUI oldRoom = FindOriginal<RoomPanelUI>();
        CharacterPickerPopup oldPicker = FindOriginal<CharacterPickerPopup>();
        UI_ConfirmDialog oldDialog = FindOriginal<UI_ConfirmDialog>();
        LobbyNavigationUI nav = Object.FindObjectOfType<LobbyNavigationUI>(true);

        if (oldList == null && oldRoom == null && nav == null)
        {
            EditorUtility.DisplayDialog("Build Lobby UI",
                "Không thấy RoomListUI, RoomPanelUI hay LobbyNavigationUI trong scene. Hãy mở LobbyScene trước.", "OK");
            return;
        }

        Canvas canvas = ResolveCanvas(oldList, oldRoom, nav);
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Build Lobby UI", "Không tìm thấy Canvas trong scene.", "OK");
            return;
        }

        if (!ConfirmBuild(canvas, "Build Lobby UI",
                $"Dựng giao diện Lobby mới dưới Canvas \"{canvas.name}\"?\n" +
                "Các panel cũ (danh sách phòng, phòng chờ, popup chọn nhân vật, hộp xác nhận) sẽ bị tắt, không xóa.\n" +
                "Ba prefab mới được tạo ở " + PrefabFolder + ".",
                out bool changeScaler))
            return;

        // Đã dựng trước đó: xóa bản cũ rồi dựng lại.
        var generated = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in canvas.transform)
            if (child.name.EndsWith(Suffix)) generated.Add(child.gameObject);

        if (generated.Count > 0)
        {
            if (!EditorUtility.DisplayDialog("Build Lobby UI",
                    $"Đã có {generated.Count} object \"{Suffix.Trim()}\" từ lần dựng trước. Xóa và dựng lại? " +
                    "(Mọi chỉnh sửa tay trên chúng sẽ mất)", "Dựng lại", "Hủy"))
                return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build Lobby UI");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (GameObject g in generated) Undo.DestroyObjectImmediate(g);
        if (changeScaler) ApplyScaler(canvas);

        Init(ResolveFont(canvas));

        // Giữ lại tham chiếu tới các nút điều hướng cũ trước khi gán lại.
        GameObject oldAccountButton = null;
        GameObject oldLeaveButton = null;
        if (nav != null)
        {
            var navSo = new SerializedObject(nav);
            oldAccountButton = (navSo.FindProperty("accountButton")?.objectReferenceValue as Button)?.gameObject;
            oldLeaveButton = (navSo.FindProperty("leaveRoomButton")?.objectReferenceValue as Button)?.gameObject;
        }

        // ---------------- Prefab ----------------
        EnsureFolder(PrefabFolder);
        TrackUndo = false;
        RoomListItem itemPrefab = BuildRoomItemPrefab();
        PlayerRowUI rowPrefab = BuildPlayerRowPrefab();
        ShopItemCard cardPrefab = BuildPickerCardPrefab();
        TrackUndo = true;

        Transform ct = canvas.transform;

        // ---------------- Nền ----------------
        RectTransform bg = NewUI("LobbyBackground" + Suffix, ct);
        Fill(bg, 0, 0, 0, 0);
        bg.gameObject.AddComponent<Image>().color = Bg;

        // ---------------- Danh sách phòng ----------------
        RectTransform listRoot = NewUI("RoomListPanel" + Suffix, ct);
        Fill(listRoot, 0, 0, 0, 0);
        RoomListUI listUi = listRoot.gameObject.AddComponent<RoomListUI>();
        RectTransform listSafe = MakeSafeArea(listRoot);

        TextMeshProUGUI listTitle = Label(listSafe, "Title", "Sảnh chờ", 64, Color.white, TextAlignmentOptions.MidlineLeft, true);
        TopRow(listTitle.rectTransform, 156, 90, 40, 120);

        TextMeshProUGUI listLabel = Label(listSafe, "OpenRoomsLabel", "Phòng đang mở", 40, TextSecondary, TextAlignmentOptions.MidlineLeft, false);
        TopRow(listLabel.rectTransform, 90, 90, 220, 50);

        RectTransform listScroll = MakeScroll(listSafe, "RoomScroll", true, 30, 0, out RectTransform roomContent);
        TopRow(listScroll, 90, 90, 290, 1380);

        TextMeshProUGUI emptyText = Label(listSafe, "EmptyText", "Chưa có phòng nào.\nHãy tạo phòng mới.", 48,
            TextSecondary, TextAlignmentOptions.Center, false, true);
        TopRow(emptyText.rectTransform, 150, 150, 790, 240);

        RectTransform createCard = NewUI("CreateRoomCard", listSafe);
        Round(createCard.gameObject.AddComponent<Image>(), Card, 48);
        BottomRow(createCard, 90, 90, 60, 530);

        TextMeshProUGUI createLabel = Label(createCard, "CreateLabel", "Tạo phòng mới", 40, TextSecondary, TextAlignmentOptions.MidlineLeft, false);
        TopRow(createLabel.rectTransform, 50, 50, 50, 50);

        TMP_InputField roomNameInput = MakeInput(createCard, "RoomNameInput", "Tên phòng (tùy chọn)", 46);
        Anchor((RectTransform)roomNameInput.transform, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(50, -270), new Vector2(-304, -130)); // cao 140, chừa bên phải cho ô số người

        TMP_InputField maxPlayersInput = MakeInput(createCard, "MaxPlayersInput", "Tối đa: 4", 46);
        maxPlayersInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        maxPlayersInput.characterLimit = 2;
        maxPlayersInput.textComponent.alignment = TextAlignmentOptions.Center;
        ((TMP_Text)maxPlayersInput.placeholder).alignment = TextAlignmentOptions.Center;
        TopRight((RectTransform)maxPlayersInput.transform, 50, 130, 230, 140);

        Button createButton = MakeButton(createCard, "CreateRoomButton", "Tạo phòng", 64, Green, GreenDark, 52, true);
        TopRow((RectTransform)createButton.transform, 50, 50, 300, 180);

        // ---------------- Phòng chờ ----------------
        RectTransform roomRoot = NewUI("RoomPanel" + Suffix, ct);
        Fill(roomRoot, 0, 0, 0, 0);
        RoomPanelUI roomUi = roomRoot.gameObject.AddComponent<RoomPanelUI>();
        RectTransform roomSafe = MakeSafeArea(roomRoot);

        TextMeshProUGUI roomTitle = Label(roomSafe, "Title", "Phòng chờ", 64, Color.white, TextAlignmentOptions.MidlineLeft, true);
        TopRow(roomTitle.rectTransform, 156, 520, 40, 120); // chừa bên phải cho nút Thoát phòng

        TextMeshProUGUI roomNameText = Label(roomSafe, "RoomNameText", "", 54, Color.white, TextAlignmentOptions.MidlineLeft, true);
        TopRow(roomNameText.rectTransform, 90, 330, 220, 110);

        RectTransform countChip = NewUI("PlayerCountChip", roomSafe);
        Round(countChip.gameObject.AddComponent<Image>(), Card, 45);
        TopRight(countChip, 90, 230, 220, 90);
        TextMeshProUGUI playerCountText = Label(countChip, "PlayerCountText", "0/0", 48, TextSoft, TextAlignmentOptions.Center, true);
        Fill(playerCountText.rectTransform, 0, 0, 0, 0);

        RectTransform playerScroll = MakeScroll(roomSafe, "PlayerScroll", true, 30, 0, out RectTransform playerContent);
        TopRow(playerScroll, 90, 90, 370, 1250);

        TextMeshProUGUI readyHint = Label(roomSafe, "ReadyHint", "Cả phòng sẵn sàng thì trận sẽ bắt đầu", 42,
            TextSecondary, TextAlignmentOptions.Center, false);
        BottomRow(readyHint.rectTransform, 90, 90, 290, 60);

        Button readyButton = MakeButton(roomSafe, "ReadyButton", "Sẵn sàng", 72, Green, GreenDark, 56, true);
        BottomRow((RectTransform)readyButton.transform, 90, 90, 60, 200);
        TMP_Text readyLabel = readyButton.GetComponentInChildren<TMP_Text>();

        // ---------------- Thanh trên dùng chung (luôn bật; LobbyNavigationUI bật/tắt nút Thoát phòng) ----------------
        RectTransform topRoot = NewUI("LobbyTopBar" + Suffix, ct);
        Fill(topRoot, 0, 0, 0, 0);
        RectTransform topSafe = MakeSafeArea(topRoot);
        RectTransform topBar = NewUI("TopBar", topSafe);
        TopRow(topBar, 90, 90, 40, 120);

        Button accountButton = MakeButton(topBar, "AccountButton", "<", 64, ButtonSoft, Color.white, 36, true);
        LeftMid((RectTransform)accountButton.transform, 0, 120, 120);

        Button leaveButton = MakeButton(topBar, "LeaveRoomButton", "Thoát phòng", 46, DangerSoft, DangerText, 36, false);
        RightMid((RectTransform)leaveButton.transform, 0, 400, 120);

        // ---------------- Popup chọn nhân vật ----------------
        RectTransform pickerRoot = NewUI("CharacterPickerPopup" + Suffix, ct);
        Fill(pickerRoot, 0, 0, 0, 0);
        CharacterPickerPopup pickerUi = pickerRoot.gameObject.AddComponent<CharacterPickerPopup>();

        RectTransform pickerView = NewUI("Root", pickerRoot);
        Fill(pickerView, 0, 0, 0, 0);
        pickerView.gameObject.AddComponent<Image>().color = Dim; // nền mờ, chặn bấm xuyên xuống

        RectTransform sheet = NewUI("Sheet", pickerView);
        Round(sheet.gameObject.AddComponent<Image>(), Sheet, 70);
        Anchor(sheet, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, -100), new Vector2(0, 1820)); // kéo xuống 100 px để giấu góc bo phía dưới

        RectTransform body = NewUI("Body", sheet);
        Fill(body, 70, 70, 70, 230);

        TextMeshProUGUI pickerTitle = Label(body, "Title", "Chọn nhân vật", 64, Color.white, TextAlignmentOptions.MidlineLeft, true);
        TopRow(pickerTitle.rectTransform, 0, 140, 0, 110);

        Button closeButton = MakeButton(body, "CloseButton", "X", 52, ButtonSoft, Color.white, 34, true);
        TopRight((RectTransform)closeButton.transform, 0, 0, 110, 110);

        TextMeshProUGUI charLabel = Label(body, "CharacterLabel", "Nhân vật", 40, TextSecondary, TextAlignmentOptions.MidlineLeft, false);
        TopRow(charLabel.rectTransform, 0, 0, 144, 50);

        RectTransform charScroll = MakeScroll(body, "CharacterScroll", false, 24, 8, out RectTransform characterGrid);
        TopRow(charScroll, 0, 0, 210, 486);

        TextMeshProUGUI skinLabel = Label(body, "SkinLabel", "Màu da", 40, TextSecondary, TextAlignmentOptions.MidlineLeft, false);
        TopRow(skinLabel.rectTransform, 0, 0, 740, 50);

        RectTransform skinScroll = MakeScroll(body, "SkinScroll", false, 24, 8, out RectTransform skinGrid);
        TopRow(skinScroll, 0, 0, 806, 486);

        TextMeshProUGUI hintText = Label(body, "HintText", "Muốn mua thêm nhân vật/màu da: về Tài khoản > Shop.", 38,
            TextSecondary, TextAlignmentOptions.TopLeft, false, true);
        TopRow(hintText.rectTransform, 0, 0, 1330, 100);

        Button pickerConfirm = MakeButton(body, "ConfirmButton", "Xác nhận", 68, Green, GreenDark, 54, true);
        BottomRow((RectTransform)pickerConfirm.transform, 0, 0, 0, 190);

        // ---------------- Hộp xác nhận ----------------
        RectTransform dialogRoot = NewUI("UI_ConfirmDialog" + Suffix, ct);
        Fill(dialogRoot, 0, 0, 0, 0);
        UI_ConfirmDialog dialogUi = dialogRoot.gameObject.AddComponent<UI_ConfirmDialog>();

        RectTransform dialogView = NewUI("DialogPanel", dialogRoot);
        Fill(dialogView, 0, 0, 0, 0);
        dialogView.gameObject.AddComponent<Image>().color = new Color(Dim.r, Dim.g, Dim.b, 0.8f);

        RectTransform dialogCard = NewUI("Card", dialogView);
        Round(dialogCard.gameObject.AddComponent<Image>(), Card, 60);
        Anchor(dialogCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-495, -280), new Vector2(495, 280));

        TextMeshProUGUI messageText = Label(dialogCard, "MessageText", "", 56, Color.white, TextAlignmentOptions.Center, false, true);
        TopRow(messageText.rectTransform, 70, 70, 80, 230);

        Button cancelButton = MakeButton(dialogCard, "CancelButton", "Hủy", 52, ButtonSoft, TextSoft, 44, false);
        Anchor((RectTransform)cancelButton.transform, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(70, 70), new Vector2(-15, 230));

        Button confirmButton = MakeButton(dialogCard, "ConfirmButton", "Đồng ý", 52, Danger, Color.white, 44, true);
        Anchor((RectTransform)confirmButton.transform, new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(15, 70), new Vector2(-70, 230));

        // ---------------- Gán field ----------------
        Wire(listUi,
            "contentParent", roomContent,
            "itemPrefab", itemPrefab,
            "roomNameInput", roomNameInput,
            "maxPlayersInput", maxPlayersInput,
            "roomPanel", roomRoot.gameObject,
            "emptyText", emptyText.gameObject);

        Wire(roomUi,
            "playerListParent", playerContent,
            "rowPrefab", rowPrefab,
            "roomNameText", roomNameText,
            "playerCountText", playerCountText,
            "readyButtonText", readyLabel);

        Wire(pickerUi,
            "root", pickerView.gameObject,
            "characterGrid", characterGrid,
            "skinGrid", skinGrid,
            "cardPrefab", cardPrefab,
            "confirmButton", pickerConfirm,
            "closeButton", closeButton,
            "hintText", hintText);

        Wire(dialogUi,
            "dialogPanel", dialogView.gameObject,
            "messageText", messageText,
            "confirmButton", confirmButton,
            "cancelButton", cancelButton);

        // Nút bấm gọi hàm public của RoomListUI / RoomPanelUI (lưu trong scene như gán tay ở Inspector).
        UnityEventTools.AddVoidPersistentListener(createButton.onClick, listUi.OnClickCreateRoom);
        UnityEventTools.AddVoidPersistentListener(readyButton.onClick, roomUi.OnClickReady);

        if (nav == null)
        {
            GameObject navGo = new GameObject("LobbyNavigation" + Suffix);
            Undo.RegisterCreatedObjectUndo(navGo, "Build Lobby UI");
            navGo.transform.SetParent(ct, false);
            nav = navGo.AddComponent<LobbyNavigationUI>();
        }
        Wire(nav,
            "accountButton", accountButton,
            "leaveRoomButton", leaveButton,
            "roomPanel", roomRoot.gameObject,
            "roomListUI", listUi);

        // ---------------- Trạng thái ban đầu ----------------
        roomRoot.gameObject.SetActive(false);  // chỉ hiện sau khi vào phòng
        leaveButton.gameObject.SetActive(false);
        pickerView.gameObject.SetActive(false);
        dialogView.gameObject.SetActive(false);
        emptyText.gameObject.SetActive(false);

        // ---------------- Tắt các object cũ (không xóa) ----------------
        var disabled = new System.Collections.Generic.List<string>();
        DisableOld(oldList != null ? oldList.gameObject : null, canvas, disabled);
        DisableOld(oldRoom != null ? oldRoom.gameObject : null, canvas, disabled);
        DisableOld(oldPicker != null ? oldPicker.gameObject : null, canvas, disabled);
        DisableOld(oldDialog != null ? oldDialog.gameObject : null, canvas, disabled);
        DisableOld(oldAccountButton, canvas, disabled);
        DisableOld(oldLeaveButton, canvas, disabled);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = listRoot.gameObject;
        AssetDatabase.SaveAssets();

        Debug.Log("[BuildLobbyUI] Đã dựng giao diện Lobby và gán field. " +
                  (disabled.Count > 0 ? "Đã tắt object cũ: " + string.Join(", ", disabled) + " (xóa tay khi hài lòng). " : "") +
                  "Xem phòng chờ: bật \"RoomPanel" + Suffix + "\" và tắt \"RoomListPanel" + Suffix + "\" trong Hierarchy. " +
                  "Đặt Game view 1170x2532 và nhớ Ctrl+S để lưu scene.");
    }

    // ================= Prefab =================

    // Dòng phòng trong danh sách: tên phòng, số người (2/4), nút Vào.
    private static RoomListItem BuildRoomItemPrefab()
    {
        RectTransform root = NewUI("RoomItem" + Suffix, null);
        Round(root.gameObject.AddComponent<Image>(), Card, 44);
        root.gameObject.AddComponent<LayoutElement>().preferredHeight = 200;

        TextMeshProUGUI nameText = Label(root, "RoomNameText", "Room", 54, Color.white, TextAlignmentOptions.MidlineLeft, true);
        Anchor(nameText.rectTransform, Vector2.zero, Vector2.one, new Vector2(40, 0), new Vector2(-498, 0));

        RectTransform chip = NewUI("PlayerCountChip", root);
        Round(chip.gameObject.AddComponent<Image>(), InputBg, 45);
        RightMid(chip, 284, 190, 90);
        TextMeshProUGUI countText = Label(chip, "PlayerCountText", "0/0", 48, TextSoft, TextAlignmentOptions.Center, true);
        Fill(countText.rectTransform, 0, 0, 0, 0);

        Button join = MakeButton(root, "JoinButton", "Vào", 50, Green, GreenDark, 36, true);
        RightMid((RectTransform)join.transform, 40, 220, 120);

        RoomListItem item = root.gameObject.AddComponent<RoomListItem>();
        Wire(item, "roomNameText", nameText, "playerCountText", countText, "joinButton", join);
        return SavePrefab(root.gameObject, "RoomItem_Generated.prefab").GetComponent<RoomListItem>();
    }

    // Dòng người chơi trong phòng chờ: avatar, tên, nhân vật, trạng thái, nút Đổi nhân vật.
    private static PlayerRowUI BuildPlayerRowPrefab()
    {
        RectTransform root = NewUI("PlayerRow" + Suffix, null);
        Round(root.gameObject.AddComponent<Image>(), Card, 44);
        root.gameObject.AddComponent<LayoutElement>().preferredHeight = 230;

        RectTransform avatar = NewUI("Avatar", root);
        Image avatarImage = avatar.gameObject.AddComponent<Image>();
        avatarImage.sprite = CircleSprite;
        avatarImage.color = Soft;
        LeftMid(avatar, 40, 130, 130);

        TextMeshProUGUI nameText = Label(root, "NameText", "Tên", 56, Color.white, TextAlignmentOptions.MidlineLeft, true);
        MidBand(nameText.rectTransform, 200, 420, 6, 82);

        TextMeshProUGUI characterText = Label(root, "CharacterNameText", "Nhân vật", 40, TextSecondary, TextAlignmentOptions.MidlineLeft, false);
        MidBand(characterText.rectTransform, 200, 420, -76, -6);

        TextMeshProUGUI statusText = Label(root, "StatusText", "Chưa sẵn sàng", 42, TextSecondary, TextAlignmentOptions.MidlineRight, true);
        RightBox(statusText.rectTransform, 40, 10, 380, 70);

        Button change = MakeButton(root, "ChangeButton", "Đổi nhân vật", 40, ButtonSoft, TextSoft, 28, false);
        RightBox((RectTransform)change.transform, 40, -100, 330, 90);

        PlayerRowUI row = root.gameObject.AddComponent<PlayerRowUI>();
        Wire(row,
            "nameText", nameText,
            "statusText", statusText,
            "characterNameText", characterText,
            "changeButton", change);
        return SavePrefab(root.gameObject, "PlayerRow_Generated.prefab").GetComponent<PlayerRowUI>();
    }

    // Thẻ chọn nhân vật / màu da trong popup (dùng lại script ShopItemCard, nhưng ngoại hình mới).
    private static ShopItemCard BuildPickerCardPrefab()
    {
        RectTransform root = NewUI("PickerCard" + Suffix, null);
        LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 327;
        layout.preferredHeight = 470;

        // Khung vàng nằm sau thẻ, nhô ra 6 px; chỉ hiện khi thẻ đang được chọn.
        RectTransform mark = NewUI("SelectedMark", root);
        Round(mark.gameObject.AddComponent<Image>(), Yellow, 40);
        Fill(mark, -6, -6, -6, -6);
        mark.gameObject.SetActive(false);

        RectTransform cardBg = NewUI("Bg", root);
        Round(cardBg.gameObject.AddComponent<Image>(), CardOnSheet, 36);
        Fill(cardBg, 0, 0, 0, 0);

        RectTransform iconBg = NewUI("IconBg", root);
        Round(iconBg.gameObject.AddComponent<Image>(), Soft, 28);
        TopCenter(iconBg, 20, 250, 250);

        // Ảnh icon là Image riêng, trắng, không tô màu; ShopItemCard.Setup tự bật/tắt khi có/không có sprite.
        RectTransform iconRt = NewUI("Icon", iconBg);
        Image icon = iconRt.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;
        Fill(iconRt, 10, 10, 10, 10);

        TextMeshProUGUI nameText = Label(root, "Name", "Tên", 44, Color.white, TextAlignmentOptions.Center, true);
        TopRow(nameText.rectTransform, 10, 10, 282, 56);

        Button button = MakeButton(root, "Button", "Chọn", 42, Green, GreenDark, 28, true);
        BottomRow((RectTransform)button.transform, 20, 20, 20, 100);

        ShopItemCard card = root.gameObject.AddComponent<ShopItemCard>();
        Wire(card,
            "icon", icon,
            "nameText", nameText,
            "button", button,
            "buttonText", button.GetComponentInChildren<TMP_Text>(),
            "selectedMark", mark.gameObject);
        return SavePrefab(root.gameObject, "PickerCard_Generated.prefab").GetComponent<ShopItemCard>();
    }

    // Lưu object tạm thành prefab rồi hủy bản tạm. Trả về GameObject của prefab (asset).
    private static GameObject SavePrefab(GameObject temp, string fileName)
    {
        string path = PrefabFolder + "/" + fileName;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        return prefab;
    }

    // ================= Tiện ích =================

    // Bản gốc của scene (không phải bản " (Generated)"), kể cả đang bị tắt.
    private static T FindOriginal<T>() where T : Component
    {
        foreach (T t in Object.FindObjectsOfType<T>(true))
            if (!t.gameObject.name.EndsWith(Suffix)) return t;
        return null;
    }

    private static Canvas ResolveCanvas(RoomListUI list, RoomPanelUI room, LobbyNavigationUI nav)
    {
        foreach (Component c in new Component[] { list, room, nav })
        {
            if (c == null) continue;
            Canvas canvas = c.GetComponentInParent<Canvas>(true);
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

    private static void DisableOld(GameObject go, Canvas canvas, System.Collections.Generic.List<string> log)
    {
        if (go == null || !go.activeSelf) return;
        // Không tắt Canvas hay object chứa các UI vừa dựng.
        if (go == canvas.gameObject || canvas.transform.IsChildOf(go.transform)) return;
        if (go.name.EndsWith(Suffix)) return;

        Undo.RecordObject(go, "Disable old Lobby UI");
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
