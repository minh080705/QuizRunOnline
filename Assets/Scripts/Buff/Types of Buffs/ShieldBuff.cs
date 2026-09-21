using UnityEngine;

[CreateAssetMenu(fileName = "ShieldBuff", menuName = "Buffs/Shield Buff")]
public class ShieldBuff : Buff
{
    public override void OnFirstApplied(PlayerStats player, BuffContext context) { }

    public override void OnStackChanged(PlayerStats player, int currentStack, BuffContext context)
    {
        var handler = player.GetComponent<PlayerCollisionHandler>();
        if (handler != null) handler.isShieldOn = currentStack > 0;
    }
}