using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Agents;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;
using Game.UI;
using Game;

namespace RealPop.UI;

// This System is based on PopulationInfoviewUISystem by CO
[CompilerGenerated]
public class PopulationStructureUISystem : UISystemBase
{
    /// <summary>
    /// Holds info about population at Age
    /// </summary>
    private struct PopulationAtAgeInfo
    {
        public int Age;
        public int Total; // asserion: Total is a sum of the below parts
        public int School1; // elementary school
        public int School2; // high school
        public int School3; // college
        public int School4; // university
        public int Work; // working
        public int Other; // not working, not student
        public PopulationAtAgeInfo(int _age) { Age = _age; }
    }

    private static void WriteData(IJsonWriter writer, PopulationAtAgeInfo info)
    {
        writer.TypeBegin("populationAtAgeInfo");
        writer.PropertyName("age");
        writer.Write(info.Age);
        writer.PropertyName("total");
        writer.Write(info.Total);
        writer.PropertyName("school1");
        writer.Write(info.School1);
        writer.PropertyName("school2");
        writer.Write(info.School2);
        writer.PropertyName("school3");
        writer.Write(info.School3);
        writer.PropertyName("school4");
        writer.Write(info.School4);
        writer.PropertyName("work");
        writer.Write(info.Work);
        writer.PropertyName("other");
        writer.Write(info.Other);
        writer.TypeEnd();
    }

    [BurstCompile]
    private struct HouseholdJob : IJobChunk
    {
        [ReadOnly]
        public BufferTypeHandle<HouseholdCitizen> m_HouseholdCitizenHandle;

        [ReadOnly]
        public ComponentTypeHandle<Household> m_HouseholdType;

        [ReadOnly]
        public ComponentLookup<Worker> m_WorkerFromEntity;

        [ReadOnly]
        public ComponentLookup<HealthProblem> m_HealthProblems;

        [ReadOnly]
        public ComponentLookup<Citizen> m_Citizens;

        [ReadOnly]
        public ComponentLookup<Student> m_Students;

        public NativeArray<PopulationAtAgeInfo> m_Results;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            //Plugin.Log($"Execute: {chunk.Count} entities");
            BufferAccessor<HouseholdCitizen> bufferAccessor = chunk.GetBufferAccessor(ref m_HouseholdCitizenHandle);
            NativeArray<Household> nativeArray = chunk.GetNativeArray(ref m_HouseholdType);
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            int num5 = 0;
            int num6 = 0;
            for (int i = 0; i < bufferAccessor.Length; i++)
            {
                DynamicBuffer<HouseholdCitizen> dynamicBuffer = bufferAccessor[i];
                if ((nativeArray[i].m_Flags & HouseholdFlags.MovedIn) == 0)
                {
                    continue;
                }
                for (int j = 0; j < dynamicBuffer.Length; j++)
                {
                    Entity citizen = dynamicBuffer[j].m_Citizen;
                    Citizen citizen2 = m_Citizens[citizen];
                    if ((m_HealthProblems.HasComponent(citizen) && CitizenUtils.IsDead(m_HealthProblems[citizen])) || (citizen2.m_State & (CitizenFlags.Tourist | CitizenFlags.Commuter)) != 0)
                    {
                        continue;
                    }
                    switch (citizen2.GetAge())
                    {
                    case CitizenAge.Child:
                        num++;
                        break;
                    case CitizenAge.Teen:
                        num2++;
                        if (m_Students.HasComponent(citizen))
                        {
                            num6++;
                        }
                        break;
                    case CitizenAge.Adult:
                        num3++;
                        if (m_Students.HasComponent(citizen))
                        {
                            num6++;
                        }
                        break;
                    case CitizenAge.Elderly:
                        num4++;
                        break;
                    }
                    if (m_WorkerFromEntity.HasComponent(citizen))
                    {
                        num5++;
                    }
                }
            }
            // Work with a local variable to avoid CS0206 error
            PopulationAtAgeInfo elem0 = m_Results[0];
            elem0.School1 = num;
            elem0.School2 = num2;
            elem0.School3 = num3;
            elem0.School4 = num4;
            m_Results[0] = elem0;
            //m_Results[0].Total += num;
            //m_Results[1].Total += num2;
            //m_Results[2].Total += num3;
            //m_Results[3].Total += num4;
            //m_Results[4].Total += num5;
            //m_Results[6].Total += num6;
        }

