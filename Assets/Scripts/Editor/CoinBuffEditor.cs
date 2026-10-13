using UnityEditor;

/// <summary>
/// Inspector của CoinBuff: ẩn ô Buff Effect Prefab thừa kế từ lớp Buff (Coin Buff dùng ô Magnet Model riêng), để không
/// gán nhầm vào đó (PlayerBuffManager sẽ sinh prefab ấy ngay chân nhân vật, không có độ lệch hay xoay).
/// </summary>
[CustomEditor(typeof(CoinBuff))]
public class CoinBuffEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        DrawPropertiesExcluding(serializedObject, "m_Script", "buffEffectPrefab");

        serializedObject.ApplyModifiedProperties();
    }
}
