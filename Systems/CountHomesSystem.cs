using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Debug;
using Game.Prefabs;
using Game.Reflection;
using Game.Tools;
using Game.Triggers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace RealPop.Systems;

[CompilerGenerated]
public class CountHomesSystem : GameSystemBase
{
    [BurstCompile]
    private struct CountResidentialPropertiesJob : IJob
    {
        [ReadOnly]
        public NativeList<ArchetypeChunk> m_ResidentialChunks; // OK

        [ReadOnly]
        public NativeList<ArchetypeChunk> m_HouseholdChunks; // OK

        //[DeallocateOnJobCompletion]
        //[ReadOnly]
        //public NativeArray<ZonePropertiesData> m_UnlockedZones;

        [ReadOnly]
        public BufferTypeHandle<Renter> m_RenterType; // OK

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> m_PrefabType; // OK

        [ReadOnly]
        public ComponentTypeHandle<PropertyRenter> m_PropertyRenterType; // OK

        [ReadOnly]
        public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas; // OK

        [ReadOnly]
        public ComponentLookup<Household> m_Households; // OK

        //[ReadOnly]
        //public ComponentLookup<Population> m_Populations;

        [ReadOnly]
        public ComponentLookup<SpawnableBuildingData> m_SpawnableDatas; // OK

        [ReadOnly]
        public ComponentLookup<ZonePropertiesData> m_ZonePropertyDatas; // OK

        //[ReadOnly]
        //public NativeList<DemandParameterData> m_DemandParameters;

        //[ReadOnly]
        //public NativeValue<int> m_UnemploymentRate;

        //[ReadOnly]
        //public NativeValue<int> m_StudyPositions;

        //[ReadOnly]
        //public NativeArray<int> m_TaxRates;

        //public Entity m_City;

        //public NativeValue<int> m_HouseholdDemand;

        //public NativeValue<int3> m_BuildingDemand;

        //public NativeArray<int> m_LowDemandFactors;

        //public NativeArray<int> m_MediumDemandFactors;

        //public NativeArray<int> m_HighDemandFactors;

        //public NativeQueue<TriggerAction> m_TriggerQueue;

        public NativeValue<int> m_TotalProperties;

        public NativeValue<int> m_FreeProperties;

