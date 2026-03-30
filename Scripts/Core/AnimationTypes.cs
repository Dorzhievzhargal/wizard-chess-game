namespace WizardChess.Core
{
    /// <summary>
    /// Animation states for chess piece animations.
    /// </summary>
    public enum AnimationState
    {
        Idle,
        Move,
        Attack,
        Hit_Reaction,
        Death
    }

    /// <summary>
    /// Configuration for battle animations.
    /// </summary>
    public struct BattleConfig
    {
        public float MinDuration; // 1.5 sec
        public float MaxDuration; // 3.0 sec

        public BattleConfig(float minDuration, float maxDuration)
        {
            MinDuration = minDuration;
            MaxDuration = maxDuration;
        }
    }

    /// <summary>
    /// Defines the attack style for a piece type during battle.
    /// </summary>
    public struct AttackStyle
    {
        public PieceType AttackerType;
        public string AnimationName;
        public string Description;

        public AttackStyle(PieceType attackerType, string animationName, string description)
        {
            AttackerType = attackerType;
            AnimationName = animationName;
            Description = description;
        }
    }
}
