﻿using EndlessClient.Rendering.Sprites;
using EOLib;
using EOLib.Domain.Character;
using EOLib.Domain.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.Rendering.CharacterProperties
{
    public class FaceRenderer : BaseCharacterPropertyRenderer
    {
        private readonly ISpriteSheet _faceSheet;
        private readonly ISpriteSheet _skinSheet;
        private readonly SkinRenderLocationCalculator _skinRenderLocationCalculator;

        public override bool CanRender => _renderProperties.IsActing(CharacterActionState.Emote) &&
                                          _renderProperties.EmoteFrame > 0 &&
                                          _renderProperties.Emote != Emote.Trade &&
                                          _renderProperties.Emote != Emote.LevelUp &&
                                          _renderProperties.Emote != Emote.MusicNotes;

        public FaceRenderer(CharacterRenderProperties renderProperties,
                            ISpriteSheet faceSheet,
                            ISpriteSheet skinSheet)
            : base(renderProperties)
        {
            _faceSheet = faceSheet;
            _skinSheet = skinSheet;
            _skinRenderLocationCalculator = new SkinRenderLocationCalculator(_renderProperties);
        }

        public override void Render(SpriteBatch spriteBatch, Rectangle parentCharacterDrawArea)
        {
            if (!_renderProperties.IsFacing(EODirection.Down, EODirection.Right))
                return;

            var skinLoc = _skinRenderLocationCalculator.CalculateDrawLocationOfCharacterSkin(_skinSheet.SourceRectangle, parentCharacterDrawArea);
            var facePos = new Vector2(skinLoc.X + (_renderProperties.IsFacing(EODirection.Down) ? 2 : 3),
                                      skinLoc.Y + (_renderProperties.Gender == 0 ? 2 : 0));

            Render(spriteBatch, _faceSheet, facePos);
        }
    }
}
