﻿// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Channel.Network.Sending;
using Aura.Channel.Skills.Base;
using Aura.Channel.Skills.Combat;
using Aura.Channel.World.Entities;
using Aura.Shared.Mabi.Const;
using Aura.Shared.Network;
using Aura.Shared.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aura.Channel.Skills.Magic
{
	/// <summary>
	/// Firebolt skill handler
	/// </summary>
	/// <remarks>
	/// Var1: Min damage
	/// Var2: Max damage
	/// Var3: ?
	/// 
	/// Contrary to what the Wiki says, training seems to be exactly the
	/// same as Icebolt, the Wiki is either outdated or incorrect.
	/// </remarks>
	[Skill(SkillId.Firebolt)]
	public class Firebolt : Icebolt
	{
		/// <summary>
		/// ID of the skill, used in training.
		/// </summary>
		protected override SkillId SkillId { get { return SkillId.Firebolt; } }

		/// <summary>
		/// String used in effect packets.
		/// </summary>
		protected override string EffectSkillName { get { return "firebolt"; } }

		/// <summary>
		/// Weapon tag that's looked for in range calculation.
		/// </summary>
		protected override string SpecialWandTag { get { return "fire_wand"; } }

		/// <summary>
		/// Handles knock back/stun/death.
		/// </summary>
		protected virtual void HandleKnockBack(Creature attacker, Creature target, TargetAction tAction)
		{
			attacker.Shove(target, KnockbackDistance);
			if (target.IsDead)
				tAction.Set(TargetOptions.FinishingKnockDown);
			else
				tAction.Set(TargetOptions.KnockDown);
		}

		/// <summary>
		/// Actions to be done before the combat action pack is handled.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="skill"></param>
		protected virtual void BeforeHandlingPack(Creature attacker, Skill skill)
		{
			skill.Stacks = 0;
		}

		/// <summary>
		/// Returns damage for creature using skill.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <returns></returns>
		protected override float GetDamage(Creature creature, Skill skill)
		{
			var damage = creature.GetRndMagicDamage(skill, skill.RankData.Var1, skill.RankData.Var2);

			if (skill.Stacks < skill.RankData.StackMax)
				damage *= skill.Stacks;
			else
				damage *= 6.5f;

			return damage;
		}
	}
}