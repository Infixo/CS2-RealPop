using System.Runtime.CompilerServices;
using Game;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace RealPop.Systems;

[CompilerGenerated]
public class ApplyToSchoolSystem_RealPop : GameSystemBase
{
    [BurstCompile]
    public struct ApplyToSchoolJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

        public ComponentTypeHandle<Citizen> m_CitizenType;

        [ReadOnly]
        public ComponentTypeHandle<Worker> m_WorkerType;

        [ReadOnly]
        public ComponentLookup<PrefabRef> m_Prefabs;

        [ReadOnly]
        public ComponentLookup<SchoolData> m_SchoolDatas;

        [ReadOnly]
        public ComponentLookup<HouseholdMember> m_HouseholdMembers;

        [ReadOnly]
        public ComponentLookup<Household> m_HouseholdDatas;

        [ReadOnly]
        public BufferLookup<Resources> m_Resources;

        [ReadOnly]
        public ComponentLookup<PropertyRenter> m_PropertyRenters;

        [ReadOnly]
        public BufferLookup<CityModifier> m_CityModifiers;

        [ReadOnly]
        public BufferLookup<ServiceFee> m_Fees;

        [ReadOnly]
        public ComponentLookup<TouristHousehold> m_TouristHouseholds;

        [ReadOnly]
        public RandomSeed m_RandomSeed;

        public uint m_UpdateFrameIndex;

        public Entity m_City;

        public uint m_SimulationFrame;

        public EconomyParameterData m_EconomyParameters;

