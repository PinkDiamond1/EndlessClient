﻿using EOLib.Domain.Character;
using System;

namespace EOLib.Domain.Extensions
{
    public static class CharacterExtensions
    {
        public static Character.Character WithAppliedData(this Character.Character original, Character.Character updatedData, bool isRangedWeapon)
        {
            var existingRenderProps = original.RenderProperties;
            var newRenderProps = updatedData.RenderProperties.ToBuilder();

            newRenderProps.CurrentAction = updatedData.RenderProperties.SitState == SitState.Standing
                ? CharacterActionState.Standing
                : CharacterActionState.Sitting;
            newRenderProps.IsDead = existingRenderProps.IsDead;
            newRenderProps.IsRangedWeapon = isRangedWeapon;
            newRenderProps.IsDrunk = existingRenderProps.IsDrunk;
            newRenderProps.IsHidden = existingRenderProps.IsHidden;

            var existingStats = original.Stats;
            var newStats = existingStats
                .WithNewStat(CharacterStat.Level, existingStats[CharacterStat.Level])
                .WithNewStat(CharacterStat.HP, existingStats[CharacterStat.HP])
                .WithNewStat(CharacterStat.MaxHP, existingStats[CharacterStat.MaxHP])
                .WithNewStat(CharacterStat.TP, existingStats[CharacterStat.TP])
                .WithNewStat(CharacterStat.MaxTP, existingStats[CharacterStat.MaxTP]);

            return original
                .WithName(updatedData.Name)
                .WithGuildTag(updatedData.GuildTag)
                .WithMapID(updatedData.MapID)
                .WithRenderProperties(newRenderProps.ToImmutable())
                .WithStats(newStats);
        }

        public static Character.Character WithDamage(this Character.Character original, int damageTaken, bool isDead)
        {
            var stats = original.Stats;
            stats = stats.WithNewStat(CharacterStat.HP, (short)Math.Max(stats[CharacterStat.HP] - damageTaken, 0));

            var props = original.RenderProperties
                .WithIsDead(isDead);

            return original.WithStats(stats).WithRenderProperties(props);
        }
    }
}
