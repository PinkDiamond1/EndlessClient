﻿using EndlessClient.GameExecution;
using EndlessClient.Rendering.Character;
using EndlessClient.Rendering.Effects;
using EndlessClient.Rendering.Factories;
using EndlessClient.Rendering.MapEntityRenderers;
using EndlessClient.Rendering.NPC;
using EOLib.Config;
using EOLib.Domain.Character;
using EOLib.Domain.Map;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Optional;
using System;
using System.Collections.Generic;

namespace EndlessClient.Rendering.Map
{
    public class MapRenderer : DrawableGameComponent, IMapRenderer
    {
        private const double TRANSITION_TIME_MS = 125.0;

        private readonly IRenderTargetFactory _renderTargetFactory;
        private readonly IEffectRendererFactory _effectRendererFactory;
        private readonly IMapEntityRendererProvider _mapEntityRendererProvider;
        private readonly ICharacterProvider _characterProvider;
        private readonly ICurrentMapProvider _currentMapProvider;
        private readonly IMapRenderDistanceCalculator _mapRenderDistanceCalculator;
        private readonly ICharacterRendererUpdater _characterRendererUpdater;
        private readonly INPCRendererUpdater _npcRendererUpdater;
        private readonly IDynamicMapObjectUpdater _dynamicMapObjectUpdater;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IMouseCursorRenderer _mouseCursorRenderer;
        private readonly IGridDrawCoordinateCalculator _gridDrawCoordinateCalculator;
        private readonly IFixedTimeStepRepository _fixedTimeStepRepository;
        private RenderTarget2D _mapBaseTarget, _mapObjectTarget;
        private SpriteBatch _sb;
        private MapTransitionState _mapTransitionState = MapTransitionState.Default;
        private int? _lastMapChecksum;
        private bool _groundDrawn;

        private Option<MapQuakeState> _quakeState;

        private IDictionary<MapCoordinate, IEffectRenderer> _mapGridEffectRenderers;

        public bool MouseOver
        {
            get
            {
                var ms = Mouse.GetState();
                //todo: turn magic numbers into meaningful values
                return Game.IsActive && ms.X > 0 && ms.Y > 0 && ms.X < 640 && ms.Y < 320;
            }
        }

        public MapCoordinate GridCoordinates => _mouseCursorRenderer.GridCoordinates;

        public MapRenderer(IEndlessGame endlessGame,
                           IRenderTargetFactory renderTargetFactory,
                           IEffectRendererFactory effectRendererFactory,
                           IMapEntityRendererProvider mapEntityRendererProvider,
                           ICharacterProvider characterProvider,
                           ICurrentMapProvider currentMapProvider,
                           IMapRenderDistanceCalculator mapRenderDistanceCalculator,
                           ICharacterRendererUpdater characterRendererUpdater,
                           INPCRendererUpdater npcRendererUpdater,
                           IDynamicMapObjectUpdater dynamicMapObjectUpdater,
                           IConfigurationProvider configurationProvider,
                           IMouseCursorRenderer mouseCursorRenderer,
                           IGridDrawCoordinateCalculator gridDrawCoordinateCalculator,
                           IFixedTimeStepRepository fixedTimeStepRepository)
            : base((Game)endlessGame)
        {
            _renderTargetFactory = renderTargetFactory;
            _effectRendererFactory = effectRendererFactory;
            _mapEntityRendererProvider = mapEntityRendererProvider;
            _characterProvider = characterProvider;
            _currentMapProvider = currentMapProvider;
            _mapRenderDistanceCalculator = mapRenderDistanceCalculator;
            _characterRendererUpdater = characterRendererUpdater;
            _npcRendererUpdater = npcRendererUpdater;
            _dynamicMapObjectUpdater = dynamicMapObjectUpdater;
            _configurationProvider = configurationProvider;
            _mouseCursorRenderer = mouseCursorRenderer;
            _gridDrawCoordinateCalculator = gridDrawCoordinateCalculator;
            _fixedTimeStepRepository = fixedTimeStepRepository;
            _mapGridEffectRenderers = new Dictionary<MapCoordinate, IEffectRenderer>();
        }

