namespace MiTschMR.Skills
{
	using GameCreator.Core;
	using GameCreator.Localization;
	using UnityEngine;
	using UnityEngine.Events;

    public class SkillAsset
    {
		// PROPERTIES: -------------------------------------------------------------------------------------------------

		public int uuid = -1;
		public bool isExecuting = false;
		public bool isInCooldown = false;
		public float cooldownTimeLeft = 0;
		public bool addedToSkillBar = false;
		public int addedToSkillBarIndex = -1;
		[LocStringNoPostProcess] public LocString skillName = new LocString("");
		[LocStringNoPostProcess] public LocString skillDescription = new LocString();

		public Sprite icon;

		[SkillsSingleSkillType] public int skillType = 0;

		public Skill.SkillExecutionType skillExecutionType = Skill.SkillExecutionType.InstantExecute;
		public float castTime = 1;
		public float executionTime = 1;
		public float cooldownTime = 1;

		public Skill.SkillState skillState = Skill.SkillState.Locked;
		public bool useSkillPoints = true;
		public float skillPointsNeeded = 1;

		public bool requiresLevel = false;
		public float level = 0;

		public SkillHolderSerialized[] reliesOnSkills;

		public bool assignableToSkillBar = false;

		public IConditionsList conditionsExecutionRequirements;

		public IActionsList actionsOnActivate;
		public IActionsList actionsOnCast;
		public IActionsList actionsOnExecute;
		public IActionsList actionsOnFinish;

		public IConditionsList conditionsRequirements;

		public IActionsList actionsOnReset;
		public bool actionsOnResetExecuted = false;

		public UnityEvent onChange;
	}
}