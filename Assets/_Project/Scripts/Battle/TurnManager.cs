using System;
using UnityEngine;

namespace MutationChess.Battle
{
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        public event Action OnPlayerTurnStart;
        public event Action OnPlayerTurnEnd;
        public event Action OnEnemyTurnStart;
        public event Action OnEnemyTurnEnd;

        private bool isPlayerTurn = true;
        private bool isBattleActive = false;

        public bool IsPlayerTurn => isPlayerTurn;
        public bool IsBattleActive => isBattleActive;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void StartBattle()
        {
            isBattleActive = true;
            isPlayerTurn = true;

            OnPlayerTurnStart?.Invoke();
        }

        public void EndPlayerTurn()
        {
            if (!isPlayerTurn || !isBattleActive)
            {
                return;
            }

            isPlayerTurn = false;
            OnPlayerTurnEnd?.Invoke();

            OnEnemyTurnStart?.Invoke();
        }

        public void EndEnemyTurn()
        {
            if (isPlayerTurn || !isBattleActive)
            {
                return;
            }

            OnEnemyTurnEnd?.Invoke();

            StartPlayerTurn();
        }

        public void StartPlayerTurn()
        {
            if (!isBattleActive)
            {
                return;
            }

            isPlayerTurn = true;
            OnPlayerTurnStart?.Invoke();
        }

        public void EndBattle()
        {
            isBattleActive = false;
            isPlayerTurn = false;
        }

        public void ResetTurnState()
        {
            isPlayerTurn = true;
            isBattleActive = false;
        }
    }
}