        public void Execute()
        {
            // COUNT HOMELESS HOUSEHOLDS
            int numHomeless = 0, numHouseholds = 0;
            for (int j = 0; j < m_HouseholdChunks.Length; j++)
            {
                ArchetypeChunk archetypeChunk = m_HouseholdChunks[j];
                if (!archetypeChunk.Has(ref m_PropertyRenterType))
                {
                    numHomeless += archetypeChunk.Count;
                }
                numHouseholds += archetypeChunk.Count;
            }
            // COUNT RESIDENTIAL PROPERTIES
            int numFree = 0, numProperties = 0;
            for (int l = 0; l < m_ResidentialChunks.Length; l++)
            {
                ArchetypeChunk archetypeChunk2 = m_ResidentialChunks[l];
                NativeArray<PrefabRef> nativeArray = archetypeChunk2.GetNativeArray(ref m_PrefabType);
                BufferAccessor<Renter> bufferAccessor = archetypeChunk2.GetBufferAccessor(ref m_RenterType);
                for (int m = 0; m < nativeArray.Length; m++)
                {
                    Entity prefab = nativeArray[m].m_Prefab;
                    SpawnableBuildingData spawnableBuildingData = m_SpawnableDatas[prefab];
                    ZonePropertiesData zonePropertiesData = m_ZonePropertyDatas[spawnableBuildingData.m_ZonePrefab];
                    if (!m_BuildingPropertyDatas.HasComponent(prefab))
                    {
                        continue;
                    }
                    BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab];
                    DynamicBuffer<Renter> renterBuffer = bufferAccessor[m];
                    int numRenters = 0;
                    for (int n = 0; n < renterBuffer.Length; n++)
                    {
                        if (m_Households.HasComponent(renterBuffer[n].m_Renter))
                        {
                            numRenters++;
                        }
                    }
                    if (!zonePropertiesData.m_ScaleResidentials) // low density
                    {
                        numProperties++;
                        numFree += 1 - numRenters;
                        //Plugin.Log($"LowRes: {numRenters} / 1");
                    }
                    else // medium & high density
                    {
                        numProperties += buildingPropertyData.m_ResidentialProperties;
                        numFree += buildingPropertyData.m_ResidentialProperties - numRenters;
                        //Plugin.Log($"Building: {numRenters} / {buildingPropertyData.m_ResidentialProperties}");
                    }
                }
            }
            // END
            m_TotalProperties.value = numProperties;
            m_FreeProperties.value = math.max(0, numFree - numHomeless);
            //Plugin.Log($"TOTAL {m_FreeProperties.value}: homes {numProperties} free {numFree} households {numHouseholds} homeless {numHomeless}");
        }
    }

    private struct TypeHandle
    {
        [ReadOnly]
        public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ZonePropertiesData> __Game_Prefabs_ZonePropertiesData_RO_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
            __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
            __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
            __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
            __Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
            __Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
            __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
            __Game_Prefabs_ZonePropertiesData_RO_ComponentLookup = state.GetComponentLookup<ZonePropertiesData>(isReadOnly: true);
        }
    }

    //private TaxSystem m_TaxSystem;

    //private CountEmploymentSystem m_CountEmploymentSystem;

    //private CountStudyPositionsSystem m_CountStudyPositionsSystem;

    //private CitySystem m_CitySystem;

    //private TriggerSystem m_TriggerSystem;

    //private EntityQuery m_DemandParameterGroup;

    private EntityQuery m_AllHouseholdGroup;

    private EntityQuery m_AllResidentialGroup;

    //private EntityQuery m_UnlockedZoneQuery;

    //private NativeValue<int> m_HouseholdDemand;

    //private NativeValue<int3> m_BuildingDemand;

    //[EnumArray(typeof(DemandFactor))]
    //[DebugWatchValue]
    //private NativeArray<int> m_LowDemandFactors;

    //[EnumArray(typeof(DemandFactor))]
    //[DebugWatchValue]
    //private NativeArray<int> m_MediumDemandFactors;

    //[EnumArray(typeof(DemandFactor))]
    //[DebugWatchValue]
    //private NativeArray<int> m_HighDemandFactors;

    private NativeValue<int> m_TotalProperties;

    private NativeValue<int> m_FreeProperties;

    //[DebugWatchDeps]
    //private JobHandle m_WriteDependencies;

    private JobHandle m_ReadDependencies;

    //private int m_LastHouseholdDemand;

    //private int3 m_LastBuildingDemand;

    private TypeHandle __TypeHandle;

    [DebugWatchValue(color = "#27ae60")]
    public int totalProperties => m_TotalProperties.value;

    [DebugWatchValue(color = "#117a65")]
    public int freeProperties => m_FreeProperties.value;

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 64;
    }

    public override int GetUpdateOffset(SystemUpdatePhase phase)
    {
        return 61;
    }

    public void AddReader(JobHandle reader)
    {
        m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, reader);
    }

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        //m_DemandParameterGroup = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
        m_AllHouseholdGroup = GetEntityQuery(ComponentType.ReadOnly<Household>(), ComponentType.Exclude<TouristHousehold>(), ComponentType.Exclude<CommuterHousehold>(), ComponentType.Exclude<MovingAway>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
        m_AllResidentialGroup = GetEntityQuery(ComponentType.ReadOnly<ResidentialProperty>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Condemned>(), ComponentType.Exclude<Abandoned>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
        //m_UnlockedZoneQuery = GetEntityQuery(ComponentType.ReadOnly<ZoneData>(), ComponentType.ReadOnly<ZonePropertiesData>(), ComponentType.Exclude<Locked>());
        //m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
        //m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
        //m_CountEmploymentSystem = base.World.GetOrCreateSystemManaged<CountEmploymentSystem>();
        //m_CountStudyPositionsSystem = base.World.GetOrCreateSystemManaged<CountStudyPositionsSystem>();
        //m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
        //m_HouseholdDemand = new NativeValue<int>(Allocator.Persistent);
        //m_BuildingDemand = new NativeValue<int3>(Allocator.Persistent);
        //m_LowDemandFactors = new NativeArray<int>(18, Allocator.Persistent);
        //m_MediumDemandFactors = new NativeArray<int>(18, Allocator.Persistent);
        //m_HighDemandFactors = new NativeArray<int>(18, Allocator.Persistent);
        m_TotalProperties = new NativeValue<int>(Allocator.Persistent);
        m_FreeProperties = new NativeValue<int>(Allocator.Persistent);
        m_TotalProperties.value = -1;
        m_FreeProperties.value = -1;
        Plugin.Log("CountHomesSystem created.");
    }

    [Preserve]
    protected override void OnDestroy()
    {
        //m_HouseholdDemand.Dispose();
        //m_BuildingDemand.Dispose();
        //m_LowDemandFactors.Dispose();
        //m_MediumDemandFactors.Dispose();
        //m_HighDemandFactors.Dispose();
        m_TotalProperties.Dispose();
        m_FreeProperties.Dispose();
        base.OnDestroy();
    }

    [Preserve]
    protected override void OnUpdate()
    {
        //if (!m_DemandParameterGroup.IsEmptyIgnoreFilter)
        //{
        //m_LastHouseholdDemand = m_HouseholdDemand.value;
        //m_LastBuildingDemand = m_BuildingDemand.value;
        __TypeHandle.__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_City_Population_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
        __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
        CountResidentialPropertiesJob countJob = default(CountResidentialPropertiesJob);
        countJob.m_ResidentialChunks = m_AllResidentialGroup.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle);
        countJob.m_HouseholdChunks = m_AllHouseholdGroup.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle2);
        //countJob.m_UnlockedZones = m_UnlockedZoneQuery.ToComponentDataArray<ZonePropertiesData>(Allocator.TempJob);
        countJob.m_RenterType = __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle;
        countJob.m_PrefabType = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
        countJob.m_PropertyRenterType = __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;
        countJob.m_BuildingPropertyDatas = __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
        countJob.m_Households = __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup;
        //countJob.m_Populations = __TypeHandle.__Game_City_Population_RO_ComponentLookup;
        countJob.m_SpawnableDatas = __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
        countJob.m_ZonePropertyDatas = __TypeHandle.__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup;
        //countJob.m_DemandParameters = m_DemandParameterGroup.ToComponentDataListAsync<DemandParameterData>(base.World.UpdateAllocator.ToAllocator, out var outJobHandle3);
        //countJob.m_UnemploymentRate = m_CountEmploymentSystem.GetUnemployment(out var deps);
        //countJob.m_StudyPositions = m_CountStudyPositionsSystem.GetStudyPositions(out var deps2);
        //countJob.m_TaxRates = m_TaxSystem.GetTaxRates();
        //countJob.m_City = m_CitySystem.City;
        //countJob.m_HouseholdDemand = m_HouseholdDemand;
        //countJob.m_BuildingDemand = m_BuildingDemand;
        //countJob.m_LowDemandFactors = m_LowDemandFactors;
        //countJob.m_MediumDemandFactors = m_MediumDemandFactors;
        //countJob.m_HighDemandFactors = m_HighDemandFactors;
        //countJob.m_TriggerQueue = m_TriggerSystem.CreateActionBuffer();
        countJob.m_TotalProperties = m_TotalProperties;
        countJob.m_FreeProperties = m_FreeProperties;
        CountResidentialPropertiesJob jobData = countJob;
        base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, m_ReadDependencies, outJobHandle, outJobHandle2));
        //m_WriteDependencies = base.Dependency;
        //m_CountEmploymentSystem.AddReader(base.Dependency);
        //m_CountStudyPositionsSystem.AddReader(base.Dependency);
        //m_TaxSystem.AddReader(base.Dependency);
        //m_TriggerSystem.AddActionBufferWriter(base.Dependency);
        //}
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
    public CountHomesSystem()
    {
    }
}
