using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Triggers;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using BepInEx.Configuration;

namespace RealPop.Systems;

[CompilerGenerated]
public class GraduationSystem_RealPop : GameSystemBase
{
    [BurstCompile]
    public struct GraduationJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

        [ReadOnly]
        public ComponentTypeHandle<Game.Citizens.Student> m_StudentType;

        public ComponentTypeHandle<Citizen> m_CitizenType;

        [ReadOnly]
        public ComponentLookup<PrefabRef> m_Prefabs;

        [ReadOnly]
        public ComponentLookup<SchoolData> m_SchoolDatas;

        [ReadOnly]
        public ComponentLookup<TravelPurpose> m_Purposes;

        [ReadOnly]
        public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

        [ReadOnly]
        public BufferLookup<CityModifier> m_CityModifiers;

        [ReadOnly]
        public BufferLookup<Efficiency> m_BuildingEfficiencies;

        [ReadOnly]
        public BufferLookup<ServiceFee> m_Fees;

        public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;

        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

        public EconomyParameterData m_EconomyParameters;

        public TimeData m_TimeData;

        public RandomSeed m_RandomSeed;

        public uint m_SimulationFrame;

        public Entity m_City;

        public uint m_UpdateFrameIndex;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
            {
                return;
            }
            //Plugin.Logger.LogInfo($"GraduationJob.Execute: frame {m_UpdateFrameIndex}");
            NativeArray<Game.Citizens.Student> nativeArray = chunk.GetNativeArray(ref m_StudentType);
            NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray(ref m_CitizenType);
            NativeArray<Entity> nativeArray3 = chunk.GetNativeArray(m_EntityType);
            DynamicBuffer<CityModifier> modifiers = m_CityModifiers[m_City];
            Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
            //RealPop.Debug.Log($"graduation loop for {chunk.Count} people");
            int day = TimeSystem.GetDay(m_SimulationFrame, m_TimeData);
            //float ageInDays = citizen.GetAgeInDays(simulationFrame, timeData);
            for (int i = 0; i < chunk.Count; i++)
            {
                // Infixo: pseudo-random approach to graduate every other day :(
                //if (random.NextInt(2) != 0)
                //{
                    //continue;
                //}
                Entity entity = nativeArray3[i];
                Game.Citizens.Student student = nativeArray[i];
                ref Citizen reference = ref nativeArray2.ElementAt(i);
                Entity school = student.m_School;
                if (!m_Prefabs.HasComponent(school))
                {
                    continue;
                }
                Entity prefab = m_Prefabs[school].m_Prefab;
                if (!m_SchoolDatas.HasComponent(prefab))
                {
                    continue;
                }
                int num = student.m_Level;
                if (num == 255)
                {
                    num = m_SchoolDatas[prefab].m_EducationLevel;
                }
                SchoolData result = m_SchoolDatas[prefab];
                if (m_InstalledUpgrades.HasBuffer(school))
                {
                    UpgradeUtils.CombineStats(ref result, m_InstalledUpgrades[school], ref m_Prefabs, ref m_SchoolDatas);
                }
                int wellBeing = reference.m_WellBeing;
                float studyWillingness = reference.GetPseudoRandom(CitizenPseudoRandom.StudyWillingness).NextFloat();
                float efficiency = BuildingUtils.GetEfficiency(school, ref m_BuildingEfficiencies);
                float graduationProbability = GetGraduationProbability(num, wellBeing, result, modifiers, studyWillingness, efficiency);
                int ageInDays = day - reference.m_BirthDay;
                //string logmsg = $"{reference.GetAge()} {entity.Index} {ageInDays} school {num} failed {reference.GetFailedEducationCount()} grad_prob {graduationProbability}";
                //RealPop.Debug.Log($"grad_prob {graduationProbability} lev {num} well {wellBeing} mod {result.m_GraduationModifier} will {studyWillingness} eff {efficiency}");
                //Plugin.Logger.LogInfo($"{reference.GetAge()} {entity.Index} age {ageInDays} school {num} failed {reference.GetFailedEducationCount()} prob {graduationProbability}");
                // Infixo: first check if graduation is possible
                bool gradPossible = false;
                switch (num)
                {
                    case 1: // elementary school - when approaching Teen age
                        gradPossible = (RealPop.Systems.AgingSystem_RealPop.GetTeenAgeLimitInDays() - ageInDays) <= 1;
                        break;
                    case 2: // high school - after min years in school
                        gradPossible = (ageInDays - RealPop.Systems.AgingSystem_RealPop.GetTeenAgeLimitInDays()) >= s_Education2InDays;
                        break;
                    case 3: // college - depends if Teen or Adult
                        if (reference.GetAge() == CitizenAge.Teen)
                        {
                            gradPossible = (ageInDays - RealPop.Systems.AgingSystem_RealPop.GetTeenAgeLimitInDays()) >= s_Education2InDays + s_Education3InDays;
                        }
                        else // Adult
                        {
                            gradPossible = (ageInDays - RealPop.Systems.AgingSystem_RealPop.GetAdultAgeLimitInDays()) >= s_Education3InDays;
                        }
                        break;
                    case 4: // university - after min years in school
                        gradPossible = (ageInDays - RealPop.Systems.AgingSystem_RealPop.GetAdultAgeLimitInDays()) >= s_Education4InDays;
                        break;
                    default:
                        break;
                }
                if (!gradPossible)
                {
                    // too early to graduate
                    //Plugin.Logger.LogInfo(logmsg + $" TOOEARLY");
                    continue;
                }
                if (random.NextFloat() < graduationProbability)
                {
                    //Plugin.Logger.LogInfo(logmsg + $" GRADUATED");
                    reference.SetEducationLevel(Mathf.Max(reference.GetEducationLevel(), num));
                    if (num > 1)
                    {
                        LeaveSchool(unfilteredChunkIndex, entity, school);
                        m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenGraduated, Entity.Null, entity, school));
                    }
                    continue;
                }
                if (num > 1)
                {
                    //Plugin.Logger.LogInfo(logmsg + $" FAILED");
                    int failedEducationCount = reference.GetFailedEducationCount();
                    if (failedEducationCount >= 3)
                    {
                        //Plugin.Logger.LogInfo(logmsg + $" REMOVED");
                        LeaveSchool(unfilteredChunkIndex, entity, school);
                        m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenFailedSchool, Entity.Null, entity, school));
                        continue;
                    }
                    reference.SetFailedEducationCount(failedEducationCount + 1);
                }
                float fee = ServiceFeeSystem.GetFee(ServiceFeeSystem.GetEducationResource(student.m_Level), m_Fees[m_City]);
                float dropoutProbability = GetDropoutProbability(reference, student.m_Level, student.m_LastCommuteTime, fee, 0, m_SimulationFrame, ref m_EconomyParameters, result, modifiers, efficiency, m_TimeData);
                dropoutProbability = 1f - math.pow(math.saturate(1f - dropoutProbability), 32f);
                if (random.NextFloat() < dropoutProbability)
                {
                    //Plugin.Logger.LogInfo(logmsg + $" DROPOUT");
                    LeaveSchool(unfilteredChunkIndex, entity, school);
                    m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenDroppedOutSchool, Entity.Null, entity, school));
                }
            }
        }

        private void LeaveSchool(int chunkIndex, Entity entity, Entity school)
        {
            m_CommandBuffer.AddComponent<StudentsRemoved>(chunkIndex, school);
            m_CommandBuffer.RemoveComponent<Game.Citizens.Student>(chunkIndex, entity);
            if (m_Purposes.TryGetComponent(entity, out var componentData))
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
        public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

        public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

        public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RW_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<SchoolData> __Game_Prefabs_SchoolData_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

        [ReadOnly]
        public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<ServiceFee> __Game_City_ServiceFee_RO_BufferLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
            __Game_Citizens_Citizen_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>();
            __Game_Citizens_Student_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Citizens.Student>(isReadOnly: true);
            __Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
            __Game_Prefabs_SchoolData_RO_ComponentLookup = state.GetComponentLookup<SchoolData>(isReadOnly: true);
            __Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
            __Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
            __Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(isReadOnly: true);
            __Game_Buildings_Efficiency_RO_BufferLookup = state.GetBufferLookup<Efficiency>(isReadOnly: true);
            __Game_City_ServiceFee_RO_BufferLookup = state.GetBufferLookup<ServiceFee>(isReadOnly: true);
        }
    }

    public const int kUpdatesPerDay = 1;

    public const int kCheckSlowdown = 2;

    private SimulationSystem m_SimulationSystem;

    private EndFrameBarrier m_EndFrameBarrier;

    private CitySystem m_CitySystem;

    private TriggerSystem m_TriggerSystem;

    private EntityQuery m_StudentQuery;

    private TypeHandle __TypeHandle;

    private EntityQuery __query_1855827631_0;

    private EntityQuery __query_1855827631_1;

    private static float s_Education2InDays; // high school
    private static float s_Education3InDays; // college
    private static float s_Education4InDays; // university

    private static float s_GraduationLevel1; // elementary school
    private static float s_GraduationLevel2; // high school
    private static float s_GraduationLevel3; // college
    private static float s_GraduationLevel4; // university

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 16384;
    }

    public static float GetDropoutProbability(Citizen citizen, int level, float commute, float fee, int wealth, uint simulationFrame, ref EconomyParameterData economyParameters, SchoolData schoolData, DynamicBuffer<CityModifier> modifiers, float efficiency, TimeData timeData)
    {
        float ageInDays = citizen.GetAgeInDays(simulationFrame, timeData);
        float studyWillingness = citizen.GetPseudoRandom(CitizenPseudoRandom.StudyWillingness).NextFloat();
        int failedEducationCount = citizen.GetFailedEducationCount();
        float graduationProbability = GetGraduationProbability(level, citizen.m_WellBeing, schoolData, modifiers, studyWillingness, efficiency);
        return GetDropoutProbability(level, commute, fee, wealth, ageInDays, studyWillingness, failedEducationCount, graduationProbability, ref economyParameters);
    }

    public static float GetDropoutProbability(int level, float commute, float fee, int wealth, float age, float studyWillingness, int failedEducationCount, float graduationProbability, ref EconomyParameterData economyParameters)
    {
        int num = 4 - failedEducationCount;
        float s = math.pow(1f - graduationProbability, num);
        float num2 = 1f / (graduationProbability * 2f * 1f);
        float num3 = num2 * fee;
        if (level > 2)
        {
            num3 -= num2 * (float)economyParameters.m_UnemploymentBenefit;
        }
        float num4 = math.max(0f, (float)AgingSystem_RealPop.GetElderAgeLimitInDays() - age);
        float num5 = (float)economyParameters.GetWage(math.min(2, level - 1)) * num4;
        float num6 = math.lerp(economyParameters.GetWage(level), economyParameters.GetWage(level - 1), s) * (num4 - num2) - num3 + (0.5f + studyWillingness) * (float)economyParameters.m_UnemploymentBenefit * num2;
        if (num5 < num6)
        {
            float num7 = (num6 - num5) / num5;
            return math.saturate(-0.1f + (float)level / 4f - 10f * num7 - (float)wealth / (num6 - num5) + commute / 5000f);
        }
        return 1f;
    }

    public static float GetGraduationProbability(int level, int wellbeing, SchoolData schoolData, DynamicBuffer<CityModifier> modifiers, float studyWillingness, float efficiency)
    {
        float2 modifier = CityUtils.GetModifier(modifiers, CityModifierType.CollegeGraduation);
        float2 modifier2 = CityUtils.GetModifier(modifiers, CityModifierType.UniversityGraduation);
        return GetGraduationProbability(level, wellbeing, schoolData.m_GraduationModifier, modifier, modifier2, studyWillingness, efficiency);
    }

    public static float GetGraduationProbability(int level, int wellbeing, float graduationModifier, float2 collegeModifier, float2 uniModifier, float studyWillingness, float efficiency)
    {
        if (efficiency <= 0.001f)
        {
            return 0f;
        }
        float num = math.saturate((0.5f + studyWillingness) * (float)wellbeing / 75f);
        float num2 = 0f;
        switch (level)
        {
        case 1:
            num2 = math.smoothstep(0f, 1f, 0.6f * num + 0.41f);
            num2 *= s_GraduationLevel1; // default 100f
            num2 /= 100f;
            break;
        case 2:
            num2 = s_GraduationLevel2 * math.log(2.6f * num + 1.1f); // default 60f
            num2 /= 100f;
            break;
        case 3:
            num2 = s_GraduationLevel3 * math.log(1.6f * num + 1f); // default 90f
            num2 += collegeModifier.x;
            num2 += num2 * collegeModifier.y;
            num2 /= 100f;
            break;
        case 4:
            num2 = s_GraduationLevel4 * num; // default 70f
            num2 += uniModifier.x;
            num2 += num2 * uniModifier.y;
            num2 /= 100f;
            break;
        default:
            num2 = 0f;
            break;
        }
        num2 = 1f - (1f - num2) / efficiency;
        return num2 + graduationModifier;
    }

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
        m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
        m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
        m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
        m_StudentQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Citizens.Student>(), ComponentType.ReadWrite<Citizen>(), ComponentType.ReadOnly<UpdateFrame>());
        RequireForUpdate(m_StudentQuery);
        RequireForUpdate<EconomyParameterData>();
        RequireForUpdate<TimeData>();
        // RealPop
        s_Education2InDays = Plugin.Education2InDays.Value;
        s_Education3InDays = Plugin.Education3InDays.Value;
        s_Education4InDays = Plugin.Education4InDays.Value;
        s_GraduationLevel1 = Plugin.GraduationLevel1.Value;
        s_GraduationLevel2 = Plugin.GraduationLevel2.Value;
        s_GraduationLevel3 = Plugin.GraduationLevel3.Value;
        s_GraduationLevel4 = Plugin.GraduationLevel4.Value;
        Plugin.Log($"Modded GraduationSystem created. School days: {s_Education2InDays}, {s_Education3InDays}, {s_Education4InDays}. Graduation params: {s_GraduationLevel1}, {s_GraduationLevel2}, {s_GraduationLevel3}, {s_GraduationLevel4}");
    }

    [Preserve]
    protected override void OnUpdate()
    {
        uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, 1, 16);
        //RealPop.Debug.Log($"frame {m_SimulationSystem.frameIndex} update {updateFrame}");
        __TypeHandle.__Game_City_ServiceFee_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_City_CityModifier_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_SchoolData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Student_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
        GraduationJob graduationJob = default(GraduationJob);
        graduationJob.m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
        graduationJob.m_UpdateFrameType = __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
        graduationJob.m_CitizenType = __TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle;
        graduationJob.m_StudentType = __TypeHandle.__Game_Citizens_Student_RO_ComponentTypeHandle;
        graduationJob.m_Prefabs = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
        graduationJob.m_SchoolDatas = __TypeHandle.__Game_Prefabs_SchoolData_RO_ComponentLookup;
        graduationJob.m_InstalledUpgrades = __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup;
        graduationJob.m_CityModifiers = __TypeHandle.__Game_City_CityModifier_RO_BufferLookup;
        graduationJob.m_Purposes = __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup;
        graduationJob.m_BuildingEfficiencies = __TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup;
        graduationJob.m_Fees = __TypeHandle.__Game_City_ServiceFee_RO_BufferLookup;
        graduationJob.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();
        graduationJob.m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer().AsParallelWriter();
        graduationJob.m_EconomyParameters = __query_1855827631_0.GetSingleton<EconomyParameterData>();
        graduationJob.m_TimeData = __query_1855827631_1.GetSingleton<TimeData>();
        graduationJob.m_RandomSeed = RandomSeed.Next();
        graduationJob.m_City = m_CitySystem.City;
        graduationJob.m_SimulationFrame = m_SimulationSystem.frameIndex;
        graduationJob.m_UpdateFrameIndex = updateFrame;
        GraduationJob jobData = graduationJob;
        base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_StudentQuery, base.Dependency);
        m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
        m_TriggerSystem.AddActionBufferWriter(base.Dependency);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void __AssignQueries(ref SystemState state)
    {
        __query_1855827631_0 = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[1] { ComponentType.ReadOnly<EconomyParameterData>() },
            Any = new ComponentType[0],
            None = new ComponentType[0],
            Disabled = new ComponentType[0],
            Absent = new ComponentType[0],
            Options = EntityQueryOptions.IncludeSystems
        });
        __query_1855827631_1 = state.GetEntityQuery(new EntityQueryDesc
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
    public GraduationSystem_RealPop()
    {
    }
}
