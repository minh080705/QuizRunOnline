using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hàm dùng chung cho các script dựng Canvas trong Editor (BuildAccountUI, BuildLobbyUI...):
/// bảng màu, tạo thành phần UI (chữ, nút, ô nhập, ScrollView), neo vị trí theo pixel của độ phân giải thiết kế 1170x2532.
/// Gọi Init() một lần ở đầu mỗi lần dựng.
/// </summary>
public static class UIBuildKit
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1170, 2532);

    // ---------------- Bảng màu (khớp mockup) ----------------
    public static readonly Color Bg = Hex("171A33");
    public static readonly Color Card = Hex("262B52");
    public static readonly Color Sheet = Hex("1F2347");
    public static readonly Color Soft = Hex("343A70");
    public static readonly Color ButtonSoft = Hex("2B3160");
    public static readonly Color Green = Hex("3ECF8E");
    public static readonly Color GreenDark = Hex("073B27");
    public static readonly Color Yellow = Hex("FFC83D");
    public static readonly Color Danger = Hex("E5484D");
    public static readonly Color DangerSoft = Hex("3A1F2B");
    public static readonly Color DangerText = Hex("FF8A8A");
    public static readonly Color TextSecondary = Hex("AAB0D8");
    public static readonly Color TextSoft = Hex("D6DAF5");
    public static readonly Color InputBg = Hex("171A33");
    public static readonly Color Placeholder = Hex("6F76AB");
    public static readonly Color Dim = new Color(0.02f, 0.024f, 0.07f, 0.72f);

    // ---------------- Trạng thái của lần dựng hiện tại ----------------
    public static TMP_FontAsset Font;
    public static Sprite RoundSprite;
    public static Sprite CircleSprite;

    /// <summary>Tắt khi dựng prefab tạm (sẽ bị hủy ngay), tránh làm rối lịch sử Undo.</summary>
    public static bool TrackUndo = true;

    public static void Init(TMP_FontAsset font)
    {
        Font = font != null ? font : TMP_Settings.defaultFontAsset;
        RoundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        CircleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        TrackUndo = true;
    }

    // ---------------- Canvas Scaler ----------------

    /// <summary>
    /// Hỏi xác nhận trước khi dựng. Nếu Canvas Scaler chưa là Scale With Screen Size 1170x2532 thì hỏi thêm có đổi không.
    /// Trả về false nếu người dùng hủy.
    /// </summary>
    public static bool ConfirmBuild(Canvas canvas, string title, string summary, out bool changeScaler)
    {
        changeScaler = false;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        bool scalerOk = scaler != null
            && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
            && scaler.referenceResolution == ReferenceResolution;

        if (scalerOk)
            return EditorUtility.DisplayDialog(title, summary, "Dựng", "Hủy");

        int choice = EditorUtility.DisplayDialogComplex(title,
            summary + "\n\nCanvas Scaler hiện chưa là Scale With Screen Size 1170x2532. Đổi sang cấu hình này thì các UI " +
            "có sẵn trong Canvas cũng co giãn theo (kích thước pixel của chúng có thể đổi).",
            "Dựng + đổi Canvas Scaler", "Dựng, giữ Canvas Scaler", "Hủy");
        if (choice == 2) return false;

        changeScaler = choice == 0;
        return true;
    }

    public static void ApplyScaler(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);

        Undo.RecordObject(scaler, "Canvas Scaler");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f; // khớp theo chiều ngang: bề ngang giữ nguyên trên mọi điện thoại dọc
    }

    // ---------------- Gán field ----------------

    /// <summary>Gán các field [SerializeField] (kể cả private) theo cặp tên, giá trị.</summary>
    public static void Wire(Object target, params object[] pairs)
    {
        SerializedObject so = new SerializedObject(target);
        for (int i = 0; i + 1 < pairs.Length; i += 2)
        {
            string field = (string)pairs[i];
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogWarning($"[UIBuildKit] {target.GetType().Name} không có field \"{field}\".");
                continue;
            }

            if (pairs[i + 1] is bool flag) p.boolValue = flag;
            else p.objectReferenceValue = pairs[i + 1] as Object;
        }
        so.ApplyModifiedProperties();
    }

    // ---------------- Tạo thành phần ----------------

    public static RectTransform NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        if (TrackUndo) Undo.RegisterCreatedObjectUndo(go, "Build UI");
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static RectTransform MakeSafeArea(Transform parent)
    {
        RectTransform safe = NewUI("SafeArea", parent);
        Fill(safe, 0, 0, 0, 0);
        safe.gameObject.AddComponent<SafeAreaFitter>();
        return safe;
    }

    public static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color,
        TextAlignmentOptions align, bool bold, bool wrap = false)
    {
        RectTransform rt = NewUI(name, parent);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (Font != null) t.font = Font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.enableWordWrapping = wrap;
        t.overflowMode = wrap ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        return t;
    }

    public static Button MakeButton(Transform parent, string name, string label, float size, Color bg, Color fg,
        float radius, bool bold)
    {
        RectTransform rt = NewUI(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        Round(img, bg, radius);
        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;

        // Nút bị khóa (interactable = false) tối đi thay vì trong suốt một nửa.
        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(0.55f, 0.57f, 0.72f, 1f);
        button.colors = colors;

        TextMeshProUGUI t = Label(rt, "Label", label, size, fg, TextAlignmentOptions.Center, bold);
        Fill(t.rectTransform, 0, 0, 0, 0);
        return button;
    }

    public static TMP_InputField MakeInput(Transform parent, string name, string placeholderText, float size = 56f)
    {
        RectTransform rt = NewUI(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        Round(img, InputBg, 36);
        TMP_InputField field = rt.gameObject.AddComponent<TMP_InputField>();

        RectTransform area = NewUI("Text Area", rt);
        Fill(area, 40, 40, 10, 10);
        area.gameObject.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = Label(area, "Placeholder", placeholderText, size, Placeholder,
            TextAlignmentOptions.MidlineLeft, false);
        placeholder.fontStyle = FontStyles.Italic;
        Fill(placeholder.rectTransform, 0, 0, 0, 0);

        TextMeshProUGUI text = Label(area, "Text", "", size, Color.white, TextAlignmentOptions.MidlineLeft, false);
        Fill(text.rectTransform, 0, 0, 0, 0);

        field.targetGraphic = img;
        field.textViewport = area;
        field.textComponent = text;
        field.placeholder = placeholder;
        if (Font != null) field.fontAsset = Font;
        field.pointSize = size;
        return field;
    }

    /// <summary>
    /// ScrollView tối giản: Viewport (RectMask2D) + Content có Layout Group và Content Size Fitter.
    /// Trả về content; phần tử con nên có LayoutElement (preferred width/height).
    /// </summary>
    public static RectTransform MakeScroll(Transform parent, string name, bool vertical, float spacing, int padding,
        out RectTransform content, int gridColumns = 0, Vector2 gridCell = default)
    {
        RectTransform root = NewUI(name, parent);
        ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();

        RectTransform viewport = NewUI("Viewport", root);
        Fill(viewport, 0, 0, 0, 0);
        Image hit = viewport.gameObject.AddComponent<Image>(); // trong suốt, để nhận thao tác kéo
        hit.color = new Color(0, 0, 0, 0);
        viewport.gameObject.AddComponent<RectMask2D>();

        content = NewUI("Content", viewport);
        if (vertical)
        {
            content.pivot = new Vector2(0.5f, 1f);
            Anchor(content, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);

            if (gridColumns > 0)
            {
                // Lưới nhiều cột (cuộn dọc): ô cố định gridCell, `gridColumns` cột.
                GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = gridCell;
                grid.spacing = new Vector2(spacing, spacing);
                grid.padding = new RectOffset(0, 0, padding, padding);
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.childAlignment = TextAnchor.UpperLeft;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = gridColumns;
            }
            else
            {
                VerticalLayoutGroup group = content.gameObject.AddComponent<VerticalLayoutGroup>();
                group.spacing = spacing;
                group.padding = new RectOffset(0, 0, padding, padding);
                group.childControlWidth = true;
                group.childControlHeight = true;
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;
            }

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        else
        {
            content.pivot = new Vector2(0f, 0.5f);
            Anchor(content, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, Vector2.zero);

            HorizontalLayoutGroup group = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.padding = new RectOffset(padding, padding, padding, padding);
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = !vertical;
        scroll.vertical = vertical;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 40f;
        return root;
    }

    // Bo góc bằng sprite UISprite (9-slice): viền gốc 10 px, nới viền hiển thị ~ 1,25 lần bán kính.
    public static void Round(Image img, Color color, float radius)
    {
        img.sprite = RoundSprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = Mathf.Clamp(8f / radius, 0.02f, 10f);
        img.color = color;
    }

    // ---------------- Neo vị trí (đơn vị: pixel của độ phân giải thiết kế) ----------------

    public static void Anchor(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
    {
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = offMin;
        rt.offsetMax = offMax;
    }

    public static void Fill(RectTransform rt, float left, float right, float top, float bottom)
    {
        Anchor(rt, Vector2.zero, Vector2.one, new Vector2(left, bottom), new Vector2(-right, -top));
    }

    // Hàng giãn ngang, bám mép trên: cách mép trên `top`, cao `height`.
    public static void TopRow(RectTransform rt, float left, float right, float top, float height)
    {
        Anchor(rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(left, -top - height), new Vector2(-right, -top));
    }

    // Hàng giãn ngang, bám mép dưới.
    public static void BottomRow(RectTransform rt, float left, float right, float bottom, float height)
    {
        Anchor(rt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(left, bottom), new Vector2(-right, bottom + height));
    }

    // Kích thước cố định, căn giữa ngang, bám mép trên.
    public static void TopCenter(RectTransform rt, float top, float width, float height)
    {
        Anchor(rt, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(-width / 2f, -top - height), new Vector2(width / 2f, -top));
    }

    // Kích thước cố định, bám góc trên-phải.
    public static void TopRight(RectTransform rt, float right, float top, float width, float height)
    {
        Anchor(rt, new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-right - width, -top - height), new Vector2(-right, -top));
    }

    // Kích thước cố định, bám mép trái, giữa theo chiều dọc.
    public static void LeftMid(RectTransform rt, float x, float width, float height)
    {
        Anchor(rt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(x, -height / 2f), new Vector2(x + width, height / 2f));
    }

    // Kích thước cố định, bám mép phải, giữa theo chiều dọc.
    public static void RightMid(RectTransform rt, float right, float width, float height)
    {
        Anchor(rt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-right - width, -height / 2f), new Vector2(-right, height / 2f));
    }

    // Hộp bám mép phải; `bottom` tính từ đường giữa (âm = phía dưới đường giữa).
    public static void RightBox(RectTransform rt, float right, float bottom, float width, float height)
    {
        Anchor(rt, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-right - width, bottom), new Vector2(-right, bottom + height));
    }

    // Dải giãn ngang quanh đường giữa; bottom/top tính từ đường giữa.
    public static void MidBand(RectTransform rt, float left, float right, float bottom, float top)
    {
        Anchor(rt, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(left, bottom), new Vector2(-right, top));
    }

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
