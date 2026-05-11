using UnityEngine;
using UnityEngine.Assertions;

public class PlayerStanceManager : MonoBehaviour
{
    public StanceType MainStance { get; private set; }
    public StanceType SubStance  { get; private set; }

    [SerializeField] private SkillBase[] mainSkills = new SkillBase[3];
    [SerializeField] private SkillBase[] subSkills  = new SkillBase[3];

    public void Initialize(IPlayerContext context)
    {
        foreach (var s in subSkills)
            if (s != null) s.Initialize(context);
    }

    public ISkill GetMainSkill(int slot)
    {
        Assert.IsTrue(slot >= 0 && slot < mainSkills.Length, $"[StanceManager] 잘못된 슬롯: {slot}");
        return mainSkills[slot];
    }

    public ISkill GetSubSkill(int slot)
    {
        Assert.IsTrue(slot >= 0 && slot < subSkills.Length, $"[StanceManager] 잘못된 슬롯: {slot}");
        return subSkills[slot];
    }

    public void SetStances(StanceType main, StanceType sub)
    {
        MainStance = main;
        SubStance  = sub;
    }
}
