﻿using AutomaticTypeMapper;
using EndlessClient.Audio;
using EndlessClient.Controllers;
using EndlessClient.ControlSets;
using EndlessClient.Dialogs.Services;
using EndlessClient.GameExecution;
using EndlessClient.HUD;
using EndlessClient.HUD.Inventory;
using EOLib.Domain.Character;
using EOLib.Graphics;
using EOLib.IO.Repositories;

namespace EndlessClient.Dialogs.Factories
{
    [AutoMappedType(IsSingleton = true)]
    public class PaperdollDialogFactory : IPaperdollDialogFactory
    {
        private readonly IGameStateProvider _gameStateProvider;
        private readonly IEODialogButtonService _eoDialogButtonService;
        private readonly IInventorySpaceValidator _inventorySpaceValidator;
        private readonly IEOMessageBoxFactory _eoMessageBoxFactory;
        private readonly IStatusLabelSetter _statusLabelSetter;
        private readonly ISfxPlayer _sfxPlayer;
        private readonly IPaperdollProvider _paperdollProvider;
        private readonly IPubFileProvider _pubFileProvider;
        private readonly IHudControlProvider _hudControlProvider;
        private readonly INativeGraphicsManager _nativeGraphicsManager;
        private IInventoryController _inventoryController;

        public PaperdollDialogFactory(IGameStateProvider gameStateProvider,
            INativeGraphicsManager nativeGraphicsManager,
            IPaperdollProvider paperdollProvider,
            IPubFileProvider pubFileProvider,
            IHudControlProvider hudControlProvider,
            IEODialogButtonService eoDialogButtonService,
            IInventorySpaceValidator inventorySpaceValidator,
            IEOMessageBoxFactory eoMessageBoxFactory,
            IStatusLabelSetter statusLabelSetter,
            ISfxPlayer sfxPlayer)
        {
            _paperdollProvider = paperdollProvider;
            _pubFileProvider = pubFileProvider;
            _hudControlProvider = hudControlProvider;
            _nativeGraphicsManager = nativeGraphicsManager;
            _gameStateProvider = gameStateProvider;
            _eoDialogButtonService = eoDialogButtonService;
            _inventorySpaceValidator = inventorySpaceValidator;
            _eoMessageBoxFactory = eoMessageBoxFactory;
            _statusLabelSetter = statusLabelSetter;
            _sfxPlayer = sfxPlayer;
        }

        public PaperdollDialog Create(Character character, bool isMainCharacter)
        {
            return new PaperdollDialog(_gameStateProvider,
                _nativeGraphicsManager,
                _inventoryController,
                _paperdollProvider,
                _pubFileProvider,
                _hudControlProvider,
                _eoDialogButtonService,
                _inventorySpaceValidator,
                _eoMessageBoxFactory,
                _statusLabelSetter,
                _sfxPlayer,
                character,
                isMainCharacter);
        }

        public void InjectInventoryController(IInventoryController inventoryController)
        {
            _inventoryController = inventoryController;
        }
    }

    public interface IPaperdollDialogFactory
    {
        PaperdollDialog Create(Character character, bool isMainCharacter);

        void InjectInventoryController(IInventoryController inventoryController);
    }
}
