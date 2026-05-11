using UnityEngine;

/// <summary>
/// '강(River)' 타입 적이 정화되었을 때의 NPC 역할을 정의합니다.
/// </summary>
public enum RiverNPCRole
{
    None,
    Shaman,     // 주술사 (최대 체력, 체력 소모 감소)
    Blacksmith, // 대장장이 (정화율 강화)
    Storyteller // 이야기꾼 (자아강화 - 0단계 강화)
}

/// <summary>
/// 정화된 '강' NPC의 메타데이터를 관리합니다.
/// 이 데이터는 마을 씬에서 해당 NPC의 기능을 결정하는 데 사용됩니다.
/// </summary>
public class RiverNPCData : MonoBehaviour
{
    public RiverNPCRole Role = RiverNPCRole.None;
    public string RoleName => Role switch
    {
        RiverNPCRole.Shaman      => "주술사",
        RiverNPCRole.Blacksmith  => "대장장이",
        RiverNPCRole.Storyteller => "이야기꾼",
        _                        => "정화된 영혼"
    };

    public void Initialize(RiverNPCRole role)
    {
        Role = role;
    }
}
