﻿using EndlessClient.GameExecution;
using EOLib.Domain.Map;
using Microsoft.Xna.Framework.Input;
using Optional;
using System;
using System.Linq;

namespace EndlessClient.Input
{
    public abstract class InputHandlerBase : IInputHandler
    {
        private const int INPUT_RATE_LIMIT_MILLISECONDS = 200;
        private const int WARP_BACKOFF_TIME_MILLISECONDS = 300;

        private readonly IEndlessGameProvider _endlessGameProvider;
        private readonly IUserInputProvider _keyStateProvider;
        private readonly IUserInputTimeRepository _userInputTimeRepository;
        private readonly ICurrentMapStateRepository _currentMapStateRepository;

        private KeyboardState CurrentState => _keyStateProvider.CurrentKeyState;

        private KeyboardState PreviousState => _keyStateProvider.PreviousKeyState;

        protected InputHandlerBase(IEndlessGameProvider endlessGameProvider,
                                   IUserInputProvider userInputProvider,
                                   IUserInputTimeRepository userInputTimeRepository,
                                   ICurrentMapStateRepository currentMapStateRepository)
        {
            _endlessGameProvider = endlessGameProvider;
            _keyStateProvider = userInputProvider;
            _userInputTimeRepository = userInputTimeRepository;
            _currentMapStateRepository = currentMapStateRepository;
        }

        public void HandleKeyboardInput(DateTime timeAtBeginningOfUpdate)
        {
            var millisecondsSinceLastUpdate = GetMillisecondsSinceLastUpdate(timeAtBeginningOfUpdate);
            if (!_endlessGameProvider.Game.IsActive ||
                millisecondsSinceLastUpdate < INPUT_RATE_LIMIT_MILLISECONDS ||
                _currentMapStateRepository.MapWarpState != WarpState.None)
                return;

            _currentMapStateRepository.MapWarpTime = _currentMapStateRepository.MapWarpTime.Match(
                some: t => (DateTime.Now - t).TotalMilliseconds > WARP_BACKOFF_TIME_MILLISECONDS ? Option.None<DateTime>() : Option.Some(t),
                none: () => Option.None<DateTime>());

            _currentMapStateRepository.MapWarpTime.MatchNone(() => HandleInput().MatchSome(_ => _userInputTimeRepository.LastInputTime = timeAtBeginningOfUpdate));
        }

        private double GetMillisecondsSinceLastUpdate(DateTime timeAtBeginningOfUpdate)
        {
            return (timeAtBeginningOfUpdate - _userInputTimeRepository.LastInputTime).TotalMilliseconds;
        }

        protected abstract Option<Keys> HandleInput();

        protected bool IsKeyHeld(params Keys[] keys)
        {
            return keys.Any(key => CurrentState.IsKeyHeld(PreviousState, key));
        }

        protected bool IsKeyPressedOnce(params Keys[] keys)
        {
            return keys.Any(key => CurrentState.IsKeyPressedOnce(PreviousState, key));
        }
    }

    public interface IInputHandler
    {
        void HandleKeyboardInput(DateTime timeAtStart);
    }
}