using System.Runtime.CompilerServices;
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

        public DemandParameterData m_DemandParameters;

        public TimeSettingsData m_TimeSettings;

        public EntityCommandBuffer m_CommandBuffer;

        [ReadOnly]
        public NativeArray<int> m_FreeWorkplaces; // RealPop

        [ReadOnly]
        public Colossal.Collections.NativeValue<int> m_StudyPositions; // RealPop

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
                // Infixo: num2 is an actual age that will be used to replace m_BirthDay at the end
                int num2 = 0;
                //int citClass = citizen.m_BirthDay; // debug
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
                    // Infixo: these are ADULTS in households
                    int adultAgeLimitInDays = AgingSystem_RealPop.GetAdultAgeLimitInDays();
                    if (s_NoChildrenWhenTooOld) // modded
                        num2 = random.NextInt(adultAgeLimitInDays, AgingSystem_RealPop.GetElderAgeLimitInDays() - adultAgeLimitInDays);
                    else // vanilla
                        num2 = adultAgeLimitInDays + random.NextInt(AgingSystem_RealPop.GetElderAgeLimitInDays() - adultAgeLimitInDays);
                    citizen.SetAge(CitizenAge.Adult);
                    citizen.m_State |= CitizenFlags.NeedsNewJob;
                    if (flag)
                    {
                        citizen.SetEducationLevel((random.NextInt(6) + 3) / 2);
                    }
                    else
                    {
                        // non-commuters
                        if (s_NewAdultsAnyEducation) // modded
                        {
                            // 240224 Issue #12 adjust education to available jobs
                            // Stop bringing Well and Highly edu once College is available
                            // TODO: this should be done using DevTree and checking if specific service is unlocked
                            // 240305 Fix for Uneducated being rolled
                            int total = m_FreeWorkplaces[0] + m_FreeWorkplaces[1] + m_FreeWorkplaces[2];
                            if (m_StudyPositions.value <= 0)
                                total += m_FreeWorkplaces[3] + m_FreeWorkplaces[4];
                            //Plugin.Log($"Chances {total}: {m_FreeWorkplaces[0]} {m_FreeWorkplaces[1]} {m_FreeWorkplaces[2]} {m_FreeWorkplaces[3]} {m_FreeWorkplaces[4]}");
                            int eduLevel = 0;
                            if (total == 0) // no free workplaces, fallback to vanilla behavior to avoid problems in early game
                            {
                                eduLevel = (random.NextInt(5) + 1) / 2;
                                //Plugin.Log($"... edu level {eduLevel}");
                            }
                            else // roll a dynamic edu level
                            {
                                int roll = random.NextInt(total);
                                int startLook = m_StudyPositions.value > 0 ? 2 : 4;
                                for (int c = startLook; c >= 0; c--)
                                {
                                    total -= m_FreeWorkplaces[c];
                                    if (roll >= total)
                                    {
                                        eduLevel = c;
                                        break;
                                    }
                                }
                                //Plugin.Log($"... edu level {eduLevel}, rolled {roll}, start {startLook}");
                            }
                            citizen.SetEducationLevel(eduLevel);
                        }
                        else // vanilla behavior, 20% 0, 40% 1, 40% 1
                            citizen.SetEducationLevel((random.NextInt(5) + 1) / 2);
                    }
                }
                else if (citizen.m_BirthDay == 2)
                {
                    // Infixo: these are CHILDREN in households
                    float studyWillingness = citizen.GetPseudoRandom(CitizenPseudoRandom.StudyWillingness).NextFloat();
					// RealPop: uses mod's teen spawn percentage
                    if (random.NextFloat(1f) > s_TeenSpawnPercentage)
                    {
                        // Infixo: spawn CHILD
                        num2 = random.NextInt(1, AgingSystem_RealPop.GetTeenAgeLimitInDays()); // exclude 0, as these are newborns that are handled differently
                        citizen.SetAge(CitizenAge.Child);
                        citizen.SetEducationLevel(0);
                    }
                    else
                    {
                        // Infixo: spawn TEEN
                        citizen.SetAge(CitizenAge.Teen);
                        // num3 is grad probability from elementary school
                        float num3 = math.pow(1f - GraduationSystem_RealPop.GetGraduationProbability(1, 75, 0f, default(float2), default(float2), studyWillingness, 1f), 4f);
                        int educationLevel = ((!(random.NextFloat(1f) < num3)) ? 1 : 0);
                        citizen.SetEducationLevel(educationLevel);
                        if (educationLevel == 1) // finished elementary, ready for high school, could even start it so we will accrue some random years from the high school
                            num2 = AgingSystem_RealPop.GetTeenAgeLimitInDays() + random.NextInt(s_Education2InDays);
                        else // no elementary, it will always be Uneducated, so age can be anything
                            num2 = random.NextInt(AgingSystem_RealPop.GetTeenAgeLimitInDays(), AgingSystem_RealPop.GetAdultAgeLimitInDays());
                    }
                }
                else if (citizen.m_BirthDay == 3)
                {
                    // Infixo: these are ELDERS in households
                    num2 = AgingSystem_RealPop.GetElderAgeLimitInDays() + random.NextInt(3 * daysPerYear);
                    citizen.SetAge(CitizenAge.Elderly);
                    if (s_NewAdultsAnyEducation)
                        citizen.SetEducationLevel(random.NextInt(5));
                    else // vanilla behavior, even spread
                        citizen.SetEducationLevel(random.NextInt(3));
                }
                else
                {
                    // Infixo: these are STUDENTS in households
                    if (s_AllowTeenStudents) // modded
                    {
                        int educationLevel = ( random.NextInt(100) < 50 ? 2 : 3 ); // decide if going for College or University, it is hardcoded 50% now
                        citizen.SetEducationLevel(educationLevel);
                        if (educationLevel == 2) // Teen for College
                            num2 = AgingSystem_RealPop.GetAdultAgeLimitInDays() - s_Education3InDays - 1;
                        else // Adult for University
                            num2 = AgingSystem_RealPop.GetAdultAgeLimitInDays();
                    }
                    else // vanilla
                    {
                        //num2 = 3 * daysPerYear + random.NextInt(daysPerYear);
                        num2 = AgingSystem_RealPop.GetAdultAgeLimitInDays() + random.NextInt(daysPerYear);
                        citizen.SetAge(CitizenAge.Adult);
                        citizen.SetEducationLevel(random.NextInt(1) + 2);
                    }
                }
                citizen.m_BirthDay = (short)(TimeSystem.GetDay(m_SimulationFrame, m_TimeData) - num2);
                m_Citizens[entity] = citizen;
                //Plugin.Log($"Citizen{citClass}: {num2} {citizen.GetAge()} {citizen.GetEducationLevel()}");
            }
            //Plugin.Log($"InitializeCitizenJob: frame {m_SimulationFrame} finished");
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

    private EntityQuery m_DemandParameterQuery;

    private SimulationSystem m_SimulationSystem;

    private TriggerSystem m_TriggerSystem;

    private CountFreeWorkplacesSystem m_CountFreeWorkplacesSystem;

    private CountStudyPositionsSystem m_CountStudyPositionsSystem;

    private ModificationBarrier5 m_EndFrameBarrier;

    private TypeHandle __TypeHandle;

    // config values
    private static bool s_NewAdultsAnyEducation;
    private static bool s_NoChildrenWhenTooOld;
    private static bool s_AllowTeenStudents;
    private static int s_Education2InDays;
    private static int s_Education3InDays;
    private static float s_TeenSpawnPercentage;

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
        m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
        m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
        m_Additions = GetEntityQuery(ComponentType.ReadWrite<Citizen>(), ComponentType.ReadWrite<HouseholdMember>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Temp>());
        m_CitizenPrefabs = GetEntityQuery(ComponentType.ReadOnly<CitizenData>());
        m_TimeSettingGroup = GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
        m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
        m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
        RequireForUpdate(m_Additions);
        RequireForUpdate(m_TimeDataQuery);
        RequireForUpdate(m_TimeSettingGroup);
        RequireForUpdate(m_DemandParameterQuery);
        // RealPop
        m_CountFreeWorkplacesSystem = base.World.GetOrCreateSystemManaged<CountFreeWorkplacesSystem>();
        m_CountStudyPositionsSystem = base.World.GetOrCreateSystemManaged<CountStudyPositionsSystem>();
        s_NewAdultsAnyEducation = Plugin.NewAdultsAnyEducation.Value;
        s_NoChildrenWhenTooOld = Plugin.NoChildrenWhenTooOld.Value;
        s_AllowTeenStudents = Plugin.AllowTeenStudents.Value;
        s_Education2InDays = Plugin.Education2InDays.Value;
        s_Education3InDays = Plugin.Education3InDays.Value;
        int teenAge = AgingSystem_RealPop.GetTeenAgeLimitInDays();
        s_TeenSpawnPercentage = 1f - (float)teenAge / (float)(teenAge + s_Education2InDays);
        Plugin.Log($"Modded CitizenInitializeSystem created. NewAdultsAnyEducation={s_NewAdultsAnyEducation}, NoChildrenWhenTooOld={s_NoChildrenWhenTooOld}, AllowTeenStudents={s_AllowTeenStudents}, TeenSpawnPercentage={100f*s_TeenSpawnPercentage:F0}");
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
        initializeCitizenJob.m_DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>();
        initializeCitizenJob.m_TimeSettings = m_TimeSettingGroup.GetSingleton<TimeSettingsData>();
        initializeCitizenJob.m_TimeData = m_TimeDataQuery.GetSingleton<TimeData>();
        initializeCitizenJob.m_SimulationFrame = m_SimulationSystem.frameIndex;
        initializeCitizenJob.m_RandomSeed = RandomSeed.Next();
        initializeCitizenJob.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
        initializeCitizenJob.m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer();
        initializeCitizenJob.m_FreeWorkplaces = m_CountFreeWorkplacesSystem.GetFreeWorkplaces(out var outJobHandleFreeWorkplaces);
        initializeCitizenJob.m_StudyPositions = m_CountStudyPositionsSystem.GetStudyPositions(out var outJobHandleStudyPositions); // This job handle will be ignored
        InitializeCitizenJob jobData = initializeCitizenJob;
        //Plugin.Log($"AllocTemp: frame {m_SimulationSystem.frameIndex}");// entities {initializeCitizenJob.m_Entities.Length} citizens {initializeCitizenJob.m_CitizenPrefabs.Length}");
        base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, outJobHandle, outJobHandleFreeWorkplaces));
        m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
        m_CountFreeWorkplacesSystem.AddReader(base.Dependency); // RealPop
        m_CountStudyPositionsSystem.AddReader(base.Dependency); // RealPop
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
