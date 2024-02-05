using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Collections;
using Colossal.Entities;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Debug;
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

namespace Game.Simulation;

[CompilerGenerated]
public class BirthSystem : GameSystemBase
{
    [BurstCompile]
    private struct CheckBirthJob : IJobChunk
    {
        public NativeCounter.Concurrent m_DebugBirthCounter;

        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public ComponentTypeHandle<Citizen> m_CitizenType;

        [ReadOnly]
        public ComponentTypeHandle<HouseholdMember> m_MemberType;

        [ReadOnly]
        public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

        [ReadOnly]
        public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

        [ReadOnly]
        public ComponentLookup<Citizen> m_Citizens;

        [ReadOnly]
        public ComponentLookup<Game.Citizens.Student> m_Students;

        [ReadOnly]
        public ComponentLookup<PropertyRenter> m_PropertyRenters;

        public uint m_UpdateFrameIndex;

        public RandomSeed m_RandomSeed;

        [ReadOnly]
        public int m_BirthChance;

        [ReadOnly]
        public NativeList<Entity> m_CitizenPrefabs;

        [ReadOnly]
        public NativeList<ArchetypeData> m_CitizenPrefabArchetypes;

        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

        public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

        private Entity SpawnBaby(int index, Entity household, ref Random random, Entity building)
        {
            m_DebugBirthCounter.Increment();
            int index2 = random.NextInt(m_CitizenPrefabs.Length);
            Entity prefab = m_CitizenPrefabs[index2];
            ArchetypeData archetypeData = m_CitizenPrefabArchetypes[index2];
            Entity entity = m_CommandBuffer.CreateEntity(index, archetypeData.m_Archetype);
            PrefabRef component = default(PrefabRef);
            component.m_Prefab = prefab;
            m_CommandBuffer.SetComponent(index, entity, component);
            HouseholdMember householdMember = default(HouseholdMember);
            householdMember.m_Household = household;
            HouseholdMember component2 = householdMember;
            m_CommandBuffer.AddComponent(index, entity, component2);
            Citizen citizen = default(Citizen);
            citizen.m_BirthDay = 0;
            citizen.m_State = CitizenFlags.None;
            Citizen component3 = citizen;
            m_CommandBuffer.SetComponent(index, entity, component3);
            m_CommandBuffer.AddComponent(index, entity, new CurrentBuilding
            {
                m_CurrentBuilding = building
            });
            return entity;
        }

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
            {
                return;
            }
            NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
            NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray(ref m_CitizenType);
            NativeArray<HouseholdMember> nativeArray3 = chunk.GetNativeArray(ref m_MemberType);
            Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
            for (int i = 0; i < nativeArray.Length; i++)
            {
                Entity entity = nativeArray[i];
                Citizen citizen = nativeArray2[i];
                if (citizen.GetAge() != CitizenAge.Adult || (citizen.m_State & (CitizenFlags.Male | CitizenFlags.Tourist | CitizenFlags.Commuter)) != 0)
                {
                    continue;
                }
                Entity household = nativeArray3[i].m_Household;
                Entity entity2 = Entity.Null;
                if (m_PropertyRenters.HasComponent(household))
                {
                    entity2 = m_PropertyRenters[household].m_Property;
                }
                if (entity2 == Entity.Null)
                {
                    continue;
                }
                DynamicBuffer<HouseholdCitizen> dynamicBuffer = m_HouseholdCitizens[household];
                int num = m_BirthChance;
                Entity @null = Entity.Null;
                for (int j = 0; j < dynamicBuffer.Length; j++)
                {
                    @null = dynamicBuffer[j].m_Citizen;
                    if (m_Citizens.HasComponent(@null))
                    {
                        Citizen citizen2 = m_Citizens[@null];
                        if ((citizen2.m_State & CitizenFlags.Male) != 0 && citizen2.GetAge() == CitizenAge.Adult)
                        {
                            num += 80;
                            break;
                        }
                    }
                }
                if (m_Students.HasComponent(entity))
                {
                    num /= 2;
                }
                if (random.NextInt(1000 * kUpdatesPerDay) < num)
                {
                    SpawnBaby(unfilteredChunkIndex, household, ref random, entity2);
                    m_StatisticsEventQueue.Enqueue(new StatisticsEvent
                    {
                        m_Statistic = StatisticType.BirthRate,
                        m_Change = 1f
                    });
                }
            }
        }

