using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Mathematics;
using Game.Achievements;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Creatures;
using Game.Notifications;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace RealPop.Systems;

[CompilerGenerated]
public class DeathCheckSystem_RealPop : GameSystemBase
{
    [BurstCompile]
    private struct DeathCheckJob : IJobChunk
    {
        [ReadOnly]
        public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public ComponentTypeHandle<Citizen> m_CitizenType;

        [ReadOnly]
        public ComponentTypeHandle<Worker> m_WorkerType;

        [ReadOnly]
        public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;

        [ReadOnly]
        public ComponentTypeHandle<Game.Citizens.Student> m_StudentType;

        [ReadOnly]
        public ComponentTypeHandle<ResourceBuyer> m_ResourceBuyerType;

        [ReadOnly]
        public ComponentTypeHandle<Leisure> m_LeisureType;

        public ComponentTypeHandle<HealthProblem> m_HealthProblemType;

        [ReadOnly]
        public ComponentLookup<CurrentBuilding> m_CurrentBuildings;

        [ReadOnly]
        public ComponentLookup<Game.Buildings.Hospital> m_HospitalData;

        [ReadOnly]
        public ComponentLookup<Building> m_BuildingData;

        [ReadOnly]
        public ComponentLookup<Game.Creatures.Resident> m_ResidentData;

        [ReadOnly]
        public ComponentLookup<CurrentTransport> m_CurrentTransport;

        [ReadOnly]
        public BufferLookup<CityModifier> m_CityModifiers;

        [ReadOnly]
        public BufferLookup<Game.Buildings.Student> m_Students;

        [ReadOnly]
        public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

        [ReadOnly]
        public uint m_UpdateFrameIndex;

        [ReadOnly]
        public RandomSeed m_RandomSeed;

        [ReadOnly]
        public HealthcareParameterData m_HealthcareParameterData;

        [ReadOnly]
        public Entity m_City;

        public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

        public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;

        public IconCommandBuffer m_IconCommandBuffer;

        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

        public NativeCounter.Concurrent m_PatientsRecoveredCounter;

        public TimeSettingsData m_TimeSettings;

        public TimeData m_TimeData;

        public uint m_SimulationFrame;

