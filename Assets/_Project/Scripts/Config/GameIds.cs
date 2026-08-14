namespace MutationChess.Core
{
    /// <summary>
    /// 遗物 ID 常量。与 RelicBalanceConfig 中的 relicId 一一对应，
    /// 代码逻辑引用遗物时一律使用本类，禁止散落字符串字面量。
    /// 改名时只需改常量值 + 配置文件中对应条目（RelicBalanceConfig 内部也引用本类）。
    /// </summary>
    public static class RelicIds
    {
        // Boss 遗物（阵营隐藏效果激活器）
        public const string Boss_BloodVein = "Boss_BloodVein";
        public const string Boss_FrostHeart = "Boss_FrostHeart";
        public const string Boss_CorruptLiver = "Boss_CorruptLiver";
        public const string Boss_SlimeGland = "Boss_SlimeGland";
        public const string Boss_ReluctantChain = "Boss_ReluctantChain";
        public const string Boss_MemoryLens = "Boss_MemoryLens";

        // 起始遗物
        public const string Starter_BurningHeart = "Starter_BurningHeart";

        // 鲜血阵营
        public const string Blood_BloodCharm = "Blood_BloodCharm";
        public const string Blood_VampireFang = "Blood_VampireFang";
        public const string Blood_BloodPact = "Blood_BloodPact";
        public const string Blood_CrimsonAltar = "Blood_CrimsonAltar";

        // 寒霜阵营
        public const string Frost_IceCrystal = "Frost_IceCrystal";
        public const string Frost_Permafrost = "Frost_Permafrost";
        public const string Frost_FrostGiant = "Frost_FrostGiant";
        public const string Frost_Snowflake = "Frost_Snowflake";

        // 腐化阵营
        public const string Corrupt_DarkTome = "Corrupt_DarkTome";
        public const string Corrupt_Necronomicon = "Corrupt_Necronomicon";
        public const string Corrupt_DeadBranch = "Corrupt_DeadBranch";

        // 粘液阵营
        public const string Slime_SlimeHeart = "Slime_SlimeHeart";
        public const string Slime_AcidicCore = "Slime_AcidicCore";
        public const string Slime_StickyGlove = "Slime_StickyGlove";

        // 不舍阵营
        public const string Reluctant_EchoRing = "Reluctant_EchoRing";
        public const string Reluctant_Nostalgia = "Reluctant_Nostalgia";

        // 暗影阵营
        public const string Shadow_Cloak = "Shadow_Cloak";
        public const string Shadow_PhantomMask = "Shadow_PhantomMask";
        public const string Shadow_AbyssGaze = "Shadow_AbyssGaze";

        // 通用
        public const string Generic_IronRing = "Generic_IronRing";
        public const string Generic_BronzeShield = "Generic_BronzeShield";
        public const string Generic_LeatherArmor = "Generic_LeatherArmor";
        public const string Generic_WarriorBelt = "Generic_WarriorBelt";
        public const string Generic_SwiftBoots = "Generic_SwiftBoots";
        public const string Generic_PowerPendant = "Generic_PowerPendant";
        public const string Generic_IronWill = "Generic_IronWill";
        public const string Generic_GoldenChalice = "Generic_GoldenChalice";
        public const string Generic_BattleStandard = "Generic_BattleStandard";
        public const string Generic_PhoenixFeather = "Generic_PhoenixFeather";
        public const string Generic_TitanHeart = "Generic_TitanHeart";
        public const string Generic_EternalFlame = "Generic_EternalFlame";
        public const string Generic_PiggyBank = "Generic_PiggyBank";
        public const string Generic_VictorySword = "Generic_VictorySword";

        // 组合
        public const string Combo_ResonanceStone = "Combo_ResonanceStone";
        public const string Combo_ChessMaster = "Combo_ChessMaster";

        // 商店专属
        public const string Shop_EnergyCore = "Shop_EnergyCore";
        public const string Shop_DrawingPad = "Shop_DrawingPad";
        public const string Shop_TreasureChest = "Shop_TreasureChest";
        public const string Shop_GoldenIdol = "Shop_GoldenIdol";
        public const string Shop_RestockTalisman = "Shop_RestockTalisman";

        // 合成
        public const string Synth_SwordShard = "Synth_SwordShard";
        public const string Synth_HiltShard = "Synth_HiltShard";
        public const string Synth_SwordCore = "Synth_SwordCore";
    }

    /// <summary>
    /// 固有效果资产名常量（Resources/InherentEffects 下的文件名，不含扩展名）。
    /// </summary>
    public static class EffectIds
    {
        public const string SlimeInherentEffect = "SlimeInherentEffect";
        public const string ReluctantInherentEffect = "ReluctantInherentEffect";
    }

    /// <summary>
    /// Resources 加载路径常量。所有 Resources.Load/LoadAll 一律引用本类，
    /// 资源目录调整时只需改这里。
    /// </summary>
    public static class ResourcePaths
    {
        // 资源文件夹
        public const string Cards = "Cards";
        public const string Effects = "Effects";
        public const string InherentEffects = "InherentEffects";
        public const string Relics = "Relics";
        public const string RelicsArt = "RelicsArt";
        public const string Potions = "Potions";
        public const string EnemySprites = "EnemySprites";
        public const string MapTextures = "MapTextures";
        public const string PlayerSprites_Player = "PlayerSprites/Player";
        public const string Player_player = "Player/player";

        // 配置资产（Resources 根或 Resources/Config 下）
        public const string GameConfig = "GameConfig";
        public const string MapConfig = "MapConfig";
        public const string BossRewardConfig = "BossRewardConfig";
        public const string ShopConfig = "ShopConfig";
    }
}
