﻿using System;
using EOLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XNAControls;

namespace EndlessClient.Rendering.Chat
{
    //todo: clear message when IHaveChatBubble dies
    public class ChatBubble : IChatBubble
    {
        private readonly IMapActor _parent;
        private readonly IChatBubbleTextureProvider _chatBubbleTextureProvider;

        private readonly XNALabel _textLabel;

        private bool _isGroupChat;
        private Vector2 _drawLocation;
        private DateTime _startTime;

        public bool ShowBubble { get; private set; } = true;

        public ChatBubble(string message,
                          IMapActor parent,
                          IChatBubbleTextureProvider chatBubbleTextureProvider)
            : this(message, false, parent, chatBubbleTextureProvider) { }

        public ChatBubble(string message,
                          bool isGroupChat,
                          IMapActor referenceRenderer,
                          IChatBubbleTextureProvider chatBubbleTextureProvider)
        {
            _isGroupChat = isGroupChat;
            _parent = referenceRenderer;
            _chatBubbleTextureProvider = chatBubbleTextureProvider;

            _textLabel = new XNALabel(Constants.FontSize08pt5)
            {
                Visible = true,
                TextWidth = 150,
                ForeColor = Color.Black,
                AutoSize = true,
                Text = message,
                DrawOrder = 100,
            };
            _textLabel.Initialize();

            if (!_textLabel.Game.Components.Contains(_textLabel))
                _textLabel.Game.Components.Add(_textLabel);

            _drawLocation = Vector2.Zero;
            _startTime = DateTime.Now;

            SetLabelDrawPosition();
        }

        public void SetMessage(string message, bool isGroupChat)
        {
            _isGroupChat = isGroupChat;
            _textLabel.Text = message;
            ShowBubble = true;

            _startTime = DateTime.Now;
        }

        public void Update()
        {
            if (!ShowBubble)
                return;

            SetLabelDrawPosition();
            _drawLocation = _textLabel.DrawPosition - new Vector2(
                _chatBubbleTextureProvider.ChatBubbleTextures[ChatBubbleTexture.TopLeft].Width,
                _chatBubbleTextureProvider.ChatBubbleTextures[ChatBubbleTexture.TopLeft].Height);

            if ((DateTime.Now - _startTime).TotalMilliseconds > Constants.ChatBubbleTimeout)
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
            
            var color = _isGroupChat ? Color.Tan : Color.FromNonPremultiplied(255, 255, 255, 232);

            //top row
            spriteBatch.Draw(TL, _drawLocation, color);
            int xCur;
            for (xCur = xCov; xCur < _textLabel.ActualWidth + 6; xCur += TM.Width)
            {
                spriteBatch.Draw(TM, _drawLocation + new Vector2(xCur, 0), color);
            }
            spriteBatch.Draw(TR, _drawLocation + new Vector2(xCur, 0), color);

            //middle area
            int y;
            for (y = yCov; y < _textLabel.ActualHeight; y += ML.Height)
            {
                spriteBatch.Draw(ML, _drawLocation + new Vector2(0, y), color);
                int x;
                for (x = xCov; x < xCur; x += MM.Width)
                {
                    spriteBatch.Draw(MM, _drawLocation + new Vector2(x, y), color);
                }
                spriteBatch.Draw(MR, _drawLocation + new Vector2(xCur, y), color);
            }

            //bottom row
            spriteBatch.Draw(BL, _drawLocation + new Vector2(0, y), color);
            int x2;
            for (x2 = xCov; x2 < xCur; x2 += BM.Width)
            {
                spriteBatch.Draw(BM, _drawLocation + new Vector2(x2, y), color);
            }
            spriteBatch.Draw(BR, _drawLocation + new Vector2(x2, y), color);

            y += BM.Height;
            spriteBatch.Draw(NUB, _drawLocation + new Vector2((x2 + BR.Width - NUB.Width)/2f, y - 1), color);
        }

        private void SetLabelDrawPosition()
        {
            _textLabel.DrawPosition = new Vector2(
                _parent.DrawArea.X + _parent.DrawArea.Width / 2.0f - _textLabel.ActualWidth / 2.0f,
                _parent.TopPixelWithOffset - _textLabel.ActualHeight - (GetTexture(ChatBubbleTexture.TopMiddle).Height * 5));
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
                _textLabel.Dispose();
            }
        }

        private Texture2D GetTexture(ChatBubbleTexture whichTexture) =>
            _chatBubbleTextureProvider.ChatBubbleTextures[whichTexture];
    }

    public interface IChatBubble : IDisposable
    {
        bool ShowBubble { get; }

        void SetMessage(string message, bool isGroupChat);

        void Update();

        void DrawToSpriteBatch(SpriteBatch spriteBatch);
    }
}
