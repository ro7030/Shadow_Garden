using System;
using System.Collections.Generic;

namespace ShadowGarden.Runtime
{
    /// <summary>
    /// Allowed AppState transitions. Invalid transitions are rejected without side effects.
    /// </summary>
    public static class AppStateMachine
    {
        private static readonly Dictionary<AppState, HashSet<AppState>> Allowed =
            new Dictionary<AppState, HashSet<AppState>>
            {
                [AppState.Title] = new HashSet<AppState> { AppState.Opening, AppState.WorldMap },
                [AppState.Opening] = new HashSet<AppState> { AppState.WorldMap },
                [AppState.WorldMap] = new HashSet<AppState> { AppState.Playing, AppState.Title, AppState.Opening },
                [AppState.Playing] = new HashSet<AppState> { AppState.GameOver, AppState.Cleared, AppState.WorldMap },
                [AppState.GameOver] = new HashSet<AppState> { AppState.Playing, AppState.WorldMap },
                [AppState.Cleared] = new HashSet<AppState> { AppState.WorldMap, AppState.Playing, AppState.Ending },
                [AppState.Ending] = new HashSet<AppState> { AppState.Title, AppState.WorldMap }
            };

        public static bool CanTransition(AppState from, AppState to)
        {
            if (from == to)
            {
                return false;
            }

            return Allowed.TryGetValue(from, out var targets) && targets.Contains(to);
        }

        public static bool IsGameplayMapState(AppState state) =>
            state == AppState.Playing;

        public static bool IsUiMapState(AppState state) =>
            state == AppState.Title ||
            state == AppState.Opening ||
            state == AppState.WorldMap ||
            state == AppState.GameOver ||
            state == AppState.Cleared ||
            state == AppState.Ending;

        public static IReadOnlyCollection<AppState> GetAllowedTargets(AppState from)
        {
            if (!Allowed.TryGetValue(from, out var targets))
            {
                return Array.Empty<AppState>();
            }

            return targets;
        }
    }
}
