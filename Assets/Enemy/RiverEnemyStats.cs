using UnityEngine;

/// <summary>
/// '강(River)' 타입 엘리트 적의 스탯 관리.
/// 정화 시 고유 NPC 역할을 주입합니다.
/// </summary>
public class RiverEnemyStats : EnemyStats
{
    [Header("[River] 엘리트 설정")]
    [SerializeField] private RiverNPCRole roleToAssign;

    private void Awake()
    {
        purificationType = WorldPurificationManager.EnemyType.River;
    }

    protected override void OnPurified(PurifiedNPC npc)
    {
        // VillageManager에 해금 알림
        VillageManager.Instance?.UnlockNPCRole(roleToAssign);

        // 정화된 NPC 오브젝트에 역할 데이터 추가
        var data = npc.gameObject.AddComponent<RiverNPCData>();
        data.Initialize(roleToAssign);

        // NPC 이름과 대화 초기 설정
        npc.npcName = data.RoleName;
        
        string roleDesc = roleToAssign switch
        {
            RiverNPCRole.Shaman      => "나는 주술사... 당신의 살점(HP)을 돌보고 소모를 줄여줄 수 있소. 단, 대가가 필요하네.",
            RiverNPCRole.Blacksmith  => "대장장이입니다. 살점 정수를 가져오면 더 넓은 정화 범위를 확보해드리지요.",
            RiverNPCRole.Storyteller => "이야기꾼이라네. 정수를 빌려준다면 0단계의 진정한 위력을 일깨워주겠소.",
            _                        => "정화해줘서 고맙네."
        };

        npc.SetDialogue(new string[] { roleDesc, "마을로 돌아가면 다시 대화하세." });
        
        Debug.Log($"[RiverStats] {roleToAssign} 역할이 NPC에게 주입되었습니다.");
    }
}
