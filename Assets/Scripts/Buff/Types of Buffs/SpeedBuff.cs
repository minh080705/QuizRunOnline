using UnityEngine;

[CreateAssetMenu(fileName = "SpeedBuff", menuName = "Buffs/Speed Buff")]
public class SpeedBuff : Buff
{
    public float speedMultiplier = 1.5f;

    public override void OnFirstApplied(PlayerStats player, BuffContext context)
    {
        // sideSpeed không có acceleration nên vẫn cần snapshot base riêng
        context.Set("baseSideSpeed", player.sideSpeed);
    }

    public override void OnStackChanged(PlayerStats player, int currentStack, BuffContext context)
    {
        float baseSideSpeed = context.Get("baseSideSpeed", player.sideSpeed);

        if (currentStack == 0)
        {
            player.RemoveSpeedMultiplier(this);
            player.sideSpeed = baseSideSpeed;
            return;
        }

        float multiplier = Mathf.Pow(speedMultiplier, currentStack);
        player.SetSpeedMultiplier(this, multiplier);
        player.sideSpeed = baseSideSpeed * multiplier;
    }
}