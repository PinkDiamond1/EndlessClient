﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using System.Collections.Generic;
using EndlessClient.Controllers;
using EndlessClient.Rendering.Factories;
using EOLib.Domain.Login;
using EOLib.Graphics;

namespace EndlessClient.UIControls
{
	public class CharacterInfoPanelFactory : ICharacterInfoPanelFactory
	{
		private readonly ICharacterSelectorProvider _characterProvider;
		private readonly INativeGraphicsManager _nativeGraphicsManager;
		private readonly ILoginControllerProvider _loginControllerProvider;
		private readonly ICharacterManagementControllerProvider _characterManagementControllerProvider;
		private readonly ICharacterRendererFactory _characterRendererFactory;

		public CharacterInfoPanelFactory(ICharacterSelectorProvider characterProvider,
										 INativeGraphicsManager nativeGraphicsManager,
										 ILoginControllerProvider loginControllerProvider,
										 ICharacterManagementControllerProvider characterManagementControllerProvider,
										 ICharacterRendererFactory characterRendererFactory)
		{
			_characterProvider = characterProvider;
			_nativeGraphicsManager = nativeGraphicsManager;
			_loginControllerProvider = loginControllerProvider;
			_characterManagementControllerProvider = characterManagementControllerProvider;
			_characterRendererFactory = characterRendererFactory;
		}

		public IEnumerable<CharacterInfoPanel> CreatePanels()
		{
			int i = 0;
			for (; i < _characterProvider.Characters.Count; ++i)
			{
				var character = _characterProvider.Characters[i];
				yield return new CharacterInfoPanel(i,
													character,
													_nativeGraphicsManager,
													_loginControllerProvider.LoginController,
													_characterManagementControllerProvider.CharacterManagementController,
													_characterRendererFactory);
			}

			for (; i < 3; ++i)
				yield return new EmptyCharacterInfoPanel(i, _nativeGraphicsManager);
		}
	}
}