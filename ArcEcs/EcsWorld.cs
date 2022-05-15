using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Poly.ArcEcs
{
    #region EcsWorld
    public class EcsWorld
    {
        //World
        private readonly string id;
        private readonly object shared;
        private bool isInited;
        private bool isDestroyed;
        //Entity
        internal EcsEntity[] entities;
        internal int entityCount;
        private int[] recycledEntities;
        private int recycledEntityCount;
        //ArcheType
        private byte[] tempCompIds;
        internal EcsArchetype[] archetypes;
        internal int archetypeCount;
        //Component
        internal EcsComponentType[] compTypes;
        internal byte compTypeCount;
        private Dictionary<Type, EcsComponentType> compTypeDict;
        //Query
        internal EcsQuery[] queries;
        internal int queryCount;
        internal EcsQueryDesc[] queryDescs;
        private Dictionary<int, int> queryIdDict;
        private EcsQueryDesc[] recycledQueryDescs;
        private int recycledQueryDescCount;
        //System
        private Dictionary<Type, IEcsSystem> systemDict;
        private List<IEcsSystem> systemList;
        //private IEcsRunSystem[] runSystems;
        //private int runSystemCount;

        public int EntityCount => entityCount - recycledEntityCount;
        public int ArchetypeCount => archetypeCount;
        public int QueryCount => queryCount;
        public IList<IEcsSystem> SystemList => systemList;

        //public event Action<int> WorldResizedEvent;
        public event Action<int> EntityCreatedEvent;
        public event Action<int> EntityDestroyedEvent;
        public event Action<int, int> EntityComponentAddedEvent;
        public event Action<int, int> EntityComponentRemovedEvent;
        public event Action<int> QueryCreatedEvent;

        public EcsWorld(string id = "Default", object shared = null, in Config cfg = default)
        {
            //World
            this.id = id;
            this.shared = shared;
            //Entity
            var capacity = cfg.EntityCapacity > 0 ? cfg.EntityCapacity : Config.EntityCapacityDefault;
            entities = new EcsEntity[capacity];
            entityCount = 0;
            capacity = cfg.RecycledEntityCapacity > 0 ? cfg.RecycledEntityCapacity : Config.RecycledEntityCapacityDefault;
            recycledEntities = new int[capacity];
            recycledEntityCount = 0;
            //ArcheType
            capacity = cfg.ArchetypeCapacity > 0 ? cfg.ArchetypeCapacity : Config.ArchetypeCapacityDefault;
            var emptyArchetypeData = new EcsArchetype(this, 0, null, 0);
            archetypes = new EcsArchetype[capacity];
            archetypeCount = 0;
            archetypes[archetypeCount++] = emptyArchetypeData;

            //Component
            //compTypeDatas = new FastArray<EcsComponentTypeData>(32);
            tempCompIds = new byte[256];
            compTypes = new EcsComponentType[256];
            compTypeCount = 0;
            compTypeDict = new Dictionary<Type, EcsComponentType>();
            //Query
            capacity = cfg.QueryCapacity > 0 ? cfg.QueryCapacity : Config.QueryCapacityDefault;
            queries = new EcsQuery[capacity];
            queryCount = 0;
            queryDescs = new EcsQueryDesc[capacity];
            queryIdDict = new Dictionary<int, int>();
            recycledQueryDescs = new EcsQueryDesc[32];
            recycledQueryDescCount = 0;
            //System
            //capacity = cfg.SystemCapacity > 0 ? cfg.SystemCapacity : Config.SystemCapacityDefault;
            systemDict = new Dictionary<Type, IEcsSystem>();
            systemList = new List<IEcsSystem>();
            //runSystems = new IEcsRunSystem[capacity];
            //runSystemCount = 0;
        }
        public void Destroy()
        {
            //Archetype
            for (int i = 0; i < archetypeCount; i++)
            {
                ref var archetype = ref archetypes[i];
                archetype.Dispose();
            }
            archetypes = null;
            archetypeCount = 0;
            //Entity
            entities = null;
            entityCount = 0;
            recycledEntities = null;
            recycledEntityCount = 0;
            //Component
            tempCompIds = null;
            compTypes = null;
            compTypeCount = 0;
            compTypeDict.Clear();
            compTypeDict = null;
            //Query
            queries = null;
            queryCount = 0;
            queryDescs = null;
            queryIdDict.Clear();
            queryIdDict = null;
            recycledQueryDescs = null;
            recycledQueryDescCount = 0;
            //System
            systemDict.Clear();
            systemDict = null;
            systemList.Clear();
            systemList = null;
        }
        public void Init()
        {
            foreach (var system in systemList)
                system.Init(this);
            isInited = true;
        }
        public void Update()
        {
            var count = systemList.Count;
            for (int i = 0; i < count; i++)
                systemList[i].Update();
        }
        public T GetShared<T>()
        {
            return (T)shared;
        }

        #region Entity
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly EcsEntity GetEntity(int entityId) => ref entities[entityId];
        private ref EcsEntity CreateEntityStruct()
        {
            int entityId;
            if (recycledEntityCount > 0)
            {
                entityId = recycledEntities[--recycledEntityCount];
                ref var entity = ref entities[entityId];
                entity.Version = (short)-entity.Version;
            }
            else
            {
                // new entity.
                if (entityCount == entities.Length) Array.Resize(ref entities, entityCount << 1);
                entityId = entityCount++;
                ref var entity = ref entities[entityId];
                entity.Index = entityId;
                entity.Version = 1;
            }
            //EntityCreatedEvent?.Invoke(entity);
            return ref entities[entityId];
        }
        private void DestroyEntityStruct(ref EcsEntity entity)
        {
            var entityId = entity.Index;
            entity.Version = (short)(entity.Version == short.MaxValue ? -1 : -(entity.Version + 1));
            if (recycledEntityCount == recycledEntities.Length) Array.Resize(ref recycledEntities, recycledEntityCount << 1);
            recycledEntities[recycledEntityCount++] = entityId;
        }
        //public int CreateEntity(params Type[] types)
        //{
        //    ref var entity = ref CreateEntity();
        //    ref var archetype = ref GetArchetype(types);
        //    entity.ArchetypeId = archetype.id;
        //    var entityId = entity.Index;
        //    archetype.AddEntity(ref entity);
        //    //Console.WriteLine($"EcsWorld.CreateEntity: {entityId},{archetypeData.entityList.Count}");
        //    return entityId;
        //}
        public int CreateEntity() => CreateEntity(null, 0);
        public int CreateEntity(params Type[] types) => CreateEntity(GetCompnentIds(types), types.Length);
        public int CreateEntity(params byte[] compIds) => CreateEntity(compIds, compIds.Length);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CreateEntity(byte[] compIds, int count)
        {
            ref var entity = ref CreateEntityStruct();
            ref var archetype = ref GetArchetype(compIds, count);
            entity.ArchetypeId = archetype.id;
            var entityId = entity.Index;
            archetype.AddEntity(ref entity);
            //Console.WriteLine($"EcsWorld.CreateEntity: {entityId},{archetypeData.entityList.Count}");
            EntityCreatedEvent?.Invoke(entityId);
            return entityId;
        }
        public void DestroyEntity(int entityId)
        {
            ref var entity = ref entities[entityId];
            if (entity.Version < 0) return;
            var archetypeId = entity.ArchetypeId;
            ref var archetype = ref archetypes[archetypeId];
            archetype.RemoveEntity(ref entity);

            DestroyEntityStruct(ref entity);
            //entity.Version = (short)(entity.Version == short.MaxValue ? -1 : -(entity.Version + 1));
            //if (recycledEntityCount == recycledEntities.Length) Array.Resize(ref recycledEntities, recycledEntityCount << 1);
            //recycledEntities[recycledEntityCount++] = entityId;
            EntityDestroyedEvent?.Invoke(entityId);
        }
        public int CreateEntity<T0>(T0 comp0) where T0 : struct
        {
            var entityId = CreateEntity(typeof(T0));
            SetComponent(entityId, comp0);
            return entityId;
        }
        public int CreateEntity<T0, T1>(T0 comp0, T1 comp1) where T0 : struct where T1 : struct
        {
            var entityId = CreateEntity(typeof(T0), typeof(T1));
            SetComponent(entityId, comp0);
            SetComponent(entityId, comp1);
            return entityId;
        }
        public int CreateEntity<T0, T1, T2>(T0 comp0, T1 comp1, T2 comp2) where T0 : struct where T1 : struct where T2 : struct
        {
            var entityId = CreateEntity(typeof(T0), typeof(T1), typeof(T2));
            SetComponent(entityId, comp0);
            SetComponent(entityId, comp1);
            SetComponent(entityId, comp2);
            return entityId;
        }
        public int CreateEntity<T0, T1, T2, T3>(T0 comp0, T1 comp1, T2 comp2, T3 comp3) where T0 : struct where T1 : struct where T2 : struct where T3 : struct
        {
            var entityId = CreateEntity(typeof(T0), typeof(T1), typeof(T2));
            SetComponent(entityId, comp0);
            SetComponent(entityId, comp1);
            SetComponent(entityId, comp2);
            SetComponent(entityId, comp3);
            return entityId;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEntityValid(int entityId) => entityId >= 0 && entityId < entityCount && entities[entityId].Version > 0;
        public bool HasComponent<T>(int entityId) where T : struct
        {
            var compId = GetComponentId(typeof(T));
            ref var entity = ref entities[entityId];
            var archetypeId = entity.ArchetypeId;
            ref var archetype = ref archetypes[archetypeId];
            return archetype.HasComponent(compId);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int entityId) where T : struct
        {
            var compId = GetComponentId(typeof(T));
            return ref GetComponent<T>(entityId, compId);
            //ref var entity = ref entities[entityId];
            //var archetypeId = entity.ArchetypeId;
            //var chunkId = entity.ArchetypeChunkId;
            //ref var archetype = ref archetypes[archetypeId];
            //return ref archetype.GetComponent<T>(compId, chunkId);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int entityId, byte compId) where T : struct
        {
            ref var entity = ref entities[entityId];
            ref var archetype = ref archetypes[entity.ArchetypeId];
            return ref archetype.GetComponent<T>(compId, entity.ArchetypeChunkId);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T AddComponent<T>(int entityId) where T : struct
        {
            var addCompId = GetComponentId(typeof(T));
            return ref AddComponent<T>(entityId, addCompId);
        }
        public ref T AddComponent<T>(int entityId, byte addCompId) where T : struct
        {
            ref var entity = ref entities[entityId];
            ref var archetype = ref archetypes[entity.ArchetypeId];
            ref var nextArchetype = ref GetNextArchetype(ref archetype, addCompId);
            //move comp data
            var chunkId = entity.ArchetypeChunkId;
            foreach (var compId in archetype.compIds)
            {
                ref var compType = ref GetComponentTypeData(compId);
                compType.CopyChunkComponent(archetype, chunkId, nextArchetype);
                //var comp = archetypeData.GetComponent<T>(compId, chunkId);
                //nextArchetypeData.AddComponent(compId, comp);
            }
            ref var comp = ref nextArchetype.AddComponent<T>(addCompId);
            archetype.RemoveEntity(ref entity);
            nextArchetype.AddEntity(ref entity);
            EntityComponentAddedEvent?.Invoke(entityId, addCompId);
            return ref comp;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(int entityId, T addComp) where T : struct
        {
            var addCompId = GetComponentId(typeof(T));
            AddComponent<T>(entityId, addComp, addCompId);
        }
        public void AddComponent<T>(int entityId, T addComp, byte addCompId) where T : struct
        {
            ref var entity = ref entities[entityId];
            ref var archetype = ref archetypes[entity.ArchetypeId];
            ref var nextArchetype = ref GetNextArchetype(ref archetype, addCompId);
            //move comp data
            var chunkId = entity.ArchetypeChunkId;
            foreach (var compId in archetype.compIds)
            {
                ref var compType = ref GetComponentTypeData(compId);
                compType.CopyChunkComponent(archetype, chunkId, nextArchetype);
                //var comp = archetypeData.GetComponent<T>(compId, chunkId);
                //nextArchetypeData.AddComponent(compId, comp);
            }
            nextArchetype.AddComponent(addCompId, addComp);
            archetype.RemoveEntity(ref entity);
            nextArchetype.AddEntity(ref entity);
            EntityComponentAddedEvent?.Invoke(entityId, addCompId);
        }
        public void RemoveComponent(int entityId, byte removeCompId)
        {
            ref var entity = ref entities[entityId];
            ref var archetype = ref archetypes[entity.ArchetypeId];
            ref var preArchetype = ref GetPreArchetype(ref archetype, removeCompId);
            //move comp data
            var chunkId = entity.ArchetypeChunkId;
            foreach (var compId in archetype.compIds)
            {
                if (compId == removeCompId) continue;
                ref var compType = ref GetComponentTypeData(compId);
                compType.CopyChunkComponent(archetype, chunkId, preArchetype);
                //var comp = archetypeData.GetComponent<T>(compId, chunkId);
                //preArchetypeData.AddComponent<T>(compId, comp);
            }
            archetype.RemoveEntity(ref entity);
            preArchetype.AddEntity(ref entity);
            EntityComponentRemovedEvent?.Invoke(entityId, removeCompId);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(int entityId) where T : struct
        {
            var removeCompId = GetComponentId(typeof(T));
            RemoveComponent(entityId, removeCompId);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponent<T>(int entityId, T comp) where T : struct
        {
            var compId = GetComponentId(typeof(T));
            SetComponent<T>(entityId, comp, compId);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponent<T>(int entityId, T comp, byte compId) where T : struct
        {
            ref var entity = ref entities[entityId];
            ref var archetype = ref archetypes[entity.ArchetypeId];
            archetype.SetComponent(compId, entity.ArchetypeChunkId, comp);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly EcsArchetype GetEntityArchetype(int entityId)
        {
            ref var entity = ref entities[entityId];
            ref var archetype = ref archetypes[entity.ArchetypeId];
            return ref archetype;
        }
        #endregion

        #region Archetype
        internal ref EcsArchetype GetNextArchetype(ref EcsArchetype archetype, byte addCompId)
        {
            var archetypeId = archetype.NextIds[addCompId];
            if (archetypeId > 0)
                return ref archetypes[archetypeId];
            //tempCompCount = archetypeData.compCount + 1;
            bool added = false;
            int length = 0;
            //for (int i = 0; i < archetypeData.compCount; i++)
            foreach (var compId in archetype.compIds)
            {
                //var compId = archetypeData.compIds[i];
                if (!added && addCompId < compId)
                {
                    tempCompIds[length++] = addCompId;
                    added = true;
                }
                tempCompIds[length++] = compId;
            }
            if (!added) tempCompIds[length++] = addCompId;
            ref var nextArchetype = ref GetArchetype(tempCompIds, length);
            archetype.NextIds[addCompId] = nextArchetype.id;
            nextArchetype.PreIds[addCompId] = archetype.id;
            Console.WriteLine($"EcsWorld.GetNextArchetype: {archetype.id}->{nextArchetype.id}, {addCompId}");
            return ref nextArchetype;
        }
        internal ref EcsArchetype GetPreArchetype(ref EcsArchetype archetype, byte removeCompId)
        {
            var archetypeId = archetype.PreIds[removeCompId];
            if (archetypeId > 0)
                return ref archetypes[archetypeId];
            //tempCompIds.Length = archetypeData.compCount - 1;
            int length = 0;
            foreach (var compId in archetype.compIds)
            {
                if (removeCompId != compId)
                    tempCompIds[length++] = compId;
            }
            ref var preArchetype = ref GetArchetype(tempCompIds, length);
            preArchetype.NextIds[removeCompId] = archetype.id;
            archetype.PreIds[removeCompId] = preArchetype.id;
            Console.WriteLine($"EcsWorld.GetPreArchetype: {archetype.id}->{preArchetype.id}, {removeCompId}");
            return ref preArchetype;
        }
        public ref EcsArchetype GetArchetype(params Type[] types)
        {
            if (types == null || types.Length == 0)
                return ref archetypes[0];
            var length = types.Length;
            //tempCompIds.Length = length;
            //for (int i = 0; i < length; i++)
            //    tempCompIds[i] = GetComponentId(types[i]);
            var compIds = GetCompnentIds(types);
            Array.Sort(compIds, 0, length);
            return ref GetArchetype(tempCompIds, length);
        }
        internal ref EcsArchetype GetArchetype(byte[] compIds, int count)
        {
            ref var curArchetype = ref archetypes[0]; //emptyArchetypeData;
            if (compIds == null || count == 0) return ref curArchetype;
            for (int i = 0; i < count; i++)
            {
                var compId = compIds[i];
                var nextArchetypeId = curArchetype.NextIds[compId];
                if (nextArchetypeId == 0)
                {
                    //create new
                    if (archetypeCount == archetypes.Length) Array.Resize(ref archetypes, archetypeCount << 1);
                    nextArchetypeId = archetypeCount++;
                    var archetype = new EcsArchetype(this, nextArchetypeId, compIds, i + 1);
                    archetype.PreIds[compId] = curArchetype.id;
                    curArchetype.NextIds[compId] = nextArchetypeId;
                    archetypes[nextArchetypeId] = archetype;
                    //archetypeCount++;
                    //Console.WriteLine($"EcsArchetypeData.GetArchetypeData: {nextArchetypeId},[{string.Join(",", archetypeData.compIds)}]");
                }
                curArchetype = ref archetypes[nextArchetypeId];
            }
            return ref curArchetype;
        }
        #endregion

        #region Component
        public int RegisterComponent<T>() where T : struct
        {
            var type = typeof(T);
            if (compTypeDict.TryGetValue(type, out var compType))
                return compType.Id;
            var compId = compTypeCount;
            compType = new EcsComponentType
            {
                Id = compId,
                Type = type,
                ComponentArrayCreator = (capacity) => new EcsComponentArray<T>(capacity),
                CopyChunkComponent = (src, chunkId, dest) =>
                {
                    var comp = src.GetComponent<T>(compId, chunkId);
                    dest.AddComponent(compId, comp);
                }
            };
            compTypeDict[type] = compType;
            compTypes[compId] = compType;
            compTypeCount++;
            return compId;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal byte[] GetCompnentIds(params Type[] types)
        {
            for (int i = 0; i < types.Length; i++)
                tempCompIds[i] = GetComponentId(types[i]);
            return tempCompIds;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetComponentId(Type type)
        {
            if (!compTypeDict.TryGetValue(type, out var compType))
                return 0;
            return compType.Id;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Type GetComponentType(byte typeId)
        {
            return compTypes[typeId].Type;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref EcsComponentType GetComponentTypeData(byte typeId) => ref compTypes[typeId];

        #endregion

        #region Query

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsQueryDesc CreateQueryDesc() => recycledQueryDescCount > 0 ? recycledQueryDescs[--recycledQueryDescCount] : new EcsQueryDesc(this);
        public ref readonly EcsQuery GetQuery(int queryId) => ref queries[queryId];
        public ref readonly EcsQuery GetQuery(EcsQueryDesc questDesc)
        {
            var hash = questDesc.hash;
            if (queryIdDict.TryGetValue(hash, out var queryId))
            {
                questDesc.Reset();
                if (recycledQueryDescCount == recycledQueryDescs.Length) Array.Resize(ref recycledQueryDescs, recycledQueryDescCount << 1);
                recycledQueryDescs[recycledQueryDescCount++] = questDesc;
                return ref queries[queryId];
            }
            if (queryCount == queries.Length)
            {
                Array.Resize(ref queries, queryCount << 1);
                Array.Resize(ref queryDescs, queryCount << 1);
            }
            queryId = queryCount++;
            var query = new EcsQuery(this, queryId);
            queryIdDict[hash] = queryId;
            queries[queryId] = query;
            queryDescs[queryId] = questDesc;
            QueryCreatedEvent?.Invoke(queryId);
            return ref queries[queryId];
        }
        #endregion

        #region System
        //public ISystem CreateSystem(Type type)
        //{

        //}
        public IEcsSystem GetSystem(Type type)
        {
            systemDict.TryGetValue(type, out var system);
            return system;
        }
        public void AddSystem(IEcsSystem system)
        {
            var type = system.GetType();
            if (systemDict.ContainsKey(type))
                return;
            systemDict.Add(type, system);
            systemList.Add(system);
            if (isInited)
            {
                system.Init(this);
                //if (system is IEcsInitSystem initSystem)
                //    initSystem.Init(this);
                //if (system is IEcsRunSystem runSystem)
                //    AddRunSystem(runSystem);
            }
        }
        public void DestroySystem(IEcsSystem system)
        {
            if (isDestroyed) return;
            var type = system.GetType();
            if (systemDict.ContainsKey(type))
                return;
            systemDict.Remove(type);
            systemList.Remove(system);
            system.Dispose();
            //if (system is IEcsDestroySystem destroySystem)
            //    destroySystem.Destroy(this);
            //if (system is IEcsRunSystem runSystem)
            //    RemoveRunSystem(runSystem);
        }
        //private void AddRunSystem(IEcsRunSystem runSystem)
        //{
        //    if (runSystemCount == runSystems.Length) Array.Resize(ref runSystems, runSystemCount << 1);
        //    runSystems[runSystemCount++] = runSystem;
        //}
        //private void RemoveRunSystem(IEcsRunSystem runSystem)
        //{
        //    var index = Array.IndexOf(runSystems, runSystem);
        //    if (index < 0) return;
        //    runSystemCount--;
        //    for (var i = index; i < runSystemCount; i++)
        //        runSystems[i] = runSystems[i + 1];
        //}
        #endregion

        public struct Config
        {
            public int EntityCapacity;
            public int RecycledEntityCapacity;
            public int ArchetypeCapacity;
            public int QueryCapacity;
            //public int SystemCapacity;
            internal const int EntityCapacityDefault = 512;
            internal const int RecycledEntityCapacityDefault = 512;
            internal const int ArchetypeCapacityDefault = 512;
            internal const int QueryCapacityDefault = 512;
            //internal const int SystemCapacityDefault = 64;
        }
    }
    #endregion
}
