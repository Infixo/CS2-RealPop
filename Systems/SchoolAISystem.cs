using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace RealPop.Systems;

[CompilerGenerated]
public class SchoolAISystem_RealPop : GameSystemBase
{
    [BurstCompile]
    private struct SchoolTickJob : IJobChunk
    {
        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

        [ReadOnly]
        public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

        [ReadOnly]
        public BufferTypeHandle<Efficiency> m_EfficiencyType;

        public ComponentTypeHandle<Game.Buildings.School> m_SchoolType;

        public BufferTypeHandle<Game.Buildings.Student> m_StudentType;

        [ReadOnly]
        public ComponentLookup<PrefabRef> m_Prefabs;

        [ReadOnly]
        public ComponentLookup<SchoolData> m_SchoolDatas;

        [ReadOnly]
        public ComponentLookup<Game.Citizens.Student> m_Students;

        [ReadOnly]
        public ComponentLookup<Citizen> m_Citizens;

        [ReadOnly]
        public ComponentLookup<TravelPurpose> m_TravelPurposes;

        [ReadOnly]
        public BufferLookup<CityModifier> m_CityModifiers;

        [ReadOnly]
        public BufferLookup<ServiceFee> m_Fees;

        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

        public EconomyParameterData m_EconomyParameters;

        public EducationParameterData m_EducationParameters;

        public TimeData m_TimeData;

        public RandomSeed m_RandomSeed;

        public Entity m_City;

        public uint m_SimulationFrame;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            DynamicBuffer<CityModifier> modifiers = m_CityModifiers[m_City];
            NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
            BufferAccessor<InstalledUpgrade> bufferAccessor = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
            BufferAccessor<Efficiency> bufferAccessor2 = chunk.GetBufferAccessor(ref m_EfficiencyType);
            NativeArray<Game.Buildings.School> nativeArray2 = chunk.GetNativeArray(ref m_SchoolType);
            BufferAccessor<Game.Buildings.Student> bufferAccessor3 = chunk.GetBufferAccessor(ref m_StudentType);
            Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
            for (int i = 0; i < chunk.Count; i++)
            {
                Entity prefab = nativeArray[i].m_Prefab;
                ref Game.Buildings.School reference = ref nativeArray2.ElementAt(i);
                float efficiency = BuildingUtils.GetEfficiency(bufferAccessor2, i);
                m_SchoolDatas.TryGetComponent(prefab, out var componentData);
                if (bufferAccessor.Length != 0)
                {
                    UpgradeUtils.CombineStats(ref componentData, bufferAccessor[i], ref m_Prefabs, ref m_SchoolDatas);
                }
                float fee = ServiceFeeSystem.GetFee(ServiceFeeSystem.GetEducationResource(componentData.m_EducationLevel), m_Fees[m_City]);
                DynamicBuffer<Game.Buildings.Student> dynamicBuffer = bufferAccessor3[i];
                float num = 0f;
                float num2 = 0f;
                int num3 = 0;
                for (int num4 = dynamicBuffer.Length - 1; num4 >= 0; num4--)
                {
                    if (m_Students.TryGetComponent(dynamicBuffer[num4], out var componentData2) && m_Citizens.TryGetComponent(dynamicBuffer[num4], out var componentData3))
                    {
                        if (efficiency <= 0.001f && random.NextFloat() < m_EducationParameters.m_InoperableSchoolLeaveProbability)
                        {
                            LeaveSchool(unfilteredChunkIndex, dynamicBuffer[num4]);
                            dynamicBuffer.RemoveAt(num4);
                        }
                        else
                        {
                            int failedEducationCount = componentData3.GetFailedEducationCount();
                            float ageInDays = componentData3.GetAgeInDays(m_SimulationFrame, m_TimeData);
                            float graduationProbability = GraduationSystem_RealPop.GetGraduationProbability(componentData2.m_Level, componentData3.m_WellBeing, componentData, modifiers, 0.5f, efficiency);
                            if (graduationProbability > 0.001f)
                            {
                                num += math.min(4f, (float)failedEducationCount + 0.5f - 1f / math.log2(1f - math.saturate(graduationProbability))) / 1f;
                                float num5 = math.pow(1f - math.saturate(graduationProbability), 4f);
                                float num6 = math.saturate(GraduationSystem_RealPop.GetDropoutProbability(componentData2.m_Level, componentData2.m_LastCommuteTime, fee, 0, ageInDays, 0.5f, failedEducationCount, graduationProbability, ref m_EconomyParameters));
                                num2 += math.saturate(num5 + num6);
                            }
                            else
                            {
                                num += 4f;
                                num2 += 1f;
                            }
                            num3++;
                        }
                    }
                    else
                    {
                        dynamicBuffer.RemoveAt(num4);
                    }
                }
                if (num3 > 0)
                {
                    reference.m_AverageGraduationTime = num / (float)num3;
                    reference.m_AverageFailProbability = num2 / (float)num3;
                }
                else
                {
                    float graduationProbability2 = GraduationSystem_RealPop.GetGraduationProbability(componentData.m_EducationLevel, 50, componentData, modifiers, 0.5f, efficiency);
                    reference.m_AverageGraduationTime = 0.5f - 1f / math.log2(1f - math.saturate(graduationProbability2));
                    reference.m_AverageFailProbability = 0f;
                }
            }
        }

