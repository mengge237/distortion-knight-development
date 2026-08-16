using UnityEngine;

namespace MutationChess.UI
{
    /// <summary>
    /// 首页场景生成哨兵：HomeSceneSetup 生成场景时挂在 HomeScreen 上，
    /// 用作"该场景已由程序生成、勿自动重建"的判定标记（按脚本 GUID 匹配，改名/重排节点不影响）。
    /// 无任何运行逻辑。
    /// </summary>
    public class HomeSceneMarker : MonoBehaviour
    {
    }
}
