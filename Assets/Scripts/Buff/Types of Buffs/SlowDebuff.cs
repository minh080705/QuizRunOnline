using UnityEngine;

[CreateAssetMenu(fileName = "SlowDebuff", menuName = "Buffs/Slow Debuff")]
public class SlowDebuff : Buff
{
    public float speedMultiplier = 0.8f;

    public override void OnFirstApplied(PlayerStats player, BuffContext context)
    {
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