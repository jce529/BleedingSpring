using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// 태세(Stance) 시스템을 관리하는 중앙 컴포넌트.
/// 각 태세별로 독립된 스킬 클래스(Strategy)를 할당하고 현재 태세에 맞는 스킬을 반환합니다.
/// </summary>
public class PlayerStanceManager : MonoBehaviour
{
    [Serializable]
    public class StanceDefinition
    {
        public StanceType StanceType;
        public SkillBase  MainSkill;
        public SkillBase  SubSkill;
    }

    [Header("태세 데이터 설정")]
    [SerializeField] private List<StanceDefinition> stances = new List<StanceDefinition>();

    public StanceType MainStanceType { get; private set; }
    public StanceType SubStanceType  { get; private set; }

    /// <summary>현재 서브 태세가 활성화되어 있는지 여부</summary>
    public bool IsSubStanceActive { get; private set; }

    private Dictionary<StanceType, StanceDefinition> stanceLookup = new Dictionary<StanceType, StanceDefinition>();

    public void Initialize(IPlayerContext context)
    {
        stanceLookup.Clear();
        foreach (var def in stances)
        {
            if (def == null) continue;
            stanceLookup[def.StanceType] = def;

            if (def.MainSkill != null) def.MainSkill.Initialize(context);
            if (def.SubSkill  != null) def.SubSkill.Initialize(context);
        }

        // 초기 태세 임시 설정 (에디터 설정 전까지는 Water 기본값)
        if (MainStanceType == StanceType.Water && SubStanceType == StanceType.Water)
        {
             SetStances(StanceType.Water, StanceType.Water);
        }
    }

    /// <summary>태세를 전환합니다. (메인 ↔ 서브)</summary>
    public void ToggleStance()
    {
        IsSubStanceActive = !IsSubStanceActive;
        Debug.Log($"[StanceManager] 태세 전환: {(IsSubStanceActive ? "Sub" : "Main")} ({GetCurrentStanceType()})");
    }

    public StanceType GetCurrentStanceType()
    {
        return IsSubStanceActive ? SubStanceType : MainStanceType;
    }

    /// <summary>현재 활성화된 태세의 메인 스킬(슬롯 1)을 가져옵니다.</summary>
    public ISkill GetActiveMainSkill()
    {
        StanceType current = GetCurrentStanceType();
        if (stanceLookup.TryGetValue(current, out var def))
            return def.MainSkill;
        return null;
    }

    /// <summary>현재 활성화된 태세의 서브 스킬(슬롯 2)을 가져옵니다.</summary>
    public ISkill GetActiveSubSkill()
    {
        StanceType current = GetCurrentStanceType();
        if (stanceLookup.TryGetValue(current, out var def))
            return def.SubSkill;
        return null;
    }

    /// <summary>슬롯 0(기본 공격)을 가져옵니다. 기본 공격은 모든 태세에서 공통일 수도, 태세별로 다를 수도 있습니다.</summary>
    public ISkill GetBasicAttack()
    {
        // 현재는 태세 매핑에 기본 공격 슬롯이 없으므로, 인스펙터의 첫 번째 태세의 MainSkill을 임시로 쓰거나 
        // 별도 슬롯이 필요합니다. 일단 null 반환 후 컨트롤러에서 기본 컴포넌트 참조를 유지하거나 확장 필요.
        return null; 
    }

    public void SetStances(StanceType main, StanceType sub)
    {
        MainStanceType = main;
        SubStanceType  = sub;
    }
}