        void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    [BurstCompile]
    private struct SumBirthJob : IJob
    {
        public NativeCounter m_DebugBirthCount;

        public NativeValue<int> m_DebugBirth;

        public void Execute()
        {
            m_DebugBirth.value = m_DebugBirthCount.Count;
        }
    }

    private struct TypeHandle
    {
        [ReadOnly]
        public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;

        public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

        [ReadOnly]
        public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Citizens_Citizen_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>(isReadOnly: true);
            __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true);
            __Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
            __Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
            __Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
            __Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(isReadOnly: true);
            __Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
        }
    }

    public static readonly int kUpdatesPerDay = 16;

    private EndFrameBarrier m_EndFrameBarrier;

    private SimulationSystem m_SimulationSystem;

    private CityStatisticsSystem m_CityStatisticsSystem;

    private TriggerSystem m_TriggerSystem;

    [DebugWatchValue]
    private NativeValue<int> m_DebugBirth;

    private NativeCounter m_DebugBirthCounter;

    private EntityQuery m_CitizenQuery;

    private EntityQuery m_CitizenPrefabQuery;

    public int m_BirthChance = 20;

    private TypeHandle __TypeHandle;

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 262144 / (kUpdatesPerDay * 16);
    }

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_DebugBirthCounter = new NativeCounter(Allocator.Persistent);
        m_DebugBirth = new NativeValue<int>(Allocator.Persistent);
        m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
        m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
        m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
        m_CitizenQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<HouseholdMember>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadOnly<CurrentBuilding>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
        m_CitizenPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenData>(), ComponentType.ReadOnly<ArchetypeData>());
        RequireForUpdate(m_CitizenPrefabQuery);
        RequireForUpdate(m_CitizenQuery);
    }

    [Preserve]
    protected override void OnDestroy()
    {
        base.OnDestroy();
        m_DebugBirthCounter.Dispose();
        m_DebugBirth.Dispose();
    }

    [Preserve]
    protected override void OnUpdate()
    {
        uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
        __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Student_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
        CheckBirthJob checkBirthJob = default(CheckBirthJob);
        checkBirthJob.m_DebugBirthCounter = m_DebugBirthCounter.ToConcurrent();
        checkBirthJob.m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
        checkBirthJob.m_CitizenType = __TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle;
        checkBirthJob.m_MemberType = __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;
        checkBirthJob.m_UpdateFrameType = __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
        checkBirthJob.m_Citizens = __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup;
        checkBirthJob.m_HouseholdCitizens = __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup;
        checkBirthJob.m_Students = __TypeHandle.__Game_Citizens_Student_RO_ComponentLookup;
        checkBirthJob.m_PropertyRenters = __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup;
        checkBirthJob.m_CitizenPrefabArchetypes = m_CitizenPrefabQuery.ToComponentDataListAsync<ArchetypeData>(base.World.UpdateAllocator.ToAllocator, out var outJobHandle);
        checkBirthJob.m_CitizenPrefabs = m_CitizenPrefabQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle2);
        checkBirthJob.m_BirthChance = m_BirthChance;
        checkBirthJob.m_RandomSeed = RandomSeed.Next();
        checkBirthJob.m_UpdateFrameIndex = updateFrame;
        checkBirthJob.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();
        checkBirthJob.m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out var deps).AsParallelWriter();
        CheckBirthJob jobData = checkBirthJob;
        base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CitizenQuery, JobUtils.CombineDependencies(base.Dependency, deps, outJobHandle2, outJobHandle));
        m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
        m_TriggerSystem.AddActionBufferWriter(base.Dependency);
        m_CityStatisticsSystem.AddWriter(base.Dependency);
        SumBirthJob sumBirthJob = default(SumBirthJob);
        sumBirthJob.m_DebugBirth = m_DebugBirth;
        sumBirthJob.m_DebugBirthCount = m_DebugBirthCounter;
        SumBirthJob jobData2 = sumBirthJob;
        base.Dependency = IJobExtensions.Schedule(jobData2, base.Dependency);
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
    public BirthSystem()
    {
    }
}