        public override void Initialize()
        {
            _mapBaseTarget = _renderTargetFactory.CreateRenderTarget();
            _mapObjectTarget = _renderTargetFactory.CreateRenderTarget();
            _sb = new SpriteBatch(Game.GraphicsDevice);

            _mouseCursorRenderer.Initialize();

            DrawOrder = -10;

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            if (!_lastMapChecksum.HasValue || _lastMapChecksum != _currentMapProvider.CurrentMap.Properties.ChecksumInt)
            {
                // The dimensions of the map are 0-based in the properties. Adjust to 1-based for RT creation
                var widthPlus1 = _currentMapProvider.CurrentMap.Properties.Width + 1;
                var heightPlus1 = _currentMapProvider.CurrentMap.Properties.Height + 1;

                _mapBaseTarget.Dispose();
                _mapBaseTarget = _renderTargetFactory.CreateRenderTarget(
                    (widthPlus1 + heightPlus1) * 32,
                    (widthPlus1 + heightPlus1) * 16);
                _groundDrawn = false;
            }

            if (Visible)
            {
                _characterRendererUpdater.UpdateCharacters(gameTime);
                _npcRendererUpdater.UpdateNPCs(gameTime);
                _dynamicMapObjectUpdater.UpdateMapObjects(gameTime);

                if (MouseOver)
                    _mouseCursorRenderer.Update(gameTime);

                UpdateQuakeState();

                foreach (var target in _mapGridEffectRenderers.Values)
                    target.Update();

                if (_fixedTimeStepRepository.IsWalkUpdateFrame || _mapTransitionState.StartTime.HasValue)
                {
                    DrawGroundLayerToRenderTarget();

                    if (_fixedTimeStepRepository.IsWalkUpdateFrame)
                    {
                        DrawMapToRenderTarget();
                    }
                }
            }

            _lastMapChecksum = _currentMapProvider.CurrentMap.Properties.ChecksumInt;

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible)
                return;

            DrawToSpriteBatch(_sb, gameTime);

            base.Draw(gameTime);
        }

        public void StartMapTransition()
        {
            _mapTransitionState = new MapTransitionState(Option.Some(DateTime.Now), 1);
        }

        public void StartEarthquake(byte strength)
        {
            _quakeState = Option.Some(new MapQuakeState(strength));
        }

        public void RedrawGroundLayer()
        {
            _lastMapChecksum = null;
            _mapTransitionState = new MapTransitionState(Option.Some(DateTime.Now - new TimeSpan(0, 5, 0)), 255);
        }

        public void RenderEffect(byte x, byte y, short effectId)
        {
            var coordinate = new MapCoordinate(x, y);

            if (!_mapGridEffectRenderers.ContainsKey(coordinate))
            {
                var renderer = _effectRendererFactory.Create();
                _mapGridEffectRenderers[coordinate] = renderer;
            }

            if (_mapGridEffectRenderers[coordinate].State == EffectState.Stopped)
            {
                _mapGridEffectRenderers[coordinate].PlayEffect(effectId + 1, coordinate);
            }
            else
            {
                _mapGridEffectRenderers[coordinate].QueueEffect(effectId + 1, coordinate);
            }
        }

        public void ClearTransientRenderables()
        {
            _mapGridEffectRenderers.Clear();
            _mouseCursorRenderer.ClearTransientRenderables();
        }

        private void UpdateQuakeState()
        {
            // when quake:
            // 1. determine offset target
            // 2. incrementally make offset approach closer to the target offset
            // 3. when offset target reached, determine new target (random based on magnitude)
            // 4. flip direction
            // 5. keep going until specific number of "direction flips" has elapsed

            _quakeState.MatchSome(q =>
            {
                var next = q.NextOffset();

                if (next.OffsetReached)
                    next = next.NextState();

                _quakeState = next.Done
                    ? Option.None<MapQuakeState>()
                    : Option.Some(next);
            });
        }

        private void DrawGroundLayerToRenderTarget()
        {
            if (_groundDrawn && (!_mapTransitionState.StartTime.HasValue && _lastMapChecksum == _currentMapProvider.CurrentMap.Properties.ChecksumInt))
                return;

            _groundDrawn = true;

            GraphicsDevice.SetRenderTarget(_mapBaseTarget);
            _sb.Begin();

            var renderBounds = new MapRenderBounds(0, _currentMapProvider.CurrentMap.Properties.Height,
                                                   0, _currentMapProvider.CurrentMap.Properties.Width);

            var transitionComplete = true;
            for (var row = renderBounds.FirstRow; row <= renderBounds.LastRow; row++)
            {
                for (var col = renderBounds.FirstCol; col <= renderBounds.LastCol; ++col)
                {
                    var alpha = GetAlphaForCoordinates(col, row, _characterProvider.MainCharacter);
                    transitionComplete &= alpha == 255;

                    if (_mapEntityRendererProvider.GroundRenderer.CanRender(row, col))
                        _mapEntityRendererProvider.GroundRenderer.RenderElementAt(_sb, row, col, alpha);
                }
            }

            if (transitionComplete)
                _mapTransitionState = new MapTransitionState(Option.None<DateTime>(), 0);

            _sb.End();
            GraphicsDevice.SetRenderTarget(null);
        }

