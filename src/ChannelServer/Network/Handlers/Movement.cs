﻿// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aura.Channel.Util;
using Aura.Shared.Network;
using Aura.Channel.World;
using Aura.Shared.Util;
using Aura.Data;
using Aura.Data.Database;
using Aura.Channel.Network.Sending;
using Aura.Mabi.Const;
using Aura.Mabi.Network;

namespace Aura.Channel.Network.Handlers
{
	public partial class ChannelServerHandlers : PacketHandlerManager<ChannelClient>
	{
		/// <summary>
		/// Simple walking/running.
		/// </summary>
		/// <example>
		/// 001 [........0000321F] Int    : 12831
		/// 002 [........0000966F] Int    : 38511
		/// 003 [..............01] Byte   : 1
		/// 004 [..............00] Byte   : 0
		/// </example>
		[PacketHandler(Op.Run, Op.Walk)]
		public void Run(ChannelClient client, Packet packet)
		{
			var x = packet.GetInt();
			var y = packet.GetInt();

			var creature = client.GetCreatureSafe(packet.Id);

			var from = creature.GetPosition();
			var to = new Position(x, y);
			var walk = (packet.Op == Op.Walk);

			if (creature.Conditions.Has(ConditionsE.Meditation) && !walk && (creature.Inventory.RightHand == null || (!creature.Inventory.RightHand.HasTag("/wand/") && !creature.Inventory.RightHand.HasTag("/staff/"))))
				throw new ModerateViolation("Tried to run with meditation active.");

			//Position intersection;
			//if (creature.Region.Collissions.Find(from, to, out intersection))
			//{
			//    Aura.Shared.Util.Log.Debug("Intersection at '{0}'", intersection);
			//}

			// Telewalk command
			if (walk && creature.Vars.Temp["telewalk"] != null)
			{
				creature.SetPosition(to.X, to.Y);
				Send.Effect(creature, Effect.SilentMoveTeleport, (byte)2, to.X, to.Y);
				Send.SkillTeleport(creature, to.X, to.Y);

				creature.Region.ActivateAis(creature, to, to);
				return;
			}

			creature.Region.ActivateAis(creature, from, to);

			creature.Move(to, walk);
		}

		/// <summary>
		/// Sent when a client side event is triggered.
		/// </summary>
		/// <remarks>
		/// For example, when entering a new area.
		/// The client events also trigger new BGM and stuff like that.
		/// </remarks>
		/// <example>
		/// 001 [00B0000100090576] Long   : 49539600196633974
		/// 002 [........00000065] Int    : 101
		/// 003 [................] String : 
		/// </example>
		[PacketHandler(Op.EventInform)]
		public void EventInform(ChannelClient client, Packet packet)
		{
			var eventId = packet.GetLong();
			var signalType = (SignalType)packet.GetInt();
			var unkString = packet.GetString();

			var creature = client.GetCreatureSafe(packet.Id);

			// Get region by id in event id
			// We can't get it from the creature, because when an event is
			// triggered the creature might already be in a different region,
			// as is the case for leaving region events.
			var regionId = (ushort)((ulong)eventId >> 32);
			var region = ChannelServer.Instance.World.GetRegion(regionId);
			if (region == null)
			{
				Log.Warning("EventInform: Unknown region '{0}'", regionId);
				return;
			}

			// Get event
			var clientEvent = region.GetClientEvent(eventId);
			if (clientEvent == null)
			{
				Log.Warning("EventInform: Player '{0:X16}' triggered unknown event '{1:X16}'.", creature.EntityId, eventId);
				return;
			}

			// Check range
			// TODO: Checking region id doesn't work because a "leave" event
			//   can be triggered after we've changed the creature's region.
			//   Checking position doesn't work because the events can be
			//   quite large.
			// TODO: Check the shape?
			if (clientEvent.Data.RegionId != creature.RegionId && signalType == SignalType.Enter)
			{
				Log.Warning("EventInform: Player '{0:X16}' triggered event '{1:X16}' out of range.", creature.EntityId, eventId);
				return;
			}

			// Check handler
			var handler = clientEvent.Handlers.Get(signalType);
			if (handler == null) return;

			// Run
			handler(creature, clientEvent.Data);
		}

		/// <summary>
		/// Sent when a client side music event is triggered.
		/// </summary>
		/// <remarks>
		/// This is sent since 190X? It contains the names of the events
		/// that change the BGM, why this is sent is unknown.
		/// It's also untested whether you still get EventInform,
		/// which seems to do almost the same...
		/// </remarks>
		/// <example>
		/// 001 [................] String : TirChonaill_North3_ambient
		/// </example>
		[PacketHandler(Op.MusicEventInform)]
		public void MusicEventInform(ChannelClient client, Packet packet)
		{
			var eventName = packet.GetString();

			var creature = client.GetCreatureSafe(packet.Id);

			// Do something with this information?
		}
	}
}
