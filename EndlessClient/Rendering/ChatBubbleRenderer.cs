﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using System;
using EndlessClient.Rendering.Chat;
using EOLib;
using EOLib.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XNAControls;

namespace EndlessClient.Rendering
{
    //todo: handle group chat color
    public class ChatBubble : IChatBubble
    {
        private readonly IHaveChatBubble _referenceRenderer;
        private readonly IChatBubbleTextureProvider _chatBubbleTextureProvider;

        private readonly XNALabel _textLabel;
        private readonly SpriteBatch _spriteBatch;

        private Vector2 _drawLocation;
        private Optional<DateTime> _startTime;

        private bool ShowBubble { get; set; }

        public ChatBubble(IHaveChatBubble referenceRenderer,
                          IChatBubbleTextureProvider chatBubbleTextureProvider,
                          IGraphicsDeviceProvider graphicsDeviceProvider)
        {
            _referenceRenderer = referenceRenderer;
            _chatBubbleTextureProvider = chatBubbleTextureProvider;
            _spriteBatch = new SpriteBatch(graphicsDeviceProvider.GraphicsDevice);

            _textLabel = new XNALabel(Constants.FontSize08pt5)
            {
                Visible = true,
                TextWidth = 165,
                TextAlign = LabelAlignment.MiddleCenter,
                ForeColor = Color.Black,
                AutoSize = true,
                Text = ""
            };
            _textLabel.Initialize(); //todo: figure out if this needs to be removed from game components

            _drawLocation = Vector2.Zero;
            _startTime = Optional<DateTime>.Empty;

            _setLabelDrawLoc();
        }

        public void HideBubble()
        {
            _textLabel.Text = "";
            _textLabel.Visible = false;
            ShowBubble = false;
        }

        public void SetMessage(string message)
        {
            _textLabel.Text = message;
            _textLabel.Visible = true;
            ShowBubble = true;

            _startTime = DateTime.Now;
        }

        private void _setLabelDrawLoc()
        {
            var extra = _chatBubbleTextureProvider.ChatBubbleTextures[ChatBubbleTexture.MiddleLeft].Width;
            _textLabel.DrawPosition = new Vector2(
                _referenceRenderer.DrawArea.X + _referenceRenderer.DrawArea.Width/2.0f - _textLabel.ActualWidth/2.0f + extra,
                _referenceRenderer.DrawArea.Y - _textLabel.ActualHeight - 5);
        }

        public void Update()
        {
            if (!ShowBubble)
                return;

            _setLabelDrawLoc();
            _drawLocation = _textLabel.DrawPosition - new Vector2(
                _chatBubbleTextureProvider.ChatBubbleTextures[ChatBubbleTexture.TopLeft].Width,
                _chatBubbleTextureProvider.ChatBubbleTextures[ChatBubbleTexture.TopLeft].Height);

            if (_startTime.HasValue && (DateTime.Now - _startTime).TotalMilliseconds > Constants.ChatBubbleTimeout)
            {
                ShowBubble = false;
                _textLabel.Visible = false;
                _startTime = Optional<DateTime>.Empty;
            }
        }

        public void DrawToSpriteBatch(SpriteBatch spriteBatch)
        {
            if (!ShowBubble)
                return;

            var TL = GetTexture(ChatBubbleTexture.TopLeft);
            var TM = GetTexture(ChatBubbleTexture.TopMiddle);
            var TR = GetTexture(ChatBubbleTexture.TopRight);
            var ML = GetTexture(ChatBubbleTexture.MiddleLeft);
            var MM = GetTexture(ChatBubbleTexture.MiddleMiddle);
            var MR = GetTexture(ChatBubbleTexture.MiddleRight);
            var BL = GetTexture(ChatBubbleTexture.BottomLeft);
            var BM = GetTexture(ChatBubbleTexture.BottomMiddle);
            var BR = GetTexture(ChatBubbleTexture.BottomRight);
            var NUB = GetTexture(ChatBubbleTexture.Nubbin);

            var xCov = TL.Width;
            var yCov = TL.Height;

            //todo: use group chat color for group chats
            var color = /*m_useGroupChatColor ? Color.Tan : */ Color.FromNonPremultiplied(255, 255, 255, 232);

            //top row
            _spriteBatch.Draw(TL, _drawLocation, color);
            int xCur;
            for (xCur = xCov; xCur < _textLabel.ActualWidth + 6; xCur += TM.Width)
            {
                _spriteBatch.Draw(TM, _drawLocation + new Vector2(xCur, 0), color);
            }
            _spriteBatch.Draw(TR, _drawLocation + new Vector2(xCur, 0), color);

            //middle area
            int y;
            for (y = yCov; y < _textLabel.ActualHeight; y += ML.Height)
            {
                _spriteBatch.Draw(ML, _drawLocation + new Vector2(0, y), color);
                int x;
                for (x = xCov; x < xCur; x += MM.Width)
                {
                    _spriteBatch.Draw(MM, _drawLocation + new Vector2(x, y), color);
                }
                _spriteBatch.Draw(MR, _drawLocation + new Vector2(xCur, y), color);
            }

            //bottom row
            _spriteBatch.Draw(BL, _drawLocation + new Vector2(0, y), color);
            int x2;
            for (x2 = xCov; x2 < xCur; x2 += BM.Width)
            {
                _spriteBatch.Draw(BM, _drawLocation + new Vector2(x2, y), color);
            }
            _spriteBatch.Draw(BR, _drawLocation + new Vector2(x2, y), color);

            y += BM.Height;
            _spriteBatch.Draw(NUB, _drawLocation + new Vector2((x2 + BR.Width - NUB.Width)/2f, y - 1), color);
        }

        ~ChatBubble()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _spriteBatch.Dispose();
                _textLabel.Dispose();
            }
        }

        private Texture2D GetTexture(ChatBubbleTexture whichTexture) =>
            _chatBubbleTextureProvider.ChatBubbleTextures[whichTexture];
    }

    public interface IChatBubble : IDisposable
    {
        void SetMessage(string message);

        void HideBubble();

        void Update();

        void DrawToSpriteBatch(SpriteBatch spriteBatch);
    }
}
