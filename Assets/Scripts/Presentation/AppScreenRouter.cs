using System;
using ShadowGarden.Runtime;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Activates exactly one AppState screen root at a time.
    /// </summary>
    public sealed class AppScreenRouter : MonoBehaviour
    {
        [SerializeField] private GameObject titleRoot;
        [SerializeField] private GameObject openingRoot;
        [SerializeField] private GameObject worldMapRoot;
        [SerializeField] private GameObject gameplayRoot;
        [SerializeField] private GameObject gameOverRoot;
        [SerializeField] private GameObject clearedRoot;
        [SerializeField] private GameObject endingRoot;

        public GameObject TitleRoot => titleRoot;
        public GameObject OpeningRoot => openingRoot;
        public GameObject WorldMapRoot => worldMapRoot;
        public GameObject GameplayRoot => gameplayRoot;
        public GameObject GameOverRoot => gameOverRoot;
        public GameObject ClearedRoot => clearedRoot;
        public GameObject EndingRoot => endingRoot;

        public void Bind(
            GameObject title,
            GameObject opening,
            GameObject worldMap,
            GameObject gameplay,
            GameObject gameOver,
            GameObject cleared,
            GameObject ending)
        {
            titleRoot = title;
            openingRoot = opening;
            worldMapRoot = worldMap;
            gameplayRoot = gameplay;
            gameOverRoot = gameOver;
            clearedRoot = cleared;
            endingRoot = ending;
        }

        public void Show(AppState state)
        {
            SetActiveExclusive(titleRoot, state == AppState.Title);
            SetActiveExclusive(openingRoot, state == AppState.Opening);
            SetActiveExclusive(worldMapRoot, state == AppState.WorldMap);
            SetActiveExclusive(gameplayRoot, state == AppState.Playing);
            SetActiveExclusive(gameOverRoot, state == AppState.GameOver);
            SetActiveExclusive(clearedRoot, state == AppState.Cleared);
            SetActiveExclusive(endingRoot, state == AppState.Ending);
        }

        public int CountActiveRoots()
        {
            var count = 0;
            if (IsActive(titleRoot)) count++;
            if (IsActive(openingRoot)) count++;
            if (IsActive(worldMapRoot)) count++;
            if (IsActive(gameplayRoot)) count++;
            if (IsActive(gameOverRoot)) count++;
            if (IsActive(clearedRoot)) count++;
            if (IsActive(endingRoot)) count++;
            return count;
        }

        private static void SetActiveExclusive(GameObject root, bool active)
        {
            if (root != null && root.activeSelf != active)
            {
                root.SetActive(active);
            }
        }

        private static bool IsActive(GameObject root) => root != null && root.activeSelf;
    }
}
