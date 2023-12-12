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

namespace Game.UI.InGame;

// This System is based on PopulationInfoviewUISystem by CO
[CompilerGenerated]
public class PopulationInfoviewUISystem : InfoviewUISystemBase
{
    private enum Result
    {
        Children,
        Teens,
        Adults,
        Seniors,
        Workers,
        Jobs,
        Students,
        ResultCount
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

        public NativeArray<int> m_Results;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
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
            m_Results[0] += num;
            m_Results[1] += num2;
            m_Results[2] += num3;
            m_Results[3] += num4;
            m_Results[4] += num5;
            m_Results[6] += num6;
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

    private CityStatisticsSystem m_CityStatisticsSystem;

    private CitySystem m_CitySystem;

    private ValueBinding<int> m_Population;

    private ValueBinding<int> m_Employed;

    private ValueBinding<int> m_Jobs;

    private ValueBinding<float> m_Unemployment;

    private ValueBinding<int> m_BirthRate;

    private ValueBinding<int> m_DeathRate;

    private ValueBinding<int> m_MovedIn;

    private ValueBinding<int> m_MovedAway;

    private RawValueBinding m_AgeData;

    private EntityQuery m_HouseholdQuery;

    private EntityQuery m_WorkProviderQuery;

    private EntityQuery m_WorkProviderModifiedQuery;

    private NativeArray<int> m_Results;

    private TypeHandle __TypeHandle;

    protected override bool Active
    {
        get
        {
            if (!base.Active && !m_Population.active && !m_Employed.active && !m_Jobs.active && !m_Unemployment.active && !m_BirthRate.active && !m_DeathRate.active && !m_MovedIn.active && !m_MovedAway.active)
            {
                return m_AgeData.active;
            }
            return true;
        }
    }

    protected override bool Modified => !m_WorkProviderModifiedQuery.IsEmptyIgnoreFilter;

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
        m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
        m_HouseholdQuery = GetEntityQuery(ComponentType.ReadOnly<Household>(), ComponentType.ReadOnly<HouseholdCitizen>(), ComponentType.Exclude<PropertySeeker>(), ComponentType.Exclude<TouristHousehold>(), ComponentType.Exclude<CommuterHousehold>(), ComponentType.Exclude<MovingAway>());
        m_WorkProviderQuery = GetEntityQuery(ComponentType.ReadOnly<WorkProvider>(), ComponentType.Exclude<PropertySeeker>(), ComponentType.Exclude<Native>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
        m_WorkProviderModifiedQuery = GetEntityQuery(ComponentType.ReadOnly<WorkProvider>(), ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Updated>(), ComponentType.Exclude<Temp>());
        AddBinding(m_Population = new ValueBinding<int>("populationInfo", "population", 0));
        AddBinding(m_Employed = new ValueBinding<int>("populationInfo", "employed", 0));
        AddBinding(m_Jobs = new ValueBinding<int>("populationInfo", "jobs", 0));
        AddBinding(m_Unemployment = new ValueBinding<float>("populationInfo", "unemployment", 0f));
        AddBinding(m_BirthRate = new ValueBinding<int>("populationInfo", "birthRate", 0));
        AddBinding(m_DeathRate = new ValueBinding<int>("populationInfo", "deathRate", 0));
        AddBinding(m_MovedIn = new ValueBinding<int>("populationInfo", "movedIn", 0));
        AddBinding(m_MovedAway = new ValueBinding<int>("populationInfo", "movedAway", 0));
        AddBinding(m_AgeData = new RawValueBinding("populationInfo", "ageData", delegate(IJsonWriter binder)
        {
            int children = m_Results[0];
            int teens = m_Results[1];
            int adults = m_Results[2];
            int seniors = m_Results[3];
            UpdateAgeDataBinding(binder, children, teens, adults, seniors);
        }));
        m_Results = new NativeArray<int>(7, Allocator.Persistent);
    }

    [Preserve]
    protected override void OnDestroy()
    {
        m_Results.Dispose();
        base.OnDestroy();
    }

    protected override void PerformUpdate()
    {
        UpdateBindings();
    }

    private void UpdateBindings()
    {
        ResetResults();
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
        __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        WorkProviderJob jobData2 = default(WorkProviderJob);
        jobData2.m_WorkProviderHandle = __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentTypeHandle;
        jobData2.m_Results = m_Results;
        JobChunkExtensions.Schedule(jobData2, m_WorkProviderQuery, base.Dependency).Complete();
        int num = m_Results[2];
        int num2 = m_Results[1];
        int num3 = m_Results[4];
        int newValue = m_Results[5];
        int num4 = num2 + num - m_Results[6];
        float newValue2 = (((float)num4 > 0f) ? ((float)(num4 - num3) / (float)num4 * 100f) : 0f);
        m_Jobs.Update(newValue);
        m_Employed.Update(num3);
        m_Unemployment.Update(newValue2);
        Population componentData = base.EntityManager.GetComponentData<Population>(m_CitySystem.City);
        m_Population.Update(componentData.m_Population);
        m_AgeData.Update();
        UpdateStatistics();
    }

    private void ResetResults()
    {
        for (int i = 0; i < m_Results.Length; i++)
        {
            m_Results[i] = 0;
        }
    }

    private void UpdateAgeData(IJsonWriter binder)
    {
        int children = m_Results[0];
        int teens = m_Results[1];
        int adults = m_Results[2];
        int seniors = m_Results[3];
        UpdateAgeDataBinding(binder, children, teens, adults, seniors);
    }

    private static void UpdateAgeDataBinding(IJsonWriter binder, int children, int teens, int adults, int seniors)
    {
        binder.TypeBegin("infoviews.ChartData");
        binder.PropertyName("values");
        binder.ArrayBegin(4u);
        binder.Write(children);
        binder.Write(teens);
        binder.Write(adults);
        binder.Write(seniors);
        binder.ArrayEnd();
        binder.PropertyName("total");
        binder.Write(children + teens + adults + seniors);
        binder.TypeEnd();
    }

    private void UpdateStatistics()
    {
        m_BirthRate.Update(m_CityStatisticsSystem.GetStatisticValue(StatisticType.BirthRate));
        m_DeathRate.Update(m_CityStatisticsSystem.GetStatisticValue(StatisticType.DeathRate));
        m_MovedIn.Update(m_CityStatisticsSystem.GetStatisticValue(StatisticType.CitizensMovedIn));
        m_MovedAway.Update(m_CityStatisticsSystem.GetStatisticValue(StatisticType.CitizensMovedAway));
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
    public PopulationInfoviewUISystem()
    {
    }
}
