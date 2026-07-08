using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace MutationChess.Battle
{
    public enum EnemyIntentType
    {
        Attack,
        Defend,
        Special,
        Buff,
        Wait
    }

    public class EnemyIntentUI : MonoBehaviour
    {
        [Header("=== UI组件 ===")]
        [SerializeField] private GameObject intentPanel;
        [SerializeField] private Image intentIcon;
        [SerializeField] private TMP_Text intentText;

        [Header("=== 意图图标 ===")]
        [SerializeField] private Sprite attackIcon;
        [SerializeField] private Sprite defendIcon;
        [SerializeField] private Sprite specialIcon;
        [SerializeField] private Sprite buffIcon;
        [SerializeField] private Sprite waitIcon;

        void Start()
        {
            if (intentPanel != null)
                intentPanel.SetActive(false);
            else
                Debug.LogError("EnemyIntentUI: intentPanel 未设置！");
        }

        public void ShowIntent(EnemyIntentType intent, int value)
        {
            if (intentPanel != null)
            {
                intentPanel.SetActive(true);
            }
            else
            {
                Debug.LogError("intentPanel 为空！无法显示意图");
                return;
            }

            if (intentIcon != null)
            {
                Sprite selectedSprite = GetSpriteForIntent(intent);

                if (selectedSprite != null)
                {
                    intentIcon.sprite = selectedSprite;
                    intentIcon.enabled = true;
                    intentIcon.color = Color.white;
                }
                else
                {
                    intentIcon.enabled = false;
                    Debug.LogWarning($"意图 {intent} 没有配置图标！请检查 Inspector");
                }
            }
            else
            {
                Debug.LogError("intentIcon 为空！");
            }

            if (intentText != null)
            {
                if (intent == EnemyIntentType.Wait)
                {
                    intentText.text = "";  // 等待不显示任何文本
                }
                else
                {
                    intentText.text = value.ToString();
                }
            }
            else
            {
                Debug.LogError("intentText 为空！");
            }
        }

        private Sprite GetSpriteForIntent(EnemyIntentType intent)
        {
            switch (intent)
            {
                case EnemyIntentType.Attack:
                    return attackIcon;
                case EnemyIntentType.Defend:
                    return defendIcon;
                case EnemyIntentType.Special:
                    return specialIcon;
                case EnemyIntentType.Buff:
                    return buffIcon;
                case EnemyIntentType.Wait:
                    return waitIcon;
                default:
                    return null;
            }
        }

        public void HideIntent()
        {
            if (intentPanel != null)
            {
                intentPanel.SetActive(false);
            }
        }

        void OnValidate()
        {
            if (attackIcon == null) Debug.LogWarning("Attack Icon 未设置！");
            if (defendIcon == null) Debug.LogWarning("Defend Icon 未设置！");
            if (specialIcon == null) Debug.LogWarning("Special Icon 未设置！");
            if (buffIcon == null) Debug.LogWarning("Buff Icon 未设置！");
            if (waitIcon == null) Debug.LogWarning("Wait Icon 未设置！");
            if (intentPanel == null) Debug.LogWarning("Intent Panel 未设置！");
            if (intentIcon == null) Debug.LogWarning("Intent Icon 未设置！");
            if (intentText == null) Debug.LogWarning("Intent Text 未设置！");
        }
    }
}