        void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    [BurstCompile]
    private struct WorkProviderJob : IJobChunk
    {
        [ReadOnly]
        public ComponentTypeHandle<WorkProvider> m_WorkProviderHandle;

        public NativeArray<int> m_Results;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<WorkProvider> nativeArray = chunk.GetNativeArray(ref m_WorkProviderHandle);
            int num = 0;
            for (int i = 0; i < nativeArray.Length; i++)
            {
                num += nativeArray[i].m_MaxWorkers;
            }
            m_Results[5] += num;
        }

        void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    private struct TypeHandle
    {
        public BufferTypeHandle<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RW_BufferTypeHandle;

        public ComponentTypeHandle<Household> __Game_Citizens_Household_RW_ComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Student> __Game_Citizens_Student_RO_ComponentLookup;

        [ReadOnly]
        public ComponentTypeHandle<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentTypeHandle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Game_Citizens_HouseholdCitizen_RW_BufferTypeHandle = state.GetBufferTypeHandle<HouseholdCitizen>();
            __Game_Citizens_Household_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Household>();
            __Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
            __Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
            __Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
            __Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Student>(isReadOnly: true);
            __Game_Companies_WorkProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WorkProvider>(isReadOnly: true);
        }
    }

    private const string kGroup = "populationInfo";

    //private CityStatisticsSystem m_CityStatisticsSystem;

    private CitySystem m_CitySystem;

    //private ValueBinding<int> m_Population;

    //private ValueBinding<int> m_Employed;

    //private ValueBinding<int> m_Jobs;

    //private ValueBinding<float> m_Unemployment;

    //private ValueBinding<int> m_BirthRate;

    //private ValueBinding<int> m_DeathRate;

    //private ValueBinding<int> m_MovedIn;

    //private ValueBinding<int> m_MovedAway;

    private RawValueBinding m_AgeData;

    private EntityQuery m_HouseholdQuery;

    private EntityQuery m_WorkProviderQuery;

    private EntityQuery m_WorkProviderModifiedQuery;

    private NativeArray<PopulationAtAgeInfo> m_Results; // final results, will be filled via jobs and then written as output

    private TypeHandle __TypeHandle;

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 128; // approx. twice per minute on normal speed
    }

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        //m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
        m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
        m_HouseholdQuery = GetEntityQuery(ComponentType.ReadOnly<Household>(), ComponentType.ReadOnly<HouseholdCitizen>(), ComponentType.Exclude<PropertySeeker>(), ComponentType.Exclude<TouristHousehold>(), ComponentType.Exclude<CommuterHousehold>(), ComponentType.Exclude<MovingAway>());
        m_WorkProviderQuery = GetEntityQuery(ComponentType.ReadOnly<WorkProvider>(), ComponentType.Exclude<PropertySeeker>(), ComponentType.Exclude<Native>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
        m_WorkProviderModifiedQuery = GetEntityQuery(ComponentType.ReadOnly<WorkProvider>(), ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Updated>(), ComponentType.Exclude<Temp>());

        AddBinding(m_AgeData = new RawValueBinding(kGroup, "structure", delegate(IJsonWriter binder)
        {
            //binder.TypeBegin("populationStructure");
            //binder.PropertyName("values");
            binder.ArrayBegin(m_Results.Length);
            for (int i = 0; i < m_Results.Length; i++)
            {
                WriteData(binder, m_Results[i]);
            }
            binder.ArrayEnd();
            //binder.TypeEnd();
            //UpdateAgeDataBinding(binder, m_Results);
        }));
        m_Results = new NativeArray<PopulationAtAgeInfo>(20, Allocator.Persistent); // INFIXO: TODO
        Plugin.Log("ProductionStructureUISystem created.");
    }

    [Preserve]
    protected override void OnDestroy()
    {
        m_Results.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        ResetResults();
        // main job that processess the households, so we can skip cims that have not yet moved in
        __TypeHandle.__Game_Citizens_Student_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Household_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_HouseholdCitizen_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
        HouseholdJob jobData = default(HouseholdJob);
        jobData.m_HouseholdCitizenHandle = __TypeHandle.__Game_Citizens_HouseholdCitizen_RW_BufferTypeHandle;
        jobData.m_HouseholdType = __TypeHandle.__Game_Citizens_Household_RW_ComponentTypeHandle;
        jobData.m_WorkerFromEntity = __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup;
        jobData.m_Citizens = __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup;
        jobData.m_HealthProblems = __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup;
        jobData.m_Students = __TypeHandle.__Game_Citizens_Student_RO_ComponentLookup;
        jobData.m_Results = m_Results;
        JobChunkExtensions.Schedule(jobData, m_HouseholdQuery, base.Dependency).Complete();
        /* INFIXO
        __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        WorkProviderJob jobData2 = default(WorkProviderJob);
        jobData2.m_WorkProviderHandle = __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentTypeHandle;
        jobData2.m_Results = m_Results;
        JobChunkExtensions.Schedule(jobData2, m_WorkProviderQuery, base.Dependency).Complete();
        */
        //int num = m_Results[2];
        //int num2 = m_Results[1];
        //int num3 = m_Results[4];
        //int newValue = m_Results[5];
        //int num4 = num2 + num - m_Results[6];
        //float newValue2 = (((float)num4 > 0f) ? ((float)(num4 - num3) / (float)num4 * 100f) : 0f);
        //m_Jobs.Update(newValue);
        //m_Employed.Update(num3);
        //m_Unemployment.Update(newValue2);
        //Population componentData = base.EntityManager.GetComponentData<Population>(m_CitySystem.City);
        //m_Population.Update(componentData.m_Population);
        m_AgeData.Update();
        //UpdateStatistics();
        //Plugin.Log("jobs",true);
    }

    private void ResetResults()
    {
        for (int i = 0; i < m_Results.Length; i++)
        {
            m_Results[i] = new PopulationAtAgeInfo(i);
        }
        //Plugin.Log("reset",true);
    }

    /*
    private void UpdateAgeData(IJsonWriter binder)
    {
        int children = m_Results[0];
        int teens = m_Results[1];
        int adults = m_Results[2];
        int seniors = m_Results[3];
        UpdateAgeDataBinding(binder, children, teens, adults, seniors);
    }
    */
    /*
    private static void UpdateAgeDataBinding(IJsonWriter binder, NativeArray<PopulationAtAgeInfo> m_Results)
    {
        binder.TypeBegin("populationStructure");
        binder.PropertyName("values");
        binder.ArrayBegin(m_Results.Length);

        for (int i = 0; i < m_Results.Length; i++)
        {
            WriteData(binder, m_Results[i]);
        }

        binder.ArrayEnd();
        binder.TypeEnd();
    }
    */
    /* Infixo: not used
    private void UpdateStatistics()
    {
        m_BirthRate.Update(m_CityStatisticsSystem.GetStatisticValue(StatisticType.BirthRate));
        m_DeathRate.Update(m_CityStatisticsSystem.GetStatisticValue(StatisticType.DeathRate));
        m_MovedIn.Update(m_CityStatisticsSystem.GetStatisticValue(StatisticType.CitizensMovedIn));
        m_MovedAway.Update(m_CityStatisticsSystem.GetStatisticValue(StatisticType.CitizensMovedAway));
    }
    */

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
    public PopulationStructureUISystem()
    {
    }
}
