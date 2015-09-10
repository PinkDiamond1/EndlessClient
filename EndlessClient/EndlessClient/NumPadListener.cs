﻿using System;
using System.Linq;
using EOLib.Net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EndlessClient
{
	public class NumPadListener : InputKeyListenerBase
	{
		public NumPadListener()
		{
			if (Game.Components.Any(x => x is NumPadListener))
				throw new InvalidOperationException("The game already contains an arrow key listener");
			Game.Components.Add(this);
		}

		public override void Update(GameTime gameTime)
		{
			if (!IgnoreInput && Character.State == CharacterActionState.Standing)
			{
				UpdateInputTime();

				bool handledPress = false;
				for (int key = (int) Keys.NumPad0; key <= (int) Keys.NumPad9; ++key)
				{
					if (IsKeyPressed((Keys) key))
					{
						var emote = key == (int)Keys.NumPad0 ? Emote.Playful : (Emote) (key - (int) Keys.NumPad0);
						_doEmote(emote);
						handledPress = true;
						break;
					} 
				}

				//The Decimal enumeration is 110, which is the Virtual Key code (VK_XXXX) for the 'del'/'.' key on the numpad
				if (!handledPress && IsKeyPressed(Keys.Decimal))
				{
					_doEmote(Emote.Embarassed);
				}
			}

			base.Update(gameTime);
		}

		private void _doEmote(Emote emote)
		{
			Character.Emote(emote);
			Renderer.PlayerEmote();
		}
	}
}
