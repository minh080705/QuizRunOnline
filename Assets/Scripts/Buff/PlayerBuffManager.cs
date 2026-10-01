using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBuffManager : MonoBehaviour
{
    private class BuffRuntimeState
    {
        public int currentStack = 0;
        public GameObject effectInstance;
        public BuffContext context = new BuffContext();
    }

    private PlayerStats stats;
    private Dictionary<Buff, BuffRuntimeState> activeBuffs = new();

    void Awake() => stats = GetComponent<PlayerStats>();

    public void ApplyStack(Buff buffDef)
    {
        if (!activeBuffs.TryGetValue(buffDef, out var state))
        {
            state = new BuffRuntimeState();
            activeBuffs[buffDef] = state;
            buffDef.OnFirstApplied(stats, state.context);
        }

        if (state.currentStack >= buffDef.maxStack) return;

        state.currentStack++;
        buffDef.OnStackChanged(stats, state.currentStack, state.context);
        PlayEffect(state, buffDef.buffEffectPrefab, buffDef.buffDuration);

        StartCoroutine(Timeout(buffDef, state));
    }

    private void PlayEffect(BuffRuntimeState state, GameObject effectPrefab, float duration)
    {
        if (state.effectInstance != null) Destroy(state.effectInstance);

        GameObject newInstance = null;
        if (effectPrefab != null)
            newInstance = Instantiate(effectPrefab, transform.position, Quaternion.identity, transform);

        state.effectInstance = newInstance;

        StartCoroutine(RemoveEffectAfter(state, newInstance, duration));
    }

    private IEnumerator RemoveEffectAfter(BuffRuntimeState state, GameObject instance, float duration)
    {
        yield return new WaitForSeconds(duration);

        // Chỉ destroy nếu instance này vẫn đang là effect hiện tại
        // (chưa bị một lần ApplyStack khác thay thế bằng effect mới hơn)
        if (state.effectInstance == instance)
        {
            if (instance != null) Destroy(instance);
            state.effectInstance = null;
        }
    }

    private IEnumerator Timeout(Buff buffDef, BuffRuntimeState state)
    {
        yield return new WaitForSeconds(buffDef.buffDuration);

        if (state.currentStack > 0) state.currentStack--;
        buffDef.OnStackChanged(stats, state.currentStack, state.context);

        // Buff hết hoàn toàn -> xóa entry để lần áp dụng sau baseline lại từ đầu (OnFirstApplied)
        if (state.currentStack <= 0
            && activeBuffs.TryGetValue(buffDef, out var currentState)
            && currentState == state)
        {
            activeBuffs.Remove(buffDef);
        }
    }
}