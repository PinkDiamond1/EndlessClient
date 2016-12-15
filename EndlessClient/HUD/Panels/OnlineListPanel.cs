﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using EOLib.Graphics;
using Microsoft.Xna.Framework;
using XNAControls;

namespace EndlessClient.HUD.Panels
{
    public class OnlineListPanel : XNAPanel, IHudPanel
    {
        private readonly INativeGraphicsManager _nativeGraphicsManager;

        public OnlineListPanel(INativeGraphicsManager nativeGraphicsManager)
        {
            _nativeGraphicsManager = nativeGraphicsManager;

            BackgroundImage = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 36);
            DrawArea = new Rectangle(102, 330, BackgroundImage.Width, BackgroundImage.Height);
        }
    }
}