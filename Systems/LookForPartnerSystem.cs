using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Citizens;
using Game.Common;
using Game.Debug;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class LookForPartnerSystem : GameSystemBase
{
    [BurstCompile]
    private struct AddPartnerSeekerJob : IJob
    {
        public NativeValue<int> m_DebugLookingForPartner;

        public Entity m_LookingForPartnerEntity;

        public BufferLookup<LookingForPartner> m_LookingForPartners;

        public NativeQueue<LookingForPartner> m_Queue;

        public void Execute()
        {
            m_DebugLookingForPartner.value = m_Queue.Count;
            DynamicBuffer<LookingForPartner> dynamicBuffer = m_LookingForPartners[m_LookingForPartnerEntity];
            LookingForPartner item;
            while (m_Queue.TryDequeue(out item))
            {
                dynamicBuffer.Add(item);
            }
        }
    }

    [BurstCompile]
    private struct LookForPartnerJob : IJobChunk
    {
        [NativeDisableContainerSafetyRestriction]
        public NativeQueue<int> m_DebugLookForPartnerQueue;

        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

        [ReadOnly]
        public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;

        [ReadOnly]
        public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<Citizen> m_Citizens;

        [ReadOnly]
        public ComponentLookup<HealthProblem> m_HealthProblems;

        [ReadOnly]
        public ComponentLookup<TouristHousehold> m_Tourists;

        [ReadOnly]
        public ComponentLookup<CommuterHousehold> m_Commuters;

        public NativeQueue<LookingForPartner>.ParallelWriter m_Queue;

        public RandomSeed m_RandomSeed;

        public uint m_UpdateFrameIndex;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
            {
                return;
            }
            Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
            NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
            NativeArray<HouseholdMember> nativeArray2 = chunk.GetNativeArray(ref m_HouseholdMemberType);
            for (int i = 0; i < nativeArray.Length; i++)
            {
                Entity entity = nativeArray[i];
                Citizen value = m_Citizens[entity];
                CitizenAge age = value.GetAge();
                if (age == CitizenAge.Child || age == CitizenAge.Teen || (value.m_State & CitizenFlags.LookingForPartner) != 0 || CitizenUtils.IsDead(entity, ref m_HealthProblems))
                {
                    continue;
                }
                Entity household = nativeArray2[i].m_Household;
                if (!m_HouseholdCitizens.HasBuffer(household) || m_Tourists.HasComponent(household) || m_Commuters.HasComponent(household))
                {
                    continue;
                }
                DynamicBuffer<HouseholdCitizen> dynamicBuffer = m_HouseholdCitizens[household];
                int num = 0;
                for (int j = 0; j < dynamicBuffer.Length; j++)
                {
                    Entity citizen = dynamicBuffer[j].m_Citizen;
                    CitizenAge age2 = m_Citizens[citizen].GetAge();
                    if (age2 == CitizenAge.Adult || age2 == CitizenAge.Elderly)
                    {
                        num++;
                    }
                }
                if (num < 2 && random.NextInt(1000) < 80)
                {
                    int num2 = value.GetPseudoRandom(CitizenPseudoRandom.PartnerType).NextInt(100);
                    PartnerType partnerType = ((num2 < 4) ? PartnerType.Same : ((num2 >= 10) ? PartnerType.Other : PartnerType.Any));
                    m_Queue.Enqueue(new LookingForPartner
                    {
                        m_Citizen = entity,
                        m_PartnerType = partnerType
                    });
                    value.m_State |= CitizenFlags.LookingForPartner;
                    m_Citizens[entity] = value;
                    if (m_DebugLookForPartnerQueue.IsCreated)
                    {
                        m_DebugLookForPartnerQueue.Enqueue(1);
                    }
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

        public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

        public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RW_ComponentTypeHandle;

        public ComponentLookup<Citizen> __Game_Citizens_Citizen_RW_ComponentLookup;

        [ReadOnly]
        public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

        [ReadOnly]
        public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<CommuterHousehold> __Game_Citizens_CommuterHousehold_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;

        public BufferLookup<LookingForPartner> __Game_Citizens_LookingForPartner_RW_BufferLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
            __Game_Citizens_HouseholdMember_RW_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>();
            __Game_Citizens_Citizen_RW_ComponentLookup = state.GetComponentLookup<Citizen>();
            __Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
            __Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
            __Game_Citizens_CommuterHousehold_RO_ComponentLookup = state.GetComponentLookup<CommuterHousehold>(isReadOnly: true);
            __Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(isReadOnly: true);
            __Game_Citizens_LookingForPartner_RW_BufferLookup = state.GetBufferLookup<LookingForPartner>();
        }
    }

    public static readonly int kUpdatesPerDay = 4;

    private SimulationSystem m_SimulationSystem;

    private EntityQuery m_CitizenQuery;

    private EntityQuery m_LookingQuery;

    private NativeQueue<LookingForPartner> m_Queue;

    [DebugWatchValue]
    private NativeValue<int> m_DebugLookingForPartner;

    private TypeHandle __TypeHandle;

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 262144 / (kUpdatesPerDay * 16);
    }

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_DebugLookingForPartner = new NativeValue<int>(Allocator.Persistent);
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
        m_CitizenQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[1] { ComponentType.ReadOnly<Citizen>() },
            None = new ComponentType[2]
            {
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Temp>()
            }
        });
        m_LookingQuery = GetEntityQuery(ComponentType.ReadOnly<LookingForPartner>());
        m_Queue = new NativeQueue<LookingForPartner>(Allocator.Persistent);
        RequireForUpdate(m_CitizenQuery);
    }

    [Preserve]
    protected override void OnDestroy()
    {
        base.OnDestroy();
        m_Queue.Dispose();
        m_DebugLookingForPartner.Dispose();
    }

    [Preserve]
    protected override void OnUpdate()
    {
        NativeQueue<int> debugLookForPartnerQueue = default(NativeQueue<int>);
        uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
        __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_CommuterHousehold_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Citizen_RW_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HouseholdMember_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
        LookForPartnerJob lookForPartnerJob = default(LookForPartnerJob);
        lookForPartnerJob.m_DebugLookForPartnerQueue = debugLookForPartnerQueue;
        lookForPartnerJob.m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
        lookForPartnerJob.m_UpdateFrameType = __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
        lookForPartnerJob.m_HouseholdMemberType = __TypeHandle.__Game_Citizens_HouseholdMember_RW_ComponentTypeHandle;
        lookForPartnerJob.m_Citizens = __TypeHandle.__Game_Citizens_Citizen_RW_ComponentLookup;
        lookForPartnerJob.m_HouseholdCitizens = __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup;
        lookForPartnerJob.m_HealthProblems = __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup;
        lookForPartnerJob.m_Commuters = __TypeHandle.__Game_Citizens_CommuterHousehold_RO_ComponentLookup;
        lookForPartnerJob.m_Tourists = __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup;
        lookForPartnerJob.m_RandomSeed = RandomSeed.Next();
        lookForPartnerJob.m_UpdateFrameIndex = updateFrame;
        lookForPartnerJob.m_Queue = m_Queue.AsParallelWriter();
        LookForPartnerJob jobData = lookForPartnerJob;
        base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CitizenQuery, base.Dependency);
        __TypeHandle.__Game_Citizens_LookingForPartner_RW_BufferLookup.Update(ref base.CheckedStateRef);
        AddPartnerSeekerJob addPartnerSeekerJob = default(AddPartnerSeekerJob);
        addPartnerSeekerJob.m_DebugLookingForPartner = m_DebugLookingForPartner;
        addPartnerSeekerJob.m_LookingForPartners = __TypeHandle.__Game_Citizens_LookingForPartner_RW_BufferLookup;
        addPartnerSeekerJob.m_LookingForPartnerEntity = m_LookingQuery.GetSingletonEntity();
        addPartnerSeekerJob.m_Queue = m_Queue;
        AddPartnerSeekerJob jobData2 = addPartnerSeekerJob;
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
    public LookForPartnerSystem()
    {
    }
}
