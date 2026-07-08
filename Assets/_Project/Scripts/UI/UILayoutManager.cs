using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MutationChess.UI
{
    /// <summary>
    /// UI布局管理器 - 纯管理版本
    /// 只负责提供场景中所有TMP_Text的引用，不控制任何UI属性
    /// </summary>
    [ExecuteAlways]
    public class UILayoutManager : MonoBehaviour
    {
        public static UILayoutManager Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        /// <summary>
        /// 获取场景中所有TMP_Text
        /// </summary>
        public TMP_Text[] GetAllTexts()
        {
            return FindObjectsOfType<TMP_Text>(true);
        }

        /// <summary>
        /// 获取场景中所有指定名称的TMP_Text
        /// </summary>
        public TMP_Text FindTextByName(string name)
        {
            TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
            foreach (var text in allTexts)
            {
                if (text.gameObject.name == name)
                    return text;
            }
            return null;
        }

        /// <summary>
        /// 获取场景中所有包含指定名称的TMP_Text
        /// </summary>
        public TMP_Text[] FindTextsContaining(string name)
        {
            TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
            List<TMP_Text> results = new List<TMP_Text>();
            foreach (var text in allTexts)
            {
                if (text.gameObject.name.Contains(name))
                    results.Add(text);
            }
            return results.ToArray();
        }
    }
}