        private void DrawMapToRenderTarget()
        {
            var immutableCharacter = _characterProvider.MainCharacter;

            GraphicsDevice.SetRenderTarget(_mapObjectTarget);
            GraphicsDevice.Clear(ClearOptions.Target, Color.Transparent, 0, 0);

            var gfxToRenderLast = new SortedList<MapRenderLayer, List<(MapCoordinate Coord, IMapEntityRenderer Renderer)>>();

            _sb.Begin();

            var renderBounds = _mapRenderDistanceCalculator.CalculateRenderBounds(immutableCharacter, _currentMapProvider.CurrentMap);

            var hitKeys = new HashSet<MapCoordinate>();

            // render the grid diagonally. hack that fixes some layering issues due to not using a depth buffer for layers
            // a better solution would be to use a depth buffer like eomap-js
            for (var rowStart = renderBounds.FirstRow; rowStart <= renderBounds.LastRow; rowStart++)
            {
                var row = rowStart;
                var col = renderBounds.FirstCol;

                if (!hitKeys.Add(new MapCoordinate(col, row)))
                    continue;

                while (row >= 0)
                {
                    RenderGridSpace(row, col);

                    row--;
                    col++;
                }
            }

            for (var colStart = renderBounds.FirstCol; colStart <= renderBounds.LastCol; colStart++)
            {
                var row = renderBounds.LastRow;
                var col = colStart;

                if (!hitKeys.Add(new MapCoordinate(col, row)))
                    continue;

                while (col <= renderBounds.LastCol)
                {
                    RenderGridSpace(row, col);
                    row--;
                    col++;
                }
            }

            foreach (var kvp in gfxToRenderLast)
            {
                foreach (var (pointKey, renderer) in kvp.Value)
                {
                    var alpha = GetAlphaForCoordinates(pointKey.X, pointKey.Y, immutableCharacter);
                    renderer.RenderElementAt(_sb, pointKey.Y, pointKey.X, alpha);
                }
            }

            _sb.End();
            GraphicsDevice.SetRenderTarget(null);

            void RenderGridSpace(int row, int col)
            {
                var alpha = GetAlphaForCoordinates(col, row, immutableCharacter);

                foreach (var renderer in _mapEntityRendererProvider.MapEntityRenderers)
                {
                    if (!renderer.CanRender(row, col))
                        continue;

                    if (renderer.ShouldRenderLast)
                    {
                        var renderLaterKey = new MapCoordinate(col, row);
                        if (gfxToRenderLast.ContainsKey(renderer.RenderLayer))
                            gfxToRenderLast[renderer.RenderLayer].Add((renderLaterKey, renderer));
                        else
                            gfxToRenderLast.Add(renderer.RenderLayer, new List<(MapCoordinate, IMapEntityRenderer)> { (renderLaterKey, renderer) });
                    }
                    else
                        renderer.RenderElementAt(_sb, row, col, alpha);
                }
            }
        }

        private void DrawToSpriteBatch(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Begin();

            var drawLoc = _gridDrawCoordinateCalculator.CalculateGroundLayerDrawCoordinatesFromGridUnits();
            var offset = _quakeState.Map(qs => qs.Offset).Match(some: o => o, none: () => 0);

            spriteBatch.Draw(_mapBaseTarget, drawLoc + new Vector2(offset, 0), Color.White);
            DrawBaseLayers(spriteBatch);

            _mouseCursorRenderer.Draw(spriteBatch, new Vector2(offset, 0));

            spriteBatch.Draw(_mapObjectTarget, new Vector2(offset, 0), Color.White);

            foreach (var target in _mapGridEffectRenderers.Values)
            {
                target.DrawBehindTarget(spriteBatch);
                target.DrawInFrontOfTarget(spriteBatch);
            }

            spriteBatch.End();
        }

