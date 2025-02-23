﻿using AutomaticTypeMapper;
using EndlessClient.Audio;
using EndlessClient.Controllers;
using EndlessClient.HUD.Spells;
using EndlessClient.Input;
using EndlessClient.Rendering.Character;
using EOLib.Config;
using EOLib.Domain.Character;
using EOLib.Domain.Extensions;
using EOLib.Domain.Map;
using EOLib.IO.Map;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Optional;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EndlessClient.Rendering.Map
{
    [AutoMappedType(IsSingleton = true)]
    public class DynamicMapObjectUpdater : IDynamicMapObjectUpdater
    {
        private const int DOOR_CLOSE_TIME_MS = 3000;

        private class DoorTimePair
        {
            public Warp Door { get; set; }
            public DateTime OpenTime { get; set; }
        }

        private readonly ICharacterProvider _characterProvider;
        private readonly ICharacterRendererProvider _characterRendererProvider;
        private readonly ICurrentMapStateRepository _currentMapStateRepository;
        private readonly IUserInputRepository _userInputRepository;
        private readonly ICurrentMapProvider _currentMapProvider;
        private readonly IMapObjectBoundsCalculator _mapObjectBoundsCalculator;
        private readonly IMapInteractionController _mapInteractionController;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly ISpellSlotDataRepository _spellSlotDataRepository;
        private readonly ISfxPlayer _sfxPlayer;

        private readonly List<DoorTimePair> _cachedDoorState;
        private IMapFile _cachedMap;
        private List<MapCoordinate> _ambientSounds;

        public DynamicMapObjectUpdater(ICharacterProvider characterProvider,
                                       ICharacterRendererProvider characterRendererProvider,
                                       ICurrentMapStateRepository currentMapStateRepository,
                                       IUserInputRepository userInputRepository,
                                       ICurrentMapProvider currentMapProvider,
                                       IMapObjectBoundsCalculator mapObjectBoundsCalculator,
                                       IMapInteractionController mapInteractionController,
                                       IConfigurationProvider configurationProvider,
                                       ISpellSlotDataRepository spellSlotDataRepository,
                                       ISfxPlayer sfxPlayer)
        {
            _characterProvider = characterProvider;
            _characterRendererProvider = characterRendererProvider;
            _currentMapStateRepository = currentMapStateRepository;
            _userInputRepository = userInputRepository;
            _currentMapProvider = currentMapProvider;
            _mapObjectBoundsCalculator = mapObjectBoundsCalculator;
            _mapInteractionController = mapInteractionController;
            _configurationProvider = configurationProvider;
            _spellSlotDataRepository = spellSlotDataRepository;
            _sfxPlayer = sfxPlayer;

            _cachedDoorState = new List<DoorTimePair>();
            _ambientSounds = new List<MapCoordinate>();
        }

        public void UpdateMapObjects(GameTime gameTime)
        {
            // todo: this should probably be part of currentMapStateRepository instead of tracked here
            if (_cachedMap != _currentMapProvider.CurrentMap)
            {
                _ambientSounds = new List<MapCoordinate>(_currentMapProvider.CurrentMap.GetTileSpecs(TileSpec.AmbientSource));
                _cachedMap = _currentMapProvider.CurrentMap;
            }

            var now = DateTime.Now;
            OpenNewDoors(now);
            CloseExpiredDoors(now);

            RemoveStaleSpikeTraps();
            UpdateAmbientNoiseVolume();

            CheckForObjectClicks();
            HideStackedCharacterNames();
        }

        private void OpenNewDoors(DateTime now)
        {
            var newDoors = _currentMapStateRepository.OpenDoors.Where(x => _cachedDoorState.All(d => d.Door != x));
            foreach (var door in newDoors)
            {
                _cachedDoorState.Add(new DoorTimePair { Door = door, OpenTime = now });
                _sfxPlayer.PlaySfx(SoundEffectID.DoorOpen);
            }
        }

        private void CloseExpiredDoors(DateTime now)
        {
            var expiredDoors = _cachedDoorState.Where(x => (now - x.OpenTime).TotalMilliseconds > DOOR_CLOSE_TIME_MS).ToList();
            foreach (var door in expiredDoors)
            {
                _cachedDoorState.Remove(door);
                if (_currentMapStateRepository.OpenDoors.Contains(door.Door))
                {
                    _currentMapStateRepository.OpenDoors.Remove(door.Door);
                    _sfxPlayer.PlaySfx(SoundEffectID.DoorClose);
                }
            }
        }

        private void RemoveStaleSpikeTraps()
        {
            var staleTraps = new List<MapCoordinate>();

            foreach (var spikeTrap in _currentMapStateRepository.VisibleSpikeTraps)
            {
                if (_currentMapStateRepository.Characters.Values
                    .Concat(new[] { _characterProvider.MainCharacter })
                    .Select(x => x.RenderProperties)
                    .All(x => x.MapX != spikeTrap.X && x.MapY != spikeTrap.Y))
                {
                    staleTraps.Add(spikeTrap);
                }
            }

            _currentMapStateRepository.VisibleSpikeTraps.RemoveWhere(staleTraps.Contains);
        }

        private void UpdateAmbientNoiseVolume()
        {
            if (_cachedMap.Properties.AmbientNoise <= 0 || !_configurationProvider.SoundEnabled)
                return;

            // the algorithm in EO main seems to scale volume with distance to the closest ambient source
            // distance is the sum of the components of the vector from character position to closest ambient source
            // this is scaled from 0-25, with 0 being on top of the tile and 25 being too far away to hear the ambient sound from it
            var props = _characterProvider.MainCharacter.RenderProperties;
            var charCoord = props.CurrentAction == CharacterActionState.Walking
                ? new MapCoordinate(props.GetDestinationX(), props.GetDestinationY())
                : new MapCoordinate(props.MapX, props.MapY);
            var shortestDistance = int.MaxValue;
            foreach (var coordinate in _ambientSounds)
            {
                var distance = Math.Abs(charCoord.X - coordinate.X) + Math.Abs(charCoord.Y - coordinate.Y);
                if (distance < shortestDistance)
                    shortestDistance = distance;
            }
            _sfxPlayer.SetLoopingSfxVolume(Math.Max((25 - shortestDistance) / 25f, 0));
        }

        private void CheckForObjectClicks()
        {
            if (_userInputRepository.ClickHandled)
                return;

            var mouseClicked = _userInputRepository.PreviousMouseState.LeftButton == ButtonState.Pressed &&
                _userInputRepository.CurrentMouseState.LeftButton == ButtonState.Released;

            if (mouseClicked)
            {
                foreach (var sign in _cachedMap.Signs)
                {
                    var gfx = _cachedMap.GFX[MapLayer.Objects][sign.Y, sign.X];
                    if (gfx > 0)
                    {
                        var bounds = _mapObjectBoundsCalculator.GetMapObjectBounds(sign.X, sign.Y, gfx);
                        if (bounds.Contains(_userInputRepository.CurrentMouseState.Position))
                        {
                            var cellState = new MapCellState
                            {
                                Sign = Option.Some(new Sign(sign)),
                                Coordinate = new MapCoordinate(sign.X, sign.Y)
                            };
                            _mapInteractionController.LeftClick(cellState, Option.None<IMouseCursorRenderer>());
                            _userInputRepository.ClickHandled = true;
                            break;
                        }
                    }
                }

                // todo: check for board object clicks

                if (_userInputRepository.ClickHandled)
                    _spellSlotDataRepository.SelectedSpellSlot = Option.None<int>();
            }
        }

        private void HideStackedCharacterNames()
        {
            var characters = _characterRendererProvider.CharacterRenderers.Values
                .Where(x => x.MouseOver)
                .GroupBy(x => x.Character.RenderProperties.Coordinates());

            foreach (var grouping in characters)
            {
                if (grouping.Count() > 1)
                {
                    var isFirst = true;
                    foreach (var character in grouping.Reverse())
                    {
                        if (isFirst)
                        {
                            character.ShowName();
                        }
                        else
                        {
                            character.HideName();
                        }

                        isFirst = false;
                    }
                }
                else
                {
                    foreach (var character in grouping)
                        character.ShowName();
                }
            }
        }
    }

    public interface IDynamicMapObjectUpdater
    {
        void UpdateMapObjects(GameTime gameTime);
    }
}
