﻿using AutomaticTypeMapper;
using EOLib.Domain.Character;
using EOLib.Domain.Login;
using EOLib.Domain.Map;
using EOLib.Net;
using EOLib.Net.Handlers;

namespace EOLib.PacketHandlers.AdminInteract
{
    /// <summary>
    /// Admin unhiding
    /// </summary>
    [AutoMappedType]
    public class AdminInteractAgree : InGameOnlyPacketHandler
    {
        private readonly ICharacterRepository _characterRepository;
        private readonly ICurrentMapStateRepository _currentMapStateRepository;

        public override PacketFamily Family => PacketFamily.AdminInteract;
        public override PacketAction Action => PacketAction.Agree;

        public AdminInteractAgree(IPlayerInfoProvider playerInfoProvider,
                                  ICharacterRepository characterRepository,
                                  ICurrentMapStateRepository currentMapStateRepository)
            : base(playerInfoProvider)
        {
            _characterRepository = characterRepository;
            _currentMapStateRepository = currentMapStateRepository;
        }

        public override bool HandlePacket(IPacket packet)
        {
            var id = packet.ReadShort();

            if (id == _characterRepository.MainCharacter.ID)
                _characterRepository.MainCharacter = Shown(_characterRepository.MainCharacter);
            else
            {
                if (_currentMapStateRepository.Characters.ContainsKey(id))
                {
                    var character = _currentMapStateRepository.Characters[id];

                    var updatedCharacter = Shown(character);
                    _currentMapStateRepository.Characters[id] = updatedCharacter;
                }
                else
                {
                    _currentMapStateRepository.UnknownPlayerIDs.Add(id);
                }
            }

            return true;
        }

        private static Character Shown(Character input)
        {
            var renderProps = input.RenderProperties.WithIsHidden(false);
            return input.WithRenderProperties(renderProps);
        }
    }
}