        private void LeaveSchool(int chunkIndex, Entity entity)
        {
            m_CommandBuffer.RemoveComponent<Game.Citizens.Student>(chunkIndex, entity);
            if (m_TravelPurposes.TryGetComponent(entity, out var componentData))
            {
                Purpose purpose = componentData.m_Purpose;
                if (purpose == Purpose.GoingToSchool || purpose == Purpose.Studying)
                {
                    m_CommandBuffer.RemoveComponent<TravelPurpose>(chunkIndex, entity);
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
        public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

        public ComponentTypeHandle<Game.Buildings.School> __Game_Buildings_School_RW_ComponentTypeHandle;

        public BufferTypeHandle<Game.Buildings.Student> __Game_Buildings_Student_RW_BufferTypeHandle;

        [ReadOnly]
        public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<SchoolData> __Game_Prefabs_SchoolData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<ServiceFee> __Game_City_ServiceFee_RO_BufferLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
            __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
            __Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
            __Game_Buildings_School_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.School>();
            __Game_Buildings_Student_RW_BufferTypeHandle = state.GetBufferTypeHandle<Game.Buildings.Student>();
            __Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
            __Game_Prefabs_SchoolData_RO_ComponentLookup = state.GetComponentLookup<SchoolData>(isReadOnly: true);
            __Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(isReadOnly: true);
            __Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
            __Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(isReadOnly: true);
            __Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
            __Game_City_ServiceFee_RO_BufferLookup = state.GetBufferLookup<ServiceFee>(isReadOnly: true);
        }
    }

    private SimulationSystem m_SimulationSystem;

    private CitySystem m_CitySystem;

    private EndFrameBarrier m_EndFrameBarrier;

    private EntityQuery m_SchoolQuery;

    private TypeHandle __TypeHandle;

    private EntityQuery __query_1235104412_0;

    private EntityQuery __query_1235104412_1;

    private EntityQuery __query_1235104412_2;

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 256;
    }

    public override int GetUpdateOffset(SystemUpdatePhase phase)
    {
        return 96;
    }

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
        m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
        m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
        m_SchoolQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Buildings.School>(), ComponentType.ReadWrite<Game.Buildings.Student>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
        RequireForUpdate(m_SchoolQuery);
        RequireForUpdate<EconomyParameterData>();
        RequireForUpdate<EducationParameterData>();
        RequireForUpdate<TimeData>();
        Plugin.Logger.LogInfo("Modded SchoolAISystem created.");
    }

    [Preserve]
    protected override void OnUpdate()
    {
        __TypeHandle.__Game_City_ServiceFee_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_City_CityModifier_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Student_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_SchoolData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Buildings_Student_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Buildings_School_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        SchoolTickJob schoolTickJob = default(SchoolTickJob);
        schoolTickJob.m_PrefabRefType = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
        schoolTickJob.m_InstalledUpgradeType = __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;
        schoolTickJob.m_EfficiencyType = __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle;
        schoolTickJob.m_SchoolType = __TypeHandle.__Game_Buildings_School_RW_ComponentTypeHandle;
        schoolTickJob.m_StudentType = __TypeHandle.__Game_Buildings_Student_RW_BufferTypeHandle;
        schoolTickJob.m_Prefabs = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
        schoolTickJob.m_SchoolDatas = __TypeHandle.__Game_Prefabs_SchoolData_RO_ComponentLookup;
        schoolTickJob.m_Students = __TypeHandle.__Game_Citizens_Student_RO_ComponentLookup;
        schoolTickJob.m_Citizens = __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup;
        schoolTickJob.m_TravelPurposes = __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup;
        schoolTickJob.m_CityModifiers = __TypeHandle.__Game_City_CityModifier_RO_BufferLookup;
        schoolTickJob.m_Fees = __TypeHandle.__Game_City_ServiceFee_RO_BufferLookup;
        schoolTickJob.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();
        schoolTickJob.m_EconomyParameters = __query_1235104412_0.GetSingleton<EconomyParameterData>();
        schoolTickJob.m_EducationParameters = __query_1235104412_1.GetSingleton<EducationParameterData>();
        schoolTickJob.m_TimeData = __query_1235104412_2.GetSingleton<TimeData>();
        schoolTickJob.m_RandomSeed = RandomSeed.Next();
        schoolTickJob.m_City = m_CitySystem.City;
        schoolTickJob.m_SimulationFrame = m_SimulationSystem.frameIndex;
        SchoolTickJob jobData = schoolTickJob;
        base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_SchoolQuery, base.Dependency);
        m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void __AssignQueries(ref SystemState state)
    {
        __query_1235104412_0 = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[1] { ComponentType.ReadOnly<EconomyParameterData>() },
            Any = new ComponentType[0],
            None = new ComponentType[0],
            Disabled = new ComponentType[0],
            Absent = new ComponentType[0],
            Options = EntityQueryOptions.IncludeSystems
        });
        __query_1235104412_1 = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[1] { ComponentType.ReadOnly<EducationParameterData>() },
            Any = new ComponentType[0],
            None = new ComponentType[0],
            Disabled = new ComponentType[0],
            Absent = new ComponentType[0],
            Options = EntityQueryOptions.IncludeSystems
        });
        __query_1235104412_2 = state.GetEntityQuery(new EntityQueryDesc
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
    public SchoolAISystem_RealPop()
    {
    }
}
