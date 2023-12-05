using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Triggers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using Game;
using Game.Citizens;

namespace RealPop.Systems;

[CompilerGenerated]
public class CitizenInitializeSystem_RealPop : GameSystemBase
{
    [BurstCompile]
    private struct InitializeCitizenJob : IJob
    {
        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<Entity> m_Entities;

        [ReadOnly]
        public NativeList<HouseholdMember> m_HouseholdMembers;

        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<Entity> m_CitizenPrefabs;

        public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

        public ComponentLookup<Citizen> m_Citizens;

        public ComponentLookup<Arrived> m_Arriveds;

        public ComponentLookup<CrimeVictim> m_CrimeVictims;

        public ComponentLookup<CarKeeper> m_CarKeepers;

        public ComponentLookup<MailSender> m_MailSenders;

        [ReadOnly]
        public ComponentLookup<CitizenData> m_CitizenDatas;

        public NativeQueue<TriggerAction> m_TriggerBuffer;

        [ReadOnly]
        public RandomSeed m_RandomSeed;

        public uint m_SimulationFrame;

        [DeallocateOnJobCompletion]
        public TimeData m_TimeData;

        public TimeSettingsData m_TimeSettings;

        public EntityCommandBuffer m_CommandBuffer;

        public void Execute()
        {
            //RealPop.Debug.Log($"initializing {m_Entities.Length} citizens");
            int daysPerYear = m_TimeSettings.m_DaysPerYear;
            Random random = m_RandomSeed.GetRandom(0);
            for (int i = 0; i < m_Entities.Length; i++)
            {
                Entity entity = m_Entities[i];
                m_Arriveds.SetComponentEnabled(entity, value: false);
                m_MailSenders.SetComponentEnabled(entity, value: false);
                m_CrimeVictims.SetComponentEnabled(entity, value: false);
                m_CarKeepers.SetComponentEnabled(entity, value: false);
                Citizen citizen = m_Citizens[entity];
                Entity household = m_HouseholdMembers[i].m_Household;
                bool flag = (citizen.m_State & CitizenFlags.Commuter) != 0;
                bool num = (citizen.m_State & CitizenFlags.Tourist) != 0;
                citizen.m_PseudoRandom = (ushort)(random.NextUInt() % 65536u);
                citizen.m_Health = (byte)(40 + random.NextInt(20));
                citizen.m_WellBeing = (byte)(40 + random.NextInt(20));
                if (num)
                {
                    citizen.m_LeisureCounter = (byte)random.NextInt(128);
                }
                else
                {
                    citizen.m_LeisureCounter = (byte)(random.NextInt(92) + 128);
                }
                if (random.NextBool())
                {
                    citizen.m_State |= CitizenFlags.Male;
                }
                Entity prefab = GetPrefab(m_CitizenPrefabs, citizen, m_CitizenDatas, random);
                m_CommandBuffer.AddComponent(entity, new PrefabRef
                {
                    m_Prefab = prefab
                });
                DynamicBuffer<HouseholdCitizen> dynamicBuffer = m_HouseholdCitizens[household];
                dynamicBuffer.Add(new HouseholdCitizen
                {
                    m_Citizen = entity
                });
                int num2 = 0;
                if (citizen.m_BirthDay == 0)
                {
                    citizen.SetAge(CitizenAge.Child);
                    Entity entity2 = Entity.Null;
                    Entity entity3 = Entity.Null;
                    for (int j = 0; j < dynamicBuffer.Length; j++)
                    {
                        Entity citizen2 = dynamicBuffer[j].m_Citizen;
                        if (m_Citizens.HasComponent(citizen2) && m_Citizens[citizen2].GetAge() == CitizenAge.Adult)
                        {
                            if (entity2 == Entity.Null)
                            {
                                entity2 = citizen2;
                            }
                            else
                            {
                                entity3 = citizen2;
                            }
                        }
                    }
                    if (entity2 != Entity.Null)
                    {
                        if (entity3 != Entity.Null)
                        {
                            m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenCoupleMadeBaby, Entity.Null, entity2, entity));
                        }
                        else
                        {
                            m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenSingleMadeBaby, Entity.Null, entity2, entity));
                        }
                    }
                }
                else if (citizen.m_BirthDay == 1)
                {
                    int adultAgeLimitInDays = AgingSystem_RealPop.GetAdultAgeLimitInDays();
                    num2 = adultAgeLimitInDays + random.NextInt(AgingSystem_RealPop.GetElderAgeLimitInDays() - adultAgeLimitInDays);
                    citizen.SetAge(CitizenAge.Adult);
                    citizen.m_State |= CitizenFlags.NeedsNewJob;
                    if (flag)
                    {
                        citizen.SetEducationLevel((random.NextInt(6) + 3) / 2);
                    }
                    else
                    {
                        citizen.SetEducationLevel((random.NextInt(5) + 1) / 2);
                    }
                }
                else if (citizen.m_BirthDay == 2)
                {
                    num2 = random.NextInt(AgingSystem_RealPop.GetAdultAgeLimitInDays());
                    float studyWillingness = citizen.GetPseudoRandom(CitizenPseudoRandom.StudyWillingness).NextFloat();
                    if (num2 < AgingSystem_RealPop.GetTeenAgeLimitInDays())
                    {
                        citizen.SetAge(CitizenAge.Child);
                        citizen.SetEducationLevel(0);
                    }
                    else
                    {
                        citizen.SetAge(CitizenAge.Teen);
                        float value = 1f - math.pow(1f - GraduationSystem_RealPop.GetGraduationProbability(1, 50, 0f, default(float2), default(float2), studyWillingness, 1f), 4f);
                        int educationLevel = MathUtils.RoundToIntRandom(ref random, value);
                        citizen.SetEducationLevel(educationLevel);
                    }
                }
                else if (citizen.m_BirthDay == 3)
                {
                    num2 = AgingSystem_RealPop.GetElderAgeLimitInDays() + random.NextInt(3 * daysPerYear);
                    citizen.SetAge(CitizenAge.Elderly);
                    citizen.SetEducationLevel(random.NextInt(3));
                }
                else
                {
                    num2 = 3 * daysPerYear + random.NextInt(daysPerYear);
                    citizen.SetAge(CitizenAge.Adult);
                    citizen.SetEducationLevel(random.NextInt(1) + 2);
                }
                citizen.m_BirthDay = (short)(TimeSystem.GetDay(m_SimulationFrame, m_TimeData) - num2);
                m_Citizens[entity] = citizen;
            }
        }
    }

    private struct TypeHandle
    {
        public ComponentLookup<Citizen> __Game_Citizens_Citizen_RW_ComponentLookup;

        public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RW_BufferLookup;

        public ComponentLookup<Arrived> __Game_Citizens_Arrived_RW_ComponentLookup;

        public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RW_ComponentLookup;

        public ComponentLookup<MailSender> __Game_Citizens_MailSender_RW_ComponentLookup;

        public ComponentLookup<CrimeVictim> __Game_Citizens_CrimeVictim_RW_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<CitizenData> __Game_Prefabs_CitizenData_RO_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Game_Citizens_Citizen_RW_ComponentLookup = state.GetComponentLookup<Citizen>();
            __Game_Citizens_HouseholdCitizen_RW_BufferLookup = state.GetBufferLookup<HouseholdCitizen>();
            __Game_Citizens_Arrived_RW_ComponentLookup = state.GetComponentLookup<Arrived>();
            __Game_Citizens_CarKeeper_RW_ComponentLookup = state.GetComponentLookup<CarKeeper>();
            __Game_Citizens_MailSender_RW_ComponentLookup = state.GetComponentLookup<MailSender>();
            __Game_Citizens_CrimeVictim_RW_ComponentLookup = state.GetComponentLookup<CrimeVictim>();
            __Game_Prefabs_CitizenData_RO_ComponentLookup = state.GetComponentLookup<CitizenData>(isReadOnly: true);
        }
    }

    private EntityQuery m_Additions;

    private EntityQuery m_TimeSettingGroup;

    private EntityQuery m_CitizenPrefabs;

    private EntityQuery m_TimeDataQuery;

    private SimulationSystem m_SimulationSystem;

    private TriggerSystem m_TriggerSystem;

    private ModificationBarrier5 m_EndFrameBarrier;

    private TypeHandle __TypeHandle;

    [Preserve]
    protected override void OnCreate()
    {
        RealPop.Debug.Log("Modded CitizenInitializeSystem created.");
        base.OnCreate();
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
        m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
        m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
        m_Additions = GetEntityQuery(ComponentType.ReadWrite<Citizen>(), ComponentType.ReadWrite<HouseholdMember>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Temp>());
        m_CitizenPrefabs = GetEntityQuery(ComponentType.ReadOnly<CitizenData>());
        m_TimeSettingGroup = GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
        m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
        RequireForUpdate(m_Additions);
        RequireForUpdate(m_TimeDataQuery);
        RequireForUpdate(m_TimeSettingGroup);
    }

    [Preserve]
    protected override void OnUpdate()
    {
        __TypeHandle.__Game_Prefabs_CitizenData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_CrimeVictim_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_MailSender_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_CarKeeper_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Arrived_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HouseholdCitizen_RW_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Citizen_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        InitializeCitizenJob initializeCitizenJob = default(InitializeCitizenJob);
        initializeCitizenJob.m_Entities = m_Additions.ToEntityArray(Allocator.TempJob);
        initializeCitizenJob.m_HouseholdMembers = m_Additions.ToComponentDataListAsync<HouseholdMember>(base.World.UpdateAllocator.ToAllocator, out var outJobHandle);
        initializeCitizenJob.m_CitizenPrefabs = m_CitizenPrefabs.ToEntityArray(Allocator.TempJob);
        initializeCitizenJob.m_Citizens = __TypeHandle.__Game_Citizens_Citizen_RW_ComponentLookup;
        initializeCitizenJob.m_HouseholdCitizens = __TypeHandle.__Game_Citizens_HouseholdCitizen_RW_BufferLookup;
        initializeCitizenJob.m_Arriveds = __TypeHandle.__Game_Citizens_Arrived_RW_ComponentLookup;
        initializeCitizenJob.m_CarKeepers = __TypeHandle.__Game_Citizens_CarKeeper_RW_ComponentLookup;
        initializeCitizenJob.m_MailSenders = __TypeHandle.__Game_Citizens_MailSender_RW_ComponentLookup;
        initializeCitizenJob.m_CrimeVictims = __TypeHandle.__Game_Citizens_CrimeVictim_RW_ComponentLookup;
        initializeCitizenJob.m_CitizenDatas = __TypeHandle.__Game_Prefabs_CitizenData_RO_ComponentLookup;
        initializeCitizenJob.m_TimeSettings = m_TimeSettingGroup.GetSingleton<TimeSettingsData>();
        initializeCitizenJob.m_TimeData = m_TimeDataQuery.GetSingleton<TimeData>();
        initializeCitizenJob.m_SimulationFrame = m_SimulationSystem.frameIndex;
        initializeCitizenJob.m_RandomSeed = RandomSeed.Next();
        initializeCitizenJob.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
        initializeCitizenJob.m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer();
        InitializeCitizenJob jobData = initializeCitizenJob;
        base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
        m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
    }

    public static Entity GetPrefab(NativeArray<Entity> citizenPrefabs, Citizen citizen, ComponentLookup<CitizenData> citizenDatas, Random rnd)
    {
        int num = 0;
        for (int i = 0; i < citizenPrefabs.Length; i++)
        {
            CitizenData citizenData = citizenDatas[citizenPrefabs[i]];
            if (((citizen.m_State & CitizenFlags.Male) == 0) ^ citizenData.m_Male)
            {
                num++;
            }
        }
        if (num > 0)
        {
            int num2 = rnd.NextInt(num);
            for (int j = 0; j < citizenPrefabs.Length; j++)
            {
                CitizenData citizenData2 = citizenDatas[citizenPrefabs[j]];
                if (((citizen.m_State & CitizenFlags.Male) == 0) ^ citizenData2.m_Male)
                {
                    num2--;
                    if (num2 < 0)
                    {
                        PrefabRef prefabRef = default(PrefabRef);
                        prefabRef.m_Prefab = citizenPrefabs[j];
                        return prefabRef.m_Prefab;
                    }
                }
            }
        }
        return Entity.Null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void __AssignQueries(ref SystemState state)
    {
    }

    protected override void OnCreateForCompiler()
    {
        base.OnCreateForCompiler();
        __AssignQueries(ref base.CheckedStateRef);
        __TypeHandle.__AssignHandles(ref base.CheckedStateRef);
    }

    [Preserve]
    public CitizenInitializeSystem_RealPop()
    {
    }
}
