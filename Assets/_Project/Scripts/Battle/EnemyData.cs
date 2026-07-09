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

        [Header("=== 2D显示 ===")]
        public string spriteName;

        [Header("=== 3D模型（可选） ===")]
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

        public Enemy CreateEnemyInstance()
        {
            return new Enemy(this);
        }
    }

    public enum EnemyType
    {
        Normal,
        Elite,
        Boss,
        Event
    }

    [CreateAssetMenu(fileName = "EnemyLibrary", menuName = "MutationChess/Enemy Library")]
    public class EnemyLibrary : ScriptableObject
    {
        public EnemyData[] enemies;

        public EnemyData GetEnemy(string name)
        {
            foreach (var enemy in enemies)
                if (enemy.enemyName == name) return enemy;
            return null;
        }

        public EnemyData GetRandomEnemy(EnemyType type = EnemyType.Normal)
        {
            var filtered = System.Array.FindAll(enemies, e => e.enemyType == type);
            if (filtered.Length == 0) return enemies[0];
            return filtered[Random.Range(0, filtered.Length)];
        }

        public EnemyData GetBoss() => GetRandomEnemy(EnemyType.Boss);
        public EnemyData GetElite() => GetRandomEnemy(EnemyType.Elite);
    }
}