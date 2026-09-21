using Fusion;
using UnityEngine;

[System.Serializable]
public class CharacterEntry
{
    public string displayName;
    public NetworkPrefabRef prefab;
}

public class CharacterCatalog : MonoBehaviour
{
    public static CharacterCatalog Instance;

    [SerializeField] private CharacterEntry[] characters;

    void Awake() => Instance = this;

    public CharacterEntry Get(int index) => characters[Wrap(index)];

    public int Wrap(int index)
    {
        int len = characters.Length;
        return ((index % len) + len) % len; // đảm bảo vòng lặp cả khi index âm
    }
}