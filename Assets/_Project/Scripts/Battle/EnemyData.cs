using UnityEngine;

namespace MutationChess.Battle
{
    [System.Serializable]
    public class EnemyData
    {
        public string enemyName;
        public int maxHealth;
        public int attackDamage;
        public EnemyType enemyType;
        public string description;

        [Header("2D")]
        public string spriteName;

        [Header("3D")]
        public GameObject enemyPrefab;
        public Vector3 modelOffset = Vector3.zero;
        public Vector3 modelScale = Vector3.one;
        public float modelRotationY = 0f;

        public string idleAnimationName = "Idle";
        public string attackAnimationName = "Attack";
        public string hurtAnimationName = "Hurt";
        public string deathAnimationName = "Death";

        public string aiPatternName = "";

        public EnemyData() { }

        public EnemyData(string name, int health, int damage, EnemyType type = EnemyType.Normal, string desc = "")
        {
            enemyName = name;
            maxHealth = health;
            attackDamage = damage;
            enemyType = type;
            description = desc;
            spriteName = name;
        }
    }

    public enum EnemyType
    {
        Normal,
        Elite,
        Boss,
        Event
    }
}