using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Collections;
using Game;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Debug;
using Game.Prefabs;
using Game.Tools;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace RealPop.Systems;

[CompilerGenerated]
public class AgingSystem_RealPop : GameSystemBase
{
    [BurstCompile]
    private struct MoveFromHomeJob : IJob
    {
        public NativeQueue<Entity> m_MoveFromHomeQueue;

        public EntityCommandBuffer m_CommandBuffer;

        [ReadOnly]
        public ComponentLookup<Household> m_Households;

        public ComponentLookup<HouseholdMember> m_HouseholdMembers;

        public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

        [ReadOnly]
        public ComponentLookup<ArchetypeData> m_ArchetypeDatas;

        [ReadOnly]
        public NativeList<Entity> m_HouseholdPrefabs;

        public RandomSeed m_RandomSeed;

        public NativeCounter m_BecomeTeenCounter;

        public NativeCounter m_BecomeAdultCounter;

        public NativeCounter m_BecomeElderCounter;

        public NativeValue<int> m_BecomeTeen;

        public NativeValue<int> m_BecomeAdult;

        public NativeValue<int> m_BecomeElder;

        public void Execute()
        {
            m_BecomeTeen.value = m_BecomeTeenCounter.Count;
            m_BecomeAdult.value = m_BecomeAdultCounter.Count;
            m_BecomeElder.value = m_BecomeElderCounter.Count;
            Random random = m_RandomSeed.GetRandom(62347);
            Entity item;
            while (m_MoveFromHomeQueue.TryDequeue(out item))
            {
                Entity entity = m_HouseholdPrefabs[random.NextInt(m_HouseholdPrefabs.Length)];
                ArchetypeData archetypeData = m_ArchetypeDatas[entity];
                HouseholdMember component = m_HouseholdMembers[item];
                Entity household = component.m_Household;
                if (!m_HouseholdCitizens.HasBuffer(household))
                {
                    continue;
                }
                DynamicBuffer<HouseholdCitizen> dynamicBuffer = m_HouseholdCitizens[household];
                if (dynamicBuffer.Length <= 1)
                {
                    continue;
                }
                Household household2 = m_Households[household];
                Entity entity2 = m_CommandBuffer.CreateEntity(archetypeData.m_Archetype);
                m_CommandBuffer.SetComponent(entity2, new Household
                {
                    m_Flags = household2.m_Flags,
                    m_Resources = 1000
                });
                m_CommandBuffer.AddComponent(entity2, default(PropertySeeker));
                m_CommandBuffer.SetComponent(entity2, new PrefabRef
                {
                    m_Prefab = entity
                });
                component.m_Household = entity2;
                m_CommandBuffer.SetComponent(item, component);
                m_CommandBuffer.SetBuffer<HouseholdCitizen>(entity2).Add(new HouseholdCitizen
                {
                    m_Citizen = item
                });
                for (int i = 0; i < dynamicBuffer.Length; i++)
                {
                    if (dynamicBuffer[i].m_Citizen == item)
                    {
                        dynamicBuffer.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }

    [BurstCompile]
    private struct AgingJob : IJobChunk
    {
        public NativeCounter.Concurrent m_BecomeTeenCounter;

        public NativeCounter.Concurrent m_BecomeAdultCounter;

        public NativeCounter.Concurrent m_BecomeElderCounter;

        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        public ComponentTypeHandle<Citizen> m_CitizenType;

        [ReadOnly]
        public ComponentTypeHandle<Game.Citizens.Student> m_StudentType;

        [ReadOnly]
        public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

        [ReadOnly]
        public BufferLookup<Game.Buildings.Student> m_Students;

        [ReadOnly]
        public ComponentLookup<TravelPurpose> m_Purposes;

        public TimeData m_TimeData;

        public NativeQueue<Entity>.ParallelWriter m_MoveFromHomeQueue;

        public uint m_SimulationFrame;

        public uint m_UpdateFrameIndex;

        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

        public bool m_DebugAgeAllCitizens;

        private void LeaveSchool(int chunkIndex, int i, Entity student, NativeArray<Game.Citizens.Student> students)
        {
            Entity school = students[i].m_School;
            m_CommandBuffer.RemoveComponent<Game.Citizens.Student>(chunkIndex, student);
            if (m_Students.HasBuffer(school))
            {
                m_CommandBuffer.AddComponent<StudentsRemoved>(chunkIndex, school);
            }
        }

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            if (!m_DebugAgeAllCitizens && chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
            {
                return;
            }
            NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
            NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray(ref m_CitizenType);
            NativeArray<Game.Citizens.Student> nativeArray3 = chunk.GetNativeArray(ref m_StudentType);
            int day = TimeSystem.GetDay(m_SimulationFrame, m_TimeData);
            //RealPop.Debug.Log($"aging loop for {nativeArray.Length} people");
            for (int i = 0; i < nativeArray.Length; i++)
            {
                Citizen value = nativeArray2[i];
                CitizenAge age = value.GetAge();
                int num = day - value.m_BirthDay;
                int num2;
                if (age == CitizenAge.Child)
                {
                    num2 = GetTeenAgeLimitInDays();
                }
                else if (age == CitizenAge.Teen)
                {
                    num2 = GetAdultAgeLimitInDays();
                }
                else
                {
                    if (age != CitizenAge.Adult)
                    {
                        continue;
                    }
                    num2 = GetElderAgeLimitInDays();
                }
                if (num < num2)
                {
                    //RealPop.Debug.Log($"{age} {num}: not aging {num} < {num2}");
                    continue;
                }
                Entity entity = nativeArray[i];
                switch (age)
                {
                case CitizenAge.Child:
                    if (chunk.Has(ref m_StudentType))
                    {
                        LeaveSchool(unfilteredChunkIndex, i, entity, nativeArray3);
                    }
                    m_BecomeTeenCounter.Increment();
                    value.SetAge(CitizenAge.Teen);
                    nativeArray2[i] = value;
                    break;
                case CitizenAge.Teen:
                    if (chunk.Has(ref m_StudentType))
                    {
                        LeaveSchool(unfilteredChunkIndex, i, entity, nativeArray3);
                    }
                    value.SetAge(CitizenAge.Adult);
                    value.m_State |= CitizenFlags.NeedsNewJob;
                    nativeArray2[i] = value;
                    m_MoveFromHomeQueue.Enqueue(entity);
                    m_BecomeAdultCounter.Increment();
                    break;
                case CitizenAge.Adult:
                    m_BecomeElderCounter.Increment();
                    if (m_Purposes.HasComponent(entity) && (m_Purposes[entity].m_Purpose == Purpose.GoingToWork || m_Purposes[entity].m_Purpose == Purpose.Working))
                    {
                        m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, entity);
                    }
                    m_CommandBuffer.RemoveComponent<Worker>(unfilteredChunkIndex, entity);
                    value.SetAge(CitizenAge.Elderly);
                    nativeArray2[i] = value;
                    break;
                }
                //RealPop.Debug.Log($"ent {entity.Index} is {num} days old, now {value.GetAge()}");
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

        public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RW_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentTypeHandle;

        public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

        [ReadOnly]
        public BufferLookup<Game.Buildings.Student> __Game_Buildings_Student_RO_BufferLookup;

        [ReadOnly]
        public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

        public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RW_ComponentLookup;

        public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RW_BufferLookup;

        [ReadOnly]
        public ComponentLookup<ArchetypeData> __Game_Prefabs_ArchetypeData_RO_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Citizens_Citizen_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>();
            __Game_Citizens_Student_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Citizens.Student>(isReadOnly: true);
            __Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
            __Game_Buildings_Student_RO_BufferLookup = state.GetBufferLookup<Game.Buildings.Student>(isReadOnly: true);
            __Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(isReadOnly: true);
            __Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
            __Game_Citizens_HouseholdMember_RW_ComponentLookup = state.GetComponentLookup<HouseholdMember>();
            __Game_Citizens_HouseholdCitizen_RW_BufferLookup = state.GetBufferLookup<HouseholdCitizen>();
            __Game_Prefabs_ArchetypeData_RO_ComponentLookup = state.GetComponentLookup<ArchetypeData>(isReadOnly: true);
        }
    }

    public static readonly int kUpdatesPerDay = 1;

    private EntityQuery m_CitizenGroup;

    private EntityQuery m_TimeDataQuery;

    private EntityQuery m_HouseholdPrefabQuery;

    private SimulationSystem m_SimulationSystem;

    private EndFrameBarrier m_EndFrameBarrier;

    private NativeQueue<Entity> m_MoveFromHomeQueue;

    public static bool s_DebugAgeAllCitizens = false;

    [DebugWatchValue]
    public NativeValue<int> m_BecomeTeen;

    [DebugWatchValue]
    public NativeValue<int> m_BecomeAdult;

    [DebugWatchValue]
    public NativeValue<int> m_BecomeElder;

    public NativeCounter m_BecomeTeenCounter;

    public NativeCounter m_BecomeAdultCounter;

    public NativeCounter m_BecomeElderCounter;

    private TypeHandle __TypeHandle;

    private static int s_TeenAgeLimitInDays;
    private static int s_AdultAgeLimitInDays;
    private static int s_ElderAgeLimitInDays;

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 262144 / (kUpdatesPerDay * 16);
    }

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
        m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
        m_CitizenGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[1] { ComponentType.ReadOnly<Citizen>() },
            None = new ComponentType[2]
            {
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Temp>()
            }
        });
        m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
        m_HouseholdPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ArchetypeData>(), ComponentType.ReadOnly<HouseholdData>(), ComponentType.ReadOnly<DynamicHousehold>());
        m_MoveFromHomeQueue = new NativeQueue<Entity>(Allocator.Persistent);
        m_BecomeTeen = new NativeValue<int>(Allocator.Persistent);
        m_BecomeAdult = new NativeValue<int>(Allocator.Persistent);
        m_BecomeElder = new NativeValue<int>(Allocator.Persistent);
        m_BecomeTeenCounter = new NativeCounter(Allocator.Persistent);
        m_BecomeAdultCounter = new NativeCounter(Allocator.Persistent);
        m_BecomeElderCounter = new NativeCounter(Allocator.Persistent);
        RequireForUpdate(m_CitizenGroup);
        // RealPop
        s_TeenAgeLimitInDays = Plugin.TeenAgeLimitInDays.Value;
        s_AdultAgeLimitInDays = Plugin.AdultAgeLimitInDays.Value;
        s_ElderAgeLimitInDays = Plugin.ElderAgeLimitInDays.Value;
        Plugin.Log($"Modded AgingSystem created. Age thresholds: {s_TeenAgeLimitInDays}, {s_AdultAgeLimitInDays}, {s_ElderAgeLimitInDays}.");
    }

    [Preserve]
    protected override void OnDestroy()
    {
        base.OnDestroy();
        m_MoveFromHomeQueue.Dispose();
        m_BecomeTeen.Dispose();
        m_BecomeAdult.Dispose();
        m_BecomeElder.Dispose();
        m_BecomeTeenCounter.Dispose();
        m_BecomeAdultCounter.Dispose();
        m_BecomeElderCounter.Dispose();
    }

    public static int GetTeenAgeLimitInDays()
    {
        return s_TeenAgeLimitInDays; // default 21
    }

    public static int GetAdultAgeLimitInDays()
    {
        return s_AdultAgeLimitInDays; // default 36
    }

    public static int GetElderAgeLimitInDays()
    {
        return s_ElderAgeLimitInDays; // default 84
    }

    [Preserve]
    protected override void OnUpdate()
    {
        //UnityEngine.Debug.Log("AgingSystem_RealPop.OnUpdate");
        uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
        __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Buildings_Student_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Student_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
        AgingJob agingJob = default(AgingJob);
        agingJob.m_BecomeTeenCounter = m_BecomeTeenCounter.ToConcurrent();
        agingJob.m_BecomeAdultCounter = m_BecomeAdultCounter.ToConcurrent();
        agingJob.m_BecomeElderCounter = m_BecomeElderCounter.ToConcurrent();
        agingJob.m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
        agingJob.m_CitizenType = __TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle;
        agingJob.m_StudentType = __TypeHandle.__Game_Citizens_Student_RO_ComponentTypeHandle;
        agingJob.m_UpdateFrameType = __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
        agingJob.m_Students = __TypeHandle.__Game_Buildings_Student_RO_BufferLookup;
        agingJob.m_Purposes = __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup;
        agingJob.m_MoveFromHomeQueue = m_MoveFromHomeQueue.AsParallelWriter();
        agingJob.m_SimulationFrame = m_SimulationSystem.frameIndex;
        agingJob.m_TimeData = m_TimeDataQuery.GetSingleton<TimeData>();
        agingJob.m_UpdateFrameIndex = updateFrame;
        agingJob.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();
        agingJob.m_DebugAgeAllCitizens = s_DebugAgeAllCitizens;
        AgingJob jobData = agingJob;
        base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CitizenGroup, base.Dependency);
        m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
        __TypeHandle.__Game_Prefabs_ArchetypeData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HouseholdCitizen_RW_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HouseholdMember_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        MoveFromHomeJob moveFromHomeJob = default(MoveFromHomeJob);
        moveFromHomeJob.m_Households = __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup;
        moveFromHomeJob.m_HouseholdMembers = __TypeHandle.__Game_Citizens_HouseholdMember_RW_ComponentLookup;
        moveFromHomeJob.m_HouseholdCitizens = __TypeHandle.__Game_Citizens_HouseholdCitizen_RW_BufferLookup;
        moveFromHomeJob.m_ArchetypeDatas = __TypeHandle.__Game_Prefabs_ArchetypeData_RO_ComponentLookup;
        moveFromHomeJob.m_HouseholdPrefabs = m_HouseholdPrefabQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle);
        moveFromHomeJob.m_RandomSeed = RandomSeed.Next();
        moveFromHomeJob.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
        moveFromHomeJob.m_MoveFromHomeQueue = m_MoveFromHomeQueue;
        moveFromHomeJob.m_BecomeTeen = m_BecomeTeen;
        moveFromHomeJob.m_BecomeAdult = m_BecomeAdult;
        moveFromHomeJob.m_BecomeElder = m_BecomeElder;
        moveFromHomeJob.m_BecomeTeenCounter = m_BecomeTeenCounter;
        moveFromHomeJob.m_BecomeAdultCounter = m_BecomeAdultCounter;
        moveFromHomeJob.m_BecomeElderCounter = m_BecomeElderCounter;
        MoveFromHomeJob jobData2 = moveFromHomeJob;
        base.Dependency = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(outJobHandle, base.Dependency));
        m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
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
    public AgingSystem_RealPop()
    {
    }
}
