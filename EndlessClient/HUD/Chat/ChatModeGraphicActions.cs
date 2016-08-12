﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using EndlessClient.ControlSets;
using EndlessClient.HUD.Controls;
using EndlessClient.UIControls;
using EOLib.Domain.Character;
using EOLib.Domain.Chat;
using Microsoft.Xna.Framework;

namespace EndlessClient.HUD.Chat
{
    public class ChatModeGraphicActions : IChatModeGraphicActions
    {
        private readonly IHudControlProvider _hudControlProvider;
        private readonly ICharacterProvider _characterProvider;
        private readonly IChatModeCalculatorService _chatModeCalculatorService;

        public ChatModeGraphicActions(IHudControlProvider hudControlProvider,
                                      ICharacterProvider characterProvider,
                                      IChatModeCalculatorService chatModeCalculatorService)
        {
            _hudControlProvider = hudControlProvider;
            _characterProvider = characterProvider;
            _chatModeCalculatorService = chatModeCalculatorService;
        }

        public void UpdateChatMode()
        {
            var chatText = _hudControlProvider.GetComponent<ChatTextBox>(HudControlIdentifier.ChatTextBox);
            var chatMode = _chatModeCalculatorService.CalculateChatType(chatText.Text,
                _characterProvider.MainCharacter.AdminLevel != AdminLevel.Player,
                !string.IsNullOrEmpty(_characterProvider.MainCharacter.GuildName));

            var chatModePicture = _hudControlProvider.GetComponent<PictureBox>(HudControlIdentifier.ChatModePictureBox);
            var width = ((Rectangle) chatModePicture.SourceRectangle).Width;
            var height = ((Rectangle) chatModePicture.SourceRectangle).Height;
            chatModePicture.SourceRectangle = new Rectangle(0, height*(int) chatMode, width, height);
        }
    }
}