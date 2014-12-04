﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;

using EOLib;

namespace EndlessClient
{
	public class EOClient : AsyncClient
	{
		private delegate void PacketHandler(Packet reader);

		//I COULD just use tuple...but it is easier to type when I make a wrapper that basically is a tuple.
		private struct FamilyActionPair
		{
			public PacketFamily fam;
			public PacketAction act;

			public FamilyActionPair(PacketFamily family, PacketAction action)
			{
				fam = family;
				act = action;
			}
		}
		
		//this is a wrapper that serializes thread access to the handler method. This serialization can be overriden.
		private class LockedHandlerMethod
		{
			private readonly PacketHandler _handler;
			private readonly bool _inGameOnly;

			public PacketHandler Handler
			{
				get
				{
					if(_inGameOnly && GameStates.PlayingTheGame != EOGame.Instance.State) //force ignore if the handler is an in-game only handler
						return p => { };
					lock (locker) return _handler;
				}
			}
			private static readonly object locker = new object();

			public LockedHandlerMethod(PacketHandler handler, bool inGameOnly = false)
			{
				_handler = handler;
				_inGameOnly = inGameOnly;
			}
		}
		private readonly Dictionary<FamilyActionPair, LockedHandlerMethod> handlers;
		
		public EOClient()
		{
			handlers = new Dictionary<FamilyActionPair, LockedHandlerMethod>
			{
				{
					new FamilyActionPair(PacketFamily.Account, PacketAction.Reply),
					new LockedHandlerMethod(Handlers.Account.AccountResponse)
				},
				{
					new FamilyActionPair(PacketFamily.Appear, PacketAction.Reply),
					new LockedHandlerMethod(Handlers.NPCPackets.AppearReply, true)
				},
				{
					new FamilyActionPair(PacketFamily.Avatar, PacketAction.Agree),
					new LockedHandlerMethod(Handlers.Avatar.AvatarAgree, true)
				},
				{
					new FamilyActionPair(PacketFamily.Avatar, PacketAction.Remove),
					new LockedHandlerMethod(Handlers.Avatar.AvatarRemove, true)
				},
				{
					new FamilyActionPair(PacketFamily.Character, PacketAction.Player),
					new LockedHandlerMethod(Handlers.Character.CharacterPlayerResponse)
				},
				{
					new FamilyActionPair(PacketFamily.Character, PacketAction.Reply),
					new LockedHandlerMethod(Handlers.Character.CharacterResponse)
				},
				{
					new FamilyActionPair(PacketFamily.Connection, PacketAction.Player),
					new LockedHandlerMethod(Handlers.Connection.PingResponse)
				},
				{
					new FamilyActionPair(PacketFamily.Door, PacketAction.Open), 
					new LockedHandlerMethod(Handlers.Door.DoorOpenResponse, true)
				},
				{
					new FamilyActionPair(PacketFamily.Face, PacketAction.Player),
					new LockedHandlerMethod(Handlers.Face.FacePlayerResponse)
				},
				{
					new FamilyActionPair(PacketFamily.Init, PacketAction.Init),
					new LockedHandlerMethod(Handlers.Init.InitResponse)
				},
				{
					new FamilyActionPair(PacketFamily.Item, PacketAction.Add), 
					new LockedHandlerMethod(Handlers.Item.ItemAddResponse, true)
				},
				{
					new FamilyActionPair(PacketFamily.Item, PacketAction.Drop), 
					new LockedHandlerMethod(Handlers.Item.ItemDropResponse, true)
				},
				{
					new FamilyActionPair(PacketFamily.Item, PacketAction.Get), 
					new LockedHandlerMethod(Handlers.Item.ItemGetResponse, true)
				},
				{
					new FamilyActionPair(PacketFamily.Item, PacketAction.Junk), 
					new LockedHandlerMethod(Handlers.Item.ItemJunkResponse, true)
				},
				{
					new FamilyActionPair(PacketFamily.Item, PacketAction.Remove), 
					new LockedHandlerMethod(Handlers.Item.ItemRemoveResponse, true)
				},
				{
					new FamilyActionPair(PacketFamily.Login, PacketAction.Reply),
					new LockedHandlerMethod(Handlers.Login.LoginResponse)
				},
				{
					new FamilyActionPair(PacketFamily.NPC, PacketAction.Player),
					new LockedHandlerMethod(Handlers.NPCPackets.NPCPlayer, true)
				},
				{
					new FamilyActionPair(PacketFamily.NPC, PacketAction.Spec),
					new LockedHandlerMethod(Handlers.NPCPackets.NPCSpec, true)
				},
				{
					new FamilyActionPair(PacketFamily.PaperDoll, PacketAction.Agree), 
					new LockedHandlerMethod(Handlers.Paperdoll.PaperdollAgree, true)
				},
				{
					new FamilyActionPair(PacketFamily.PaperDoll, PacketAction.Remove), 
					new LockedHandlerMethod(Handlers.Paperdoll.PaperdollRemove, true)
				},
				{
					new FamilyActionPair(PacketFamily.PaperDoll, PacketAction.Reply), 
					new LockedHandlerMethod(Handlers.Paperdoll.PaperdollReply, true)
				},
				{
					new FamilyActionPair(PacketFamily.Players, PacketAction.Agree), 
					new LockedHandlerMethod(Handlers.Players.PlayersAgree, true)
				},
				{
					new FamilyActionPair(PacketFamily.Refresh, PacketAction.Reply),
					new LockedHandlerMethod(Handlers.Refresh.RefreshReply, true)
				},
				{
					new FamilyActionPair(PacketFamily.StatSkill, PacketAction.Player), 
					new LockedHandlerMethod(Handlers.StatSkill.StatSkillPlayer, true)
				},
				//TALK PACKETS
				{
					new FamilyActionPair(PacketFamily.Talk, PacketAction.Message),
 					new LockedHandlerMethod(Handlers.Talk.TalkMessage, true)
				},
				{
					new FamilyActionPair(PacketFamily.Talk, PacketAction.Player),
 					new LockedHandlerMethod(Handlers.Talk.TalkPlayer, true)
				},
				{
					new FamilyActionPair(PacketFamily.Talk, PacketAction.Reply), 
					new LockedHandlerMethod(Handlers.Talk.TalkReply, true)
				},
				{
					new FamilyActionPair(PacketFamily.Talk, PacketAction.Request), 
					new LockedHandlerMethod(Handlers.Talk.TalkRequest, true)
				},
				{
					new FamilyActionPair(PacketFamily.Talk, PacketAction.Tell), 
					new LockedHandlerMethod(Handlers.Talk.TalkTell, true)
				},
				//
				{
					new FamilyActionPair(PacketFamily.Walk, PacketAction.Reply), 
					new LockedHandlerMethod(Handlers.Walk.WalkReply, true)
				},
				{
					new FamilyActionPair(PacketFamily.Walk, PacketAction.Player), 
					new LockedHandlerMethod(Handlers.Walk.WalkPlayer, true)
				},
				{
					new FamilyActionPair(PacketFamily.Warp, PacketAction.Agree), 
					new LockedHandlerMethod(Handlers.Warp.WarpAgree, true)
				},
				{
					new FamilyActionPair(PacketFamily.Warp, PacketAction.Request), 
					new LockedHandlerMethod(Handlers.Warp.WarpRequest, true)
				},
				{
					new FamilyActionPair(PacketFamily.Welcome, PacketAction.Reply),
					new LockedHandlerMethod(Handlers.Welcome.WelcomeResponse)
				}
			};
		}

		public new void Disconnect()
		{
			World.Instance.MainPlayer.Logout();
			base.Disconnect();
		}

		protected override void _handle(object state)
		{
			Packet pkt = (Packet)state;

			FamilyActionPair pair = new FamilyActionPair(pkt.Family, pkt.Action);
			if(handlers.ContainsKey(pair))
			{
				handlers[pair].Handler(pkt);
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (!disposing) return;

			Handlers.Account.Cleanup();
			Handlers.Character.Cleanup();
			Handlers.Init.Cleanup();
			Handlers.Login.Cleanup();
			Handlers.Walk.Cleanup();
			Handlers.Welcome.Cleanup();
		}
	}
}
