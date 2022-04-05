﻿using AutomaticTypeMapper;
using EOLib.Domain.Interact.Quest;
using EOLib.Domain.Login;
using EOLib.Net;
using EOLib.Net.Handlers;
using Optional;
using System.Collections.Generic;

namespace EOLib.PacketHandlers.Quest
{
    [AutoMappedType]
    public class QuestDialogHandler : InGameOnlyPacketHandler
    {
        private readonly IQuestDataRepository _questDataRepository;

        private enum DialogEntryType : byte
        {
            Text = 1,
            Link
        }

        public override PacketFamily Family => PacketFamily.Quest;

        public override PacketAction Action => PacketAction.Dialog;

        public QuestDialogHandler(IPlayerInfoProvider playerInfoProvider,
                                  IQuestDataRepository questDataRepository)
            : base(playerInfoProvider)
        {
            _questDataRepository = questDataRepository;
        }

        public override bool HandlePacket(IPacket packet)
        {
            var numDialogs = packet.ReadChar();
            var vendorID = packet.ReadShort();
            var questID = packet.ReadShort();
            var sessionID = packet.ReadShort();
            var dialogID = packet.ReadShort();

            if (packet.ReadByte() != 255)
                return false;

            var questData = new QuestDialogData()
                .WithVendorID(vendorID)
                .WithQuestID(questID)
                .WithSessionID(sessionID) // not used by eoserv
                .WithDialogID(dialogID); // not used by eoserv

            var dialogTitles = new Dictionary<short, string>(numDialogs);
            for (int i = 0; i < numDialogs; i++)
                dialogTitles.Add(packet.ReadShort(), packet.ReadBreakString());

            var pages = new List<string>();
            var links = new List<(short, string)>();
            while (packet.ReadPosition < packet.Length)
            {
                var entryType = (DialogEntryType)packet.ReadShort();
                switch (entryType)
                {
                    case DialogEntryType.Text: pages.Add(packet.ReadBreakString()); break;
                    case DialogEntryType.Link: links.Add((packet.ReadShort(), packet.ReadBreakString())); break;
                    default: return false;
                }
            }

            questData = questData.WithDialogTitles(dialogTitles)
                .WithPageText(pages)
                .WithActions(links);

            _questDataRepository.QuestDialogData = Option.Some(questData);

            return true;
        }
    }
}
