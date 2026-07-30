using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;  namespace MutationChess.Core
{  /// <summary>  ///  /// Boss+2  /// </summary>  [CreateAssetMenu(fileName = "ExhaustDrawEffect", menuName = "MutationChess/Relic Effects/Exhaust Draw")]  public class ExhaustDrawEffect : CardEffect  {  [Header("")]  [Tooltip("")]  public int drawCount = 1;  public override void Execute(CombatContext context)  {  TryDrawCard(context);  }  public override void Execute(EffectContext context)  {  TryDrawCard(context?.combat);  }  private void TryDrawCard(CombatContext context)  {  if (context == null) return;  int effectiveDraw = drawCount;  if (ConversionModifier.BossCorruptLiverActive)  effectiveDraw = drawCount * 2;  var handManager = HandManager.Instance;  if (handManager != null)  {  handManager.DrawCards(effectiveDraw);  GameLogger.Log($"[ExhaustDraw] ?? {effectiveDraw} {(ConversionModifier.BossCorruptLiverActive ? "??Boss" : "")}");  }  }  }
}  