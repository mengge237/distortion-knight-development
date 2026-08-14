using UnityEngine;
using UnityEngine.UI;
using MutationChess.Core;
using System.Collections.Generic;

namespace MutationChess.UI
{
    /// <summary>
    /// 遗物栏UI组件，显示玩家持有的遗物图标
    /// </summary>
    [RequireComponent(typeof(HorizontalLayoutGroup))]
    public class RelicBarUI : MonoBehaviour
    {
        [Header("图标设置")]
        [SerializeField] private Transform iconContainer;
        [SerializeField] private GameObject iconPrefab;

        [Header("Layout")]
        [SerializeField] private float iconSize = 90f;
        [SerializeField] private float spacing = 4f;

        private List<GameObject> spawnedIcons = new List<GameObject>();
        private HorizontalLayoutGroup layoutGroup;

        void Start()
        {
            layoutGroup = GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.childAlignment = TextAnchor.MiddleLeft;
                layoutGroup.spacing = spacing;
                layoutGroup.childControlWidth = false;
                layoutGroup.childControlHeight = false;
                layoutGroup.childForceExpandWidth = false;
                layoutGroup.childForceExpandHeight = false;
            }

            var relicManager = RelicManager.Instance;
            if (relicManager != null)
            {
                relicManager.OnRelicsChanged += Refresh;
                Refresh();
            }
        }

        void OnDestroy()
        {
            var relicManager = RelicManager.Instance;
            if (relicManager != null)
                relicManager.OnRelicsChanged -= Refresh;
        }

        public void Refresh()
        {
            if (iconContainer == null) return;

            foreach (var icon in spawnedIcons)
                Destroy(icon);
            spawnedIcons.Clear();

            var relicManager = RelicManager.Instance;
            if (relicManager == null) return;

            var relics = relicManager.GetAllRelics();
            if (relics.Count == 0) return;

            for (int i = 0; i < relics.Count; i++)
            {
                Relic relic = relics[i];
                if (relic == null) continue;

                GameObject iconObj;
                if (iconPrefab != null)
                {
                    iconObj = Instantiate(iconPrefab, iconContainer, false);
                }
                else
                {
                    iconObj = new GameObject(relic.relicName, typeof(RectTransform), typeof(Image));
                    iconObj.transform.SetParent(iconContainer, false);
                }

                RectTransform rt = iconObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(iconSize, iconSize);
                    rt.localScale = Vector3.one;
                }

                Image img = iconObj.GetComponent<Image>();
                if (img != null)
                {
                    img.preserveAspect = true;
                    if (relic.icon != null)
                        img.sprite = relic.icon;
                }

                spawnedIcons.Add(iconObj);
            }
        }
    }
}


