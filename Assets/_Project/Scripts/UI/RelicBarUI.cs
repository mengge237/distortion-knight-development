using UnityEngine;
using UnityEngine.UI;
using MutationChess.Core;
using System.Collections.Generic;

namespace MutationChess.UI
{
    /// <summary>


    /// </summary>
    public class RelicBarUI : MonoBehaviour
    {
        [Header("===  ===")]
        [SerializeField] private Transform iconContainer;
        [SerializeField] private GameObject iconPrefab;

        [Header("===  ===")]
        [SerializeField] private float iconSize = 90f;
        [SerializeField] private float spacing = 4f;

        private List<GameObject> spawnedIcons = new List<GameObject>();

        void Start()
        {

            if (iconContainer != null)
            {
                var layout = iconContainer.GetComponent<LayoutGroup>();
                if (layout != null) Destroy(layout);
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
                    rt.anchorMin = new Vector2(0, 0.5f);
                    rt.anchorMax = new Vector2(0, 0.5f);
                    rt.pivot = new Vector2(0, 0.5f);
                    rt.sizeDelta = new Vector2(iconSize, iconSize);
                    rt.anchoredPosition = new Vector2(i * (iconSize + spacing), 0);
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