        private void Die(ArchetypeChunk chunk, int chunkIndex, int i, Entity citizen, Entity household, NativeArray<Game.Citizens.Student> students, NativeArray<HealthProblem> healthProblems)
        {
            if (!healthProblems.IsCreated)
            {
                HealthProblem healthProblem = default(HealthProblem);
                healthProblem.m_Flags = HealthProblemFlags.Dead | HealthProblemFlags.RequireTransport;
                HealthProblem component = healthProblem;
                m_CommandBuffer.AddComponent(chunkIndex, citizen, component);
            }
            else
            {
                HealthProblem value = healthProblems[i];
                if ((value.m_Flags & HealthProblemFlags.RequireTransport) != 0)
                {
                    m_IconCommandBuffer.Remove(citizen, m_HealthcareParameterData.m_AmbulanceNotificationPrefab);
                    value.m_Timer = 0;
                }
                value.m_Flags &= ~(HealthProblemFlags.Sick | HealthProblemFlags.Injured);
                value.m_Flags |= HealthProblemFlags.Dead | HealthProblemFlags.RequireTransport;
                healthProblems[i] = value;
            }
            PerformAfterDeathActions(citizen, household, m_TriggerBuffer, m_StatisticsEventQueue, ref m_HouseholdCitizens);
            if (chunk.Has(ref m_StudentType))
            {
                Entity school = students[i].m_School;
                if (m_Students.HasBuffer(school))
                {
                    m_CommandBuffer.AddComponent<StudentsRemoved>(chunkIndex, school);
                }
                m_CommandBuffer.RemoveComponent<Game.Citizens.Student>(chunkIndex, citizen);
            }
            if (chunk.Has(ref m_WorkerType))
            {
                m_CommandBuffer.RemoveComponent<Worker>(chunkIndex, citizen);
            }
            if (chunk.Has(ref m_ResourceBuyerType))
            {
                m_CommandBuffer.RemoveComponent<ResourceBuyer>(chunkIndex, citizen);
            }
            if (chunk.Has(ref m_LeisureType))
            {
                m_CommandBuffer.RemoveComponent<Leisure>(chunkIndex, citizen);
            }
        }

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
            {
                return;
            }
            Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
            DynamicBuffer<CityModifier> modifiers = m_CityModifiers[m_City];
            NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
            NativeArray<HealthProblem> nativeArray2 = chunk.GetNativeArray(ref m_HealthProblemType);
            NativeArray<Citizen> nativeArray3 = chunk.GetNativeArray(ref m_CitizenType);
            NativeArray<Game.Citizens.Student> nativeArray4 = chunk.GetNativeArray(ref m_StudentType);
            NativeArray<HouseholdMember> nativeArray5 = chunk.GetNativeArray(ref m_HouseholdMemberType);
            for (int i = 0; i < chunk.Count; i++)
            {
                Entity entity = nativeArray[i];
                Entity household = ((nativeArray5.Length != 0) ? nativeArray5[i].m_Household : Entity.Null);
                Citizen citizen = nativeArray3[i];
                if (m_CurrentTransport.HasComponent(entity))
                {
                    CurrentTransport currentTransport = m_CurrentTransport[entity];
                    if (m_ResidentData.HasComponent(currentTransport.m_CurrentTransport) && (m_ResidentData[currentTransport.m_CurrentTransport].m_Flags & ResidentFlags.InVehicle) != 0)
                    {
                        continue;
                    }
                }
                if (nativeArray2.IsCreated && (nativeArray2[i].m_Flags & HealthProblemFlags.Dead) != 0)
                {
                    continue;
                }

                /* ORIGINAL CODE
                float ageInDays = citizen.GetAgeInDays(m_SimulationFrame, m_TimeData);
                if (ageInDays / (float)m_TimeSettings.m_DaysPerYear >= (float)kMaxAge)
                {
                    int num = citizen.GetPseudoRandom(CitizenPseudoRandom.Death).NextInt(m_TimeSettings.m_DaysPerYear);
                    if (ageInDays - (float)(m_TimeSettings.m_DaysPerYear * kMaxAge) > (float)num)
                    {
                        Die(chunk, unfilteredChunkIndex, i, entity, household, nativeArray4, nativeArray2);
                        continue;
                    }
                }
                */
                // RealPop
                int ageInDays = TimeSystem.GetDay(m_SimulationFrame, m_TimeData) - citizen.m_BirthDay;
                if (ageInDays > s_DeathStartAge)
                {
                    int deathChance = (ageInDays - s_DeathStartAge) * s_DeathChanceIncrease;
                    //bool isDead = citizen.GetPseudoRandom(CitizenPseudoRandom.Death).NextInt(1000) < deathChance; // Infixo: doesn't generate good random numbers (!)
                    bool isDead = random.NextInt(1000) < deathChance;
                    //Plugin.Log($"Death: {ageInDays} {deathChance} -> {isDead}");
                    if (isDead)
                    {
                        Die(chunk, unfilteredChunkIndex, i, entity, household, nativeArray4, nativeArray2);
                        continue;
                    }
                }

                if (!nativeArray2.IsCreated || (nativeArray2[i].m_Flags & (HealthProblemFlags.Sick | HealthProblemFlags.Injured)) == 0)
                {
                    continue;
                }
                HealthProblem value = nativeArray2[i];
                int num2 = 10 - (int)citizen.m_Health / 10;
                int num3 = num2 * num2;
                num3 += 8;
                if (random.NextInt(kUpdatesPerDay * 1000) <= num3)
                {
                    Die(chunk, unfilteredChunkIndex, i, entity, household, nativeArray4, nativeArray2);
                    continue;
                }
                float num4 = MathUtils.Logistic(3f, 1000f, 6f, (float)num2 / 10f - 0.35f);
                int num5 = 0;
                if (m_CurrentBuildings.HasComponent(entity))
                {
                    Entity currentBuilding = m_CurrentBuildings[entity].m_CurrentBuilding;
                    if (m_BuildingData.HasComponent(currentBuilding) && !BuildingUtils.CheckOption(m_BuildingData[currentBuilding], BuildingOption.Inactive) && m_HospitalData.HasComponent(currentBuilding))
                    {
                        num5 = m_HospitalData[currentBuilding].m_TreatmentBonus;
                    }
                }
                num4 -= (float)(10 * num5);
                CityUtils.ApplyModifier(ref num4, modifiers, CityModifierType.RecoveryFailChange);
                if (random.NextFloat(1000f) >= num4)
                {
                    if ((value.m_Flags & HealthProblemFlags.RequireTransport) != 0)
                    {
                        m_IconCommandBuffer.Remove(entity, m_HealthcareParameterData.m_AmbulanceNotificationPrefab);
                        value.m_Timer = 0;
                    }
                    value.m_Flags &= ~(HealthProblemFlags.Sick | HealthProblemFlags.Injured | HealthProblemFlags.RequireTransport);
                    nativeArray2[i] = value;
                    m_PatientsRecoveredCounter.Increment();
                }
            }
        }