        public TimeData m_TimeData;

        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
            {
                return;
            }
            //RealPop.Debug.Log($"ApplyToSchoolJob.Execute: frame {m_UpdateFrameIndex} count {chunk.Count}");
            NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
            NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray(ref m_CitizenType);
            NativeArray<Worker> nativeArray3 = chunk.GetNativeArray(ref m_WorkerType);
            DynamicBuffer<CityModifier> dynamicBuffer = m_CityModifiers[m_City];
            Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
            //int day = TimeSystem.GetDay(m_SimulationFrame, m_TimeData);
            for (int i = 0; i < chunk.Count; i++)
            {
                Citizen citizen = nativeArray2[i];
                CitizenAge age = citizen.GetAge();
                if (age == CitizenAge.Elderly)
                {
                    continue;
                }
                //int ageInDays = day - citizen.m_BirthDay;
                int num = random.NextInt(100);
                int num2 = random.NextInt(100);
                int num3 = ((age == CitizenAge.Child) ? 1 : (citizen.GetEducationLevel() + 1));
                int failedEducationCount = citizen.GetFailedEducationCount();
                // Infixo: removes College skipping and going directly to University; idk if this is a bug or intended
                // Dev Diaries say nothing about such situation however this code didn't write itself
                // hard to say what was the intention here
                //if (failedEducationCount == 0 && num3 == 3)
                //{
                    //num3 = 4;
                //}
                // Infixo: removes Teens going to University, Dev Diaries say that only Adults are allowed to go to University
                //bool num4 = age == CitizenAge.Child || (age == CitizenAge.Teen && num3 >= 2) || (age == CitizenAge.Adult && num3 >= 3);
                bool num4 = age == CitizenAge.Child || (age == CitizenAge.Teen && num3 >= 2 && num3 < 4) || (age == CitizenAge.Adult && num3 >= 3);
                Entity household = m_HouseholdMembers[nativeArray[i]].m_Household;
                //string logmsg = $"{age} {ageInDays} edu {num3} failed {failedEducationCount}";
                if (!num4 || !m_HouseholdDatas.HasComponent(household) || (m_HouseholdDatas[household].m_Flags & HouseholdFlags.MovedIn) == 0 || failedEducationCount >= 3 || num3 < 1 || num3 > 4)
                {
                    //Plugin.Logger.LogInfo(logmsg + $" NOENTRY flags {m_HouseholdDatas[household].m_Flags} ");
                    continue;
                }
                int householdWorth = EconomyUtils.GetHouseholdWorth(household, m_HouseholdDatas[household], m_Resources[household]);
                float fee = ServiceFeeSystem.GetFee(ServiceFeeSystem.GetEducationResource(num3), m_Fees[m_City]);
                SchoolData schoolData = default(SchoolData);
                float efficiency = 1f;
                float dropoutProbability = GraduationSystem_RealPop.GetDropoutProbability(citizen, num3, 0f, fee, householdWorth, m_SimulationFrame, ref m_EconomyParameters, schoolData, dynamicBuffer, efficiency, m_TimeData);
                float willingness = citizen.GetPseudoRandom(CitizenPseudoRandom.StudyWillingness).NextFloat();
                float enteringProbability = GetEnteringProbability(age, nativeArray3.IsCreated, num3, citizen.m_WellBeing, willingness, dynamicBuffer);
                //RealPop.Debug.Log($"ApplyToSchool: prob {enteringProbability} lev {num3} age {age} work {nativeArray3.IsCreated} well {citizen.m_WellBeing} will {willingness}");
                if ((float)num2 > 100f * dropoutProbability && (float)num < 100f * enteringProbability)
                {
                    //Plugin.Logger.LogInfo(logmsg + $" APPLIED prob {enteringProbability}");
                    if (m_PropertyRenters.HasComponent(household) && !m_TouristHouseholds.HasComponent(household))
                    {
                        Entity property = m_PropertyRenters[household].m_Property;
                        Entity entity = m_CommandBuffer.CreateEntity(unfilteredChunkIndex);
                        m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new Owner
                        {
                            m_Owner = nativeArray[i]
                        });
                        m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new SchoolSeeker
                        {
                            m_Level = num3
                        });
                        m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new CurrentBuilding
                        {
                            m_CurrentBuilding = property
                        });
                        m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[i], new HasSchoolSeeker
                        {
                            m_Seeker = entity
                        });
                    }
                }
                else
                {
                    //Plugin.Logger.LogInfo(logmsg + $" DENIED prob {enteringProbability}");
                    citizen.SetFailedEducationCount(math.min(3, failedEducationCount + 1));
                    nativeArray2[i] = citizen;
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
        public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

        public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RW_ComponentTypeHandle;

        [ReadOnly]
        public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Worker> __Game_Citizens_Worker_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

        [ReadOnly]
        public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<SchoolData> __Game_Prefabs_SchoolData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<Resources> __Game_Economy_Resources_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<ServiceFee> __Game_City_ServiceFee_RO_BufferLookup;

        [ReadOnly]
        public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
            __Game_Citizens_Citizen_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>();
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Citizens_Worker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Worker>(isReadOnly: true);
            __Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
            __Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
            __Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
            __Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
            __Game_Prefabs_SchoolData_RO_ComponentLookup = state.GetComponentLookup<SchoolData>(isReadOnly: true);
            __Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
            __Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Resources>(isReadOnly: true);
            __Game_City_ServiceFee_RO_BufferLookup = state.GetBufferLookup<ServiceFee>(isReadOnly: true);
            __Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(isReadOnly: true);
        }
    }

    public const uint UPDATE_INTERVAL = 8192u;

    private EntityQuery m_CitizenGroup;

    private SimulationSystem m_SimulationSystem;

    private EndFrameBarrier m_EndFrameBarrier;

    private CitySystem m_CitySystem;

    private TypeHandle __TypeHandle;

    private EntityQuery __query_2069025488_0;

    private EntityQuery __query_2069025488_1;

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 512;
    }

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
        m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
        m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
        m_CitizenGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[2]
            {
                ComponentType.ReadWrite<Citizen>(),
                ComponentType.ReadOnly<UpdateFrame>()
            },
            None = new ComponentType[5]
            {
                ComponentType.ReadOnly<HealthProblem>(),
                ComponentType.ReadOnly<HasJobSeeker>(),
                ComponentType.ReadOnly<HasSchoolSeeker>(),
                ComponentType.ReadOnly<Game.Citizens.Student>(),
                ComponentType.ReadOnly<Deleted>()
            }
        });
        RequireForUpdate(m_CitizenGroup);
        RequireForUpdate<EconomyParameterData>();
        RequireForUpdate<TimeData>();
        Plugin.Logger.LogInfo("Modded ApplyToSchoolSystem created.");
    }

    [Preserve]
    protected override void OnUpdate()
    {
        uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
        //RealPop.Debug.Log($"Updating interval {updateFrameWithInterval} frame {m_SimulationSystem.frameIndex}");
        __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_City_ServiceFee_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Economy_Resources_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_SchoolData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_City_CityModifier_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref base.CheckedStateRef);
        ApplyToSchoolJob applyToSchoolJob = default(ApplyToSchoolJob);
        applyToSchoolJob.m_UpdateFrameType = __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
        applyToSchoolJob.m_CitizenType = __TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle;
        applyToSchoolJob.m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
        applyToSchoolJob.m_WorkerType = __TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle;
        applyToSchoolJob.m_HouseholdMembers = __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup;
        applyToSchoolJob.m_PropertyRenters = __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup;
        applyToSchoolJob.m_CityModifiers = __TypeHandle.__Game_City_CityModifier_RO_BufferLookup;
        applyToSchoolJob.m_Prefabs = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
        applyToSchoolJob.m_SchoolDatas = __TypeHandle.__Game_Prefabs_SchoolData_RO_ComponentLookup;
        applyToSchoolJob.m_HouseholdDatas = __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup;
        applyToSchoolJob.m_Resources = __TypeHandle.__Game_Economy_Resources_RO_BufferLookup;
        applyToSchoolJob.m_Fees = __TypeHandle.__Game_City_ServiceFee_RO_BufferLookup;
        applyToSchoolJob.m_TouristHouseholds = __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup;
        applyToSchoolJob.m_RandomSeed = RandomSeed.Next();
        applyToSchoolJob.m_SimulationFrame = m_SimulationSystem.frameIndex;
        applyToSchoolJob.m_EconomyParameters = __query_2069025488_0.GetSingleton<EconomyParameterData>();
        applyToSchoolJob.m_TimeData = __query_2069025488_1.GetSingleton<TimeData>();
        applyToSchoolJob.m_City = m_CitySystem.City;
        applyToSchoolJob.m_UpdateFrameIndex = updateFrameWithInterval;
        applyToSchoolJob.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();
        ApplyToSchoolJob jobData = applyToSchoolJob;
        base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CitizenGroup, base.Dependency);
        m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
    }

    public static float GetEnteringProbability(CitizenAge age, bool worker, int level, int wellbeing, float willingness, DynamicBuffer<CityModifier> cityModifiers)
    {
        //UnityEngine.Debug.Log($"GetEnteringProbability: lev {level} age {age} work {worker} well {wellbeing} will {willingness}");
        if (level == 1)
        {
            if (age != 0)
            {
                return 0f;
            }
            return 1f;
        }
        if (age == CitizenAge.Child || age == CitizenAge.Elderly)
        {
            return 0f;
        }
        float num = (float)wellbeing / 100f * (0.5f + willingness);
        switch (level)
        {
        case 2:
            if (age != CitizenAge.Teen)
            {
                return 0f;
            }
            return 0.77f * (worker ? 0.9f : 1f) * math.log(2.6f * num + 1f);
        case 3:
            return 0.5f * (worker ? 0.75f : 1f) * math.log(1.6f * num + 1f);
        case 4:
        {
            float value = 0.3f * (worker ? 0.6f : 1f) * num;
            CityUtils.ApplyModifier(ref value, cityModifiers, CityModifierType.UniversityInterest);
            return value;
        }
        default:
            return 0f;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void __AssignQueries(ref SystemState state)
    {
        __query_2069025488_0 = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[1] { ComponentType.ReadOnly<EconomyParameterData>() },
            Any = new ComponentType[0],
            None = new ComponentType[0],
            Disabled = new ComponentType[0],
            Absent = new ComponentType[0],
            Options = EntityQueryOptions.IncludeSystems
        });
        __query_2069025488_1 = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[1] { ComponentType.ReadOnly<TimeData>() },
            Any = new ComponentType[0],
            None = new ComponentType[0],
            Disabled = new ComponentType[0],
            Absent = new ComponentType[0],
            Options = EntityQueryOptions.IncludeSystems
        });
    }

    protected override void OnCreateForCompiler()
    {
        base.OnCreateForCompiler();
        __AssignQueries(ref base.CheckedStateRef);
        __TypeHandle.__AssignHandles(ref base.CheckedStateRef);
    }

    [Preserve]
    public ApplyToSchoolSystem_RealPop()
    {
    }
}
