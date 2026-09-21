using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBuffManager : MonoBehaviour
{
    private class BuffRuntimeState
    {
        public int currentStack = 0;
        public GameObject effectInstance;
        // Xóa: public Coroutine timeoutRoutine; -- không cần biến chặn 1-coroutine-duy-nhất nữa
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
        if (effectPrefab != null)
            state.effectInstance = Instantiate(effectPrefab, transform.position, Quaternion.identity, transform);

        StartCoroutine(RemoveEffectAfter(state, duration));
    }

    private IEnumerator RemoveEffectAfter(BuffRuntimeState state, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (state.effectInstance != null) Destroy(state.effectInstance);
    }

    private IEnumerator Timeout(Buff buffDef, BuffRuntimeState state)
    {
        yield return new WaitForSeconds(buffDef.buffDuration);

        if (state.currentStack > 0) state.currentStack--;
        buffDef.OnStackChanged(stats, state.currentStack, state.context);
        
    }
}