        private void DrawBaseLayers(SpriteBatch spriteBatch)
        {
            var offset = _quakeState.Map(qs => qs.Offset).Match(some: o => o, none: () => 0);

            var renderBounds = _mapRenderDistanceCalculator.CalculateRenderBounds(_characterProvider.MainCharacter, _currentMapProvider.CurrentMap);

            for (var row = renderBounds.FirstRow; row <= renderBounds.LastRow; row++)
            {
                for (var col = renderBounds.FirstCol; col <= renderBounds.LastCol; ++col)
                {
                    var alpha = GetAlphaForCoordinates(col, row, _characterProvider.MainCharacter);

                    foreach (var renderer in _mapEntityRendererProvider.BaseRenderers)
                    {
                        if (renderer.CanRender(row, col))
                            renderer.RenderElementAt(spriteBatch, row, col, alpha, new Vector2(offset, 0));
                    }
                }
            }
        }

        private int GetAlphaForCoordinates(int objX, int objY, EOLib.Domain.Character.Character character)
        {
            if (!_configurationProvider.ShowTransition)
            {
                _mapTransitionState = new MapTransitionState(Option.None<DateTime>(), 0);
                return 255;
            }

            //get the farther away of X or Y coordinate for the map object
            var metric = Math.Max(Math.Abs(objX - character.RenderProperties.MapX),
                                  Math.Abs(objY - character.RenderProperties.MapY));

            int alpha = 0;
            if (!_mapTransitionState.StartTime.HasValue ||
                metric < _mapTransitionState.TransitionMetric ||
                _mapTransitionState.TransitionMetric == 0)
            {
                alpha = 255;
            }
            else if (metric == _mapTransitionState.TransitionMetric)
            {
                _mapTransitionState.StartTime
                    .MatchSome(startTime =>
                    {
                        var ms = (DateTime.Now - startTime).TotalMilliseconds;
                        alpha = (int)Math.Round(ms / TRANSITION_TIME_MS * 255);

                        if (ms / TRANSITION_TIME_MS >= 1)
                            _mapTransitionState = new MapTransitionState(Option.Some(DateTime.Now), _mapTransitionState.TransitionMetric + 1);
                    });
            }
            else
                alpha = 0;

            return alpha;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _mapBaseTarget.Dispose();
                _mapObjectTarget.Dispose();
                _sb.Dispose();
                _mouseCursorRenderer.Dispose();

                _npcRendererUpdater.Dispose();
                _characterRendererUpdater.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    internal struct MapTransitionState
    {
        internal static MapTransitionState Default => new MapTransitionState(Option.None<DateTime>(), 0);

        internal Option<DateTime> StartTime { get; }

        internal int TransitionMetric { get; }

        internal MapTransitionState(Option<DateTime> startTime, int transitionMetric)
            : this()
        {
            StartTime = startTime;
            TransitionMetric = transitionMetric;
        }
    }

    internal struct MapQuakeState
    {
        private static readonly Random _random = new Random();

        internal static MapQuakeState Default => new MapQuakeState();

        internal int Magnitude { get; }

        internal float Offset { get; }

        internal float OffsetTarget { get; }

        internal bool OffsetReached => Math.Abs(Offset) >= Math.Abs(OffsetTarget);

        internal int Flips { get; }

        internal int FlipsMax => Magnitude == 0 ? 0 : 10 + Magnitude * 2;

        internal bool Done => Flips >= FlipsMax;

        internal MapQuakeState(int magnitude)
            : this(magnitude, 0, 0) { }

        private MapQuakeState(int magnitude, float offset, int flips)
            : this(magnitude, offset, NewOffsetTarget(magnitude), flips) { }

        private MapQuakeState(int magnitude, float offset, float offsetTarget, int flips)
        {
            Magnitude = magnitude;
            Offset = offset;
            OffsetTarget = offsetTarget;
            Flips = flips;
        }

        internal MapQuakeState NextOffset()
        {
            var nextOffset = Offset + OffsetTarget / 4f;
            return new MapQuakeState(Magnitude, nextOffset, OffsetTarget, Flips);
        }

        internal MapQuakeState NextState()
        {
            var flip = -OffsetTarget / Math.Abs(OffsetTarget);
            var offset = OffsetTarget + 1*flip;
            var nextOffsetTarget = NewOffsetTarget(Magnitude) * flip;

            return new MapQuakeState(Magnitude, offset, nextOffsetTarget, Flips + 1);
        }

        private static float NewOffsetTarget(int magnitude) => 16 + 3 * _random.Next(0, (int)(magnitude * 1.5));
    }
}