        void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    private struct TypeHandle
    {
        [ReadOnly]
        public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Worker> __Game_Citizens_Worker_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;

        public ComponentTypeHandle<HealthProblem> __Game_Citizens_HealthProblem_RW_ComponentTypeHandle;

        public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Leisure> __Game_Citizens_Leisure_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<ResourceBuyer> __Game_Companies_ResourceBuyer_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Game.Buildings.Hospital> __Game_Buildings_Hospital_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Game.Creatures.Resident> __Game_Creatures_Resident_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<Game.Buildings.Student> __Game_Buildings_Student_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Citizens_Citizen_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>(isReadOnly: true);
            __Game_Citizens_Worker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Worker>(isReadOnly: true);
            __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true);
            __Game_Citizens_HealthProblem_RW_ComponentTypeHandle = state.GetComponentTypeHandle<HealthProblem>();
            __Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
            __Game_Citizens_Leisure_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Leisure>(isReadOnly: true);
            __Game_Companies_ResourceBuyer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResourceBuyer>(isReadOnly: true);
            __Game_Citizens_Student_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Citizens.Student>(isReadOnly: true);
            __Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(isReadOnly: true);
            __Game_Buildings_Hospital_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Hospital>(isReadOnly: true);
            __Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
            __Game_Citizens_CurrentTransport_RO_ComponentLookup = state.GetComponentLookup<CurrentTransport>(isReadOnly: true);
            __Game_Creatures_Resident_RO_ComponentLookup = state.GetComponentLookup<Game.Creatures.Resident>(isReadOnly: true);
            __Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
            __Game_Buildings_Student_RO_BufferLookup = state.GetBufferLookup<Game.Buildings.Student>(isReadOnly: true);
            __Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
        }
    }

    public static readonly int kUpdatesPerDay = 4;

    public static readonly int kMaxAge = 9;

    private SimulationSystem m_SimulationSystem;

    private IconCommandSystem m_IconCommandSystem;

    private CitySystem m_CitySystem;

    private CityStatisticsSystem m_CityStatisticsSystem;

    private EndFrameBarrier m_EndFrameBarrier;

    private EntityQuery m_DeathCheckQuery;

    private EntityQuery m_HealthcareSettingsQuery;

    private EntityQuery m_TimeSettingsQuery;

    private EntityQuery m_TimeDataQuery;

    private TriggerSystem m_TriggerSystem;

    private AchievementTriggerSystem m_AchievementTriggerSystem;

    private TypeHandle __TypeHandle;

    private static int s_DeathChanceIncrease; // RealPop

    private static int s_DeathStartAge; // RealPop

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 262144 / (kUpdatesPerDay * 16);
    }

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
        m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
        m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
        m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
        m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
        m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
        m_AchievementTriggerSystem = base.World.GetOrCreateSystemManaged<AchievementTriggerSystem>();
        m_DeathCheckQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
        m_HealthcareSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<HealthcareParameterData>());
        m_TimeSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
        m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
        RequireForUpdate(m_DeathCheckQuery);
        RequireForUpdate(m_HealthcareSettingsQuery);
        // RealPop
        s_DeathStartAge = Plugin.ElderAgeLimitInDays.Value;
        s_DeathChanceIncrease = Plugin.DeathChanceIncrease.Value;
        Plugin.Log($"Modded DeathCheckSystem created. DeathStartAge={s_DeathStartAge}, DeathChanceIncrease={s_DeathChanceIncrease}.");
    }

    [Preserve]
    protected override void OnUpdate()
    {
        uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
        __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Buildings_Student_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_City_CityModifier_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Creatures_Resident_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Buildings_Hospital_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Student_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Companies_ResourceBuyer_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Leisure_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HealthProblem_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
        DeathCheckJob deathCheckJob = default(DeathCheckJob);
        deathCheckJob.m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
        deathCheckJob.m_CitizenType = __TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle;
        deathCheckJob.m_WorkerType = __TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle;
        deathCheckJob.m_HouseholdMemberType = __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;
        deathCheckJob.m_HealthProblemType = __TypeHandle.__Game_Citizens_HealthProblem_RW_ComponentTypeHandle;
        deathCheckJob.m_UpdateFrameType = __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
        deathCheckJob.m_LeisureType = __TypeHandle.__Game_Citizens_Leisure_RO_ComponentTypeHandle;
        deathCheckJob.m_ResourceBuyerType = __TypeHandle.__Game_Companies_ResourceBuyer_RO_ComponentTypeHandle;
        deathCheckJob.m_StudentType = __TypeHandle.__Game_Citizens_Student_RO_ComponentTypeHandle;
        deathCheckJob.m_CurrentBuildings = __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup;
        deathCheckJob.m_HospitalData = __TypeHandle.__Game_Buildings_Hospital_RO_ComponentLookup;
        deathCheckJob.m_BuildingData = __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup;
        deathCheckJob.m_CurrentTransport = __TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentLookup;
        deathCheckJob.m_ResidentData = __TypeHandle.__Game_Creatures_Resident_RO_ComponentLookup;
        deathCheckJob.m_CityModifiers = __TypeHandle.__Game_City_CityModifier_RO_BufferLookup;
        deathCheckJob.m_Students = __TypeHandle.__Game_Buildings_Student_RO_BufferLookup;
        deathCheckJob.m_HouseholdCitizens = __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup;
        deathCheckJob.m_UpdateFrameIndex = updateFrame;
        deathCheckJob.m_RandomSeed = RandomSeed.Next();
        deathCheckJob.m_HealthcareParameterData = m_HealthcareSettingsQuery.GetSingleton<HealthcareParameterData>();
        deathCheckJob.m_City = m_CitySystem.City;
        deathCheckJob.m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer().AsParallelWriter();
        deathCheckJob.m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out var deps).AsParallelWriter();
        deathCheckJob.m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer();
        deathCheckJob.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();
        deathCheckJob.m_PatientsRecoveredCounter = m_AchievementTriggerSystem.m_PatientsTreatedCounter.ToConcurrent();
        deathCheckJob.m_SimulationFrame = m_SimulationSystem.frameIndex;
        deathCheckJob.m_TimeSettings = m_TimeSettingsQuery.GetSingleton<TimeSettingsData>();
        deathCheckJob.m_TimeData = m_TimeDataQuery.GetSingleton<TimeData>();
        DeathCheckJob jobData = deathCheckJob;
        base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_DeathCheckQuery, JobHandle.CombineDependencies(base.Dependency, deps));
        m_IconCommandSystem.AddCommandBufferWriter(base.Dependency);
        m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
        m_CityStatisticsSystem.AddWriter(base.Dependency);
        m_TriggerSystem.AddActionBufferWriter(base.Dependency);
    }

    public static void PerformAfterDeathActions(Entity citizen, Entity household, NativeQueue<TriggerAction>.ParallelWriter triggerBuffer, NativeQueue<StatisticsEvent>.ParallelWriter statisticsEventQueue, ref BufferLookup<HouseholdCitizen> householdCitizens)
    {
        triggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenDied, Entity.Null, citizen, Entity.Null));
        if (household != Entity.Null && householdCitizens.TryGetBuffer(household, out var bufferData))
        {
            for (int i = 0; i < bufferData.Length; i++)
            {
                if (bufferData[i].m_Citizen != citizen)
                {
                    triggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizensFamilyMemberDied, Entity.Null, bufferData[i].m_Citizen, citizen));
                }
            }
        }
        statisticsEventQueue.Enqueue(new StatisticsEvent
        {
            m_Statistic = StatisticType.DeathRate,
            m_Change = 1f
        });
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
    public DeathCheckSystem_RealPop()
    {
    }
}
