using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Poly.ArcEcs.Test
{
    [TestClass]
    public partial class EcsWorldTest
    {
        public struct ComponentA : IEquatable<ComponentA>
        {
            public int Value;
            public bool Equals(ComponentA other) => Value == other.Value;
        }

        public struct ComponentB : IEquatable<ComponentB>
        {
            public int Value;
            public string Str;
            public bool Equals(ComponentB other) => Value == other.Value && Str == other.Str;
        }
        public struct ComponentC : IEquatable<ComponentC>
        {
            public int Value;
            public bool Equals(ComponentC other) => Value == other.Value;
        }
        public struct ComponentD : IEquatable<ComponentD>
        {
            public int Value;
            public bool Equals(ComponentD other) => Value == other.Value;
        }

        private static EcsWorld world;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
        }
        [ClassCleanup]
        public static void ClassCleanup()
        {
        }
        [TestInitialize]
        public void TestInitialize()
        {
            world = new EcsWorld();
            world.RegisterComponent<ComponentA>();
            world.RegisterComponent<ComponentB>();
            world.RegisterComponent<ComponentC>();
            world.RegisterComponent<ComponentD>();
        }
        [TestCleanup]
        public void TestCleanup()
        {
            world.Destroy();
            world = null;
        }
        [TestMethod]
        public void CommonTest()
        {
            var componentB = new ComponentB { Value = 12 };

            var entity = world.CreateEntity();
            var archetype = world.GetEntityArchetype(entity);
            Assert.AreEqual(0, archetype.Id);
            Assert.AreEqual(1, archetype.EntityCount);

            world.AddComponent(entity, new ComponentA());
            //[A]
            archetype = world.GetEntityArchetype(entity);
            Assert.AreEqual(1, archetype.Id);
            Assert.AreEqual(1, archetype.CompCount);

            world.AddComponent(entity, componentB);
            //[A,B]
            archetype = world.GetEntityArchetype(entity);
            Assert.AreEqual(2, archetype.Id);
            Assert.AreEqual(2, archetype.CompCount);

            world.AddComponent(entity, new ComponentC());
            //[A,B,C]
            archetype = world.GetEntityArchetype(entity);
            Assert.AreEqual(3, archetype.Id);
            Assert.AreEqual(3, archetype.CompCount);

            Assert.IsFalse(world.HasComponent<ComponentD>(entity));
            Assert.IsTrue(world.HasComponent<ComponentB>(entity));

            ref var compB = ref world.GetComponent<ComponentB>(entity);
            var str = "huo";
            Assert.AreEqual(componentB, compB);
            compB.Value = 100;
            compB.Str = str;
            compB = ref world.GetComponent<ComponentB>(entity);
            Assert.AreEqual(str, compB.Str);

            world.SetComponent(entity, new ComponentB { Value = 13, Str = "dian" });
            compB = ref world.GetComponent<ComponentB>(entity);
            Assert.AreEqual(13, compB.Value);
            Assert.AreEqual("dian", compB.Str);

            var archetypeAC = world.GetArchetype(typeof(ComponentC), typeof(ComponentA));

            world.RemoveComponent<ComponentB>(entity);
            //[A,C]
            archetype = world.GetEntityArchetype(entity);
            Assert.AreEqual(archetypeAC.Id, archetype.Id);
            Assert.AreEqual(2, archetype.CompCount);
            Assert.IsFalse(world.HasComponent<ComponentB>(entity));

            entity = world.CreateEntity(typeof(ComponentA), typeof(ComponentC));
            //[A,C]
            archetype = world.GetEntityArchetype(entity);
            Assert.AreEqual(archetypeAC.Id, archetype.Id);
            Assert.AreEqual(2, archetype.EntityCount);

            entity = world.CreateEntity(new ComponentA { Value = 13 }, new ComponentC { Value = 17 });
            //[A,C]
            archetype = world.GetEntityArchetype(entity);
            Assert.AreEqual(archetypeAC.Id, archetype.Id);
            Assert.AreEqual(3, archetype.EntityCount);

            world.RemoveComponent<ComponentA>(entity);
            ref var compC = ref world.GetComponent<ComponentC>(entity);
            Assert.AreEqual(17, compC.Value);

            world.RemoveComponent<ComponentC>(entity);
            //[]
            archetype = world.GetEntityArchetype(entity);
            Assert.AreEqual(0, archetype.Id);
            Assert.AreEqual(1, archetype.EntityCount);
        }

        [TestMethod]
        public void QueryTest()
        {
            var entityA = world.CreateEntity(typeof(ComponentA));
            var entityB = world.CreateEntity(typeof(ComponentB));
            var entityABD = world.CreateEntity(typeof(ComponentA), typeof(ComponentB), typeof(ComponentD));
            var entityABC = world.CreateEntity(typeof(ComponentA), typeof(ComponentB), typeof(ComponentC));
            var entityAC = world.CreateEntity(typeof(ComponentA), typeof(ComponentC));
            var entityBD0 = world.CreateEntity(typeof(ComponentB), typeof(ComponentD));
            var entityBD1 = world.CreateEntity(typeof(ComponentB), typeof(ComponentD));
            var entityBC = world.CreateEntity(typeof(ComponentB), typeof(ComponentC));
            var entityAB = world.CreateEntity(typeof(ComponentA), typeof(ComponentB));
            var entityAD = world.CreateEntity(typeof(ComponentA), typeof(ComponentD));

            var archetypeBD = world.GetArchetype(typeof(ComponentD), typeof(ComponentB));
            Assert.AreEqual(2, archetypeBD.EntityCount);

            var queryDesc = world.CreateQueryDesc().WithAll<ComponentB, ComponentA>().WithNone<ComponentC>().Build();
            var query = world.GetQuery(queryDesc);
            Assert.AreEqual(1, world.QueryCount);
            //entityABD,entityAB
            Assert.AreEqual(2, query.GetEntityCount());
            Assert.IsFalse(query.Matchs(entityBC));
            Assert.IsTrue(query.Matchs(entityABD));

            //Set comp
            query.ForEach((EcsEntity entity, ref ComponentB compB) =>
            {
                compB.Value = 13;
            });
            ref var compB = ref world.GetComponent<ComponentB>(entityABD);
            Assert.AreEqual(13, compB.Value);

            //Remove comp: entityABD -> entityBD
            query.ForEach((EcsEntity entity, ref ComponentB compB) =>
            {
                if (world.HasComponent<ComponentD>(entity))
                {
                    world.RemoveComponent<ComponentA>(entity);
                }
            });
            Assert.AreEqual(1, query.GetEntityCount());
            Assert.AreEqual(3, archetypeBD.EntityCount);//BD

            Assert.IsFalse(world.HasComponent<ComponentA>(entityABD));
            world.AddComponent<ComponentA>(entityABD);
            Assert.IsTrue(query.Matchs(entityABD));
            //var entityABCD = world.CreateEntity(typeof(ComponentA), typeof(ComponentB), typeof(ComponentC), typeof(ComponentD));
            //entityAB,entityABD
            Assert.AreEqual(2, query.GetEntityCount());

            //TODO
            var index = 0;
            query.ForEach((EcsEntity entity, ref ComponentB compB) =>
            {
                index++;
                world.CreateEntity(typeof(ComponentA), typeof(ComponentB), typeof(ComponentD));
            });
            Assert.AreEqual(3, index);
            Assert.AreEqual(5, query.GetEntityCount());
        }

        public class System : IEcsSystem
        {
            private EcsQuery query;
            public void Init(EcsWorld world)
            {
                var queryDesc = world.CreateQueryDesc().WithAll<ComponentA>().WithNone<ComponentB>().Build();
                query = world.GetQuery(queryDesc);
            }
            public void Dispose()
            {
            }
            public void Update()
            {
                query.ForEach((EcsEntity entity, ref ComponentA compA) =>
                {
                    compA.Value++;
                });
            }
        }
    }
}