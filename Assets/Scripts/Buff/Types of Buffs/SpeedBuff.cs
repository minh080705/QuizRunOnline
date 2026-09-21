using UnityEngine;

[CreateAssetMenu(fileName = "SpeedBuff", menuName = "Buffs/Speed Buff")]
public class SpeedBuff : Buff
{
    public float speedMultiplier = 1.5f;

    public override void OnFirstApplied(PlayerStats player, BuffContext context)
    {
        context.Set("baseMoveSpeed", player.moveSpeed);
        context.Set("baseSideSpeed", player.sideSpeed);
    }

    public override void OnStackChanged(PlayerStats player, int currentStack, BuffContext context)
    {
        float baseMoveSpeed = context.Get("baseMoveSpeed", player.moveSpeed);
        float baseSideSpeed = context.Get("baseSideSpeed", player.sideSpeed);

        if (currentStack == 0)
        {
            player.moveSpeed = baseMoveSpeed;
            player.sideSpeed = baseSideSpeed;
            return;
        }

        float multiplier = Mathf.Pow(speedMultiplier, currentStack);
        player.moveSpeed = baseMoveSpeed * multiplier;
        player.sideSpeed = baseSideSpeed * multiplier;
    }
}