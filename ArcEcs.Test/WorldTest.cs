using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Poly.ArcEcs.Test
{
    [TestClass]
    public partial class WorldTest
    {
        public struct CompA : IEquatable<CompA>
        {
            public int Value;
            public bool Equals(CompA other) => Value == other.Value;
            public override string ToString() => $"CompA:{{{Value}}}";
        }

        public struct CompB : IEquatable<CompB>
        {
            public int Value;
            public string Str;
            public bool Equals(CompB other) => Value == other.Value && Str == other.Str;
            public override string ToString() => $"CompB:{{{Value},{Str}}}";
        }
        public struct CompC : IEquatable<CompC>, IDisposable
        {
            public int Value;

            public void Dispose() => Console.WriteLine($"CompC.Dispose!");
            public bool Equals(CompC other) => Value == other.Value;
        }
        public struct CompD : IEquatable<CompD>, IDisposable
        {
            public int Value;
            public void Dispose() => Console.WriteLine($"CompD.Dispose!");
            public bool Equals(CompD other) => Value == other.Value;
        }

        private static World world;

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
            world = new World();
        }
        [TestCleanup]
        public void TestCleanup()
        {
            world.Dispose();
            world = null;
        }
        [TestMethod]
        public void CommonTest()
        {
            var compB = new CompB { Value = 12 };

            var entity = world.CreateEntity();
            var archetype = world.GetEntityArchetype(entity);
            Assert.AreEqual(0, archetype.Id);
            Assert.AreEqual(1, archetype.EntityCount);

            world.AddComponent(entity, new CompA { Value = 10 });
            //[A]
            archetype = world.GetEntityArchetype(entity);
            Assert.AreEqual(1, archetype.Id);
            Assert.AreEqual(1, archetype.CompCount);

            world.AddComponent(entity, compB);
            //[A,B]
            archetype = world.GetEntityArchetype(entity);
            Assert.AreEqual(2, archetype.Id);
            Assert.AreEqual(2, archetype.CompCount);

            world.AddComponent(entity, new CompC());
            //[A,B,C]
            archetype = world.GetEntityArchetype(entity);
            Assert.AreEqual(3, archetype.Id);
            Assert.AreEqual(3, archetype.CompCount);

            Assert.IsFalse(world.HasComponent<CompD>(entity));
            Assert.IsTrue(world.HasComponent<CompB>(entity));

            ref var compB1 = ref world.GetComponent<CompB>(entity);
            var str = "huo";
            Assert.AreEqual(compB, compB1);
            compB1.Value = 100;
            compB1.Str = str;
            compB1 = ref world.GetComponent<CompB>(entity);
            Assert.AreEqual(str, compB1.Str);

            world.SetComponent(entity, new CompB { Value = 13, Str = "dian" });
            compB1 = ref world.GetComponent<CompB>(entity);
            Assert.AreEqual(13, compB1.Value);
            Assert.AreEqual("dian", compB1.Str);

            var archetypeAC = world.GetArchetype<CompC, CompA>();

            world.RemoveComponent<CompB>(entity);
            //[A,C]
            archetype = world.GetEntityArchetype(entity);
            Assert.AreEqual(archetypeAC.Id, archetype.Id);
            Assert.AreEqual(2, archetype.CompCount);
            Assert.IsFalse(world.HasComponent<CompB>(entity));

            entity = world.CreateEntity<CompA, CompC>();
            //[A,C]
            archetype = world.GetEntityArchetype(entity);
            Assert.AreEqual(archetypeAC.Id, archetype.Id);
            Assert.AreEqual(2, archetype.EntityCount);

            entity = world.CreateEntity(new CompA { Value = 13 }, new CompC { Value = 17 });
            //[A,C]
            archetype = world.GetEntityArchetype(entity);
            Assert.AreEqual(archetypeAC.Id, archetype.Id);
            Assert.AreEqual(3, archetype.EntityCount);

            world.RemoveComponent<CompA>(entity);
            ref var compC = ref world.GetComponent<CompC>(entity);
            Assert.AreEqual(17, compC.Value);

            world.RemoveComponent<CompC>(entity);
            //[]
            archetype = world.GetEntityArchetype(entity);
            Assert.AreEqual(0, archetype.Id);
            Assert.AreEqual(1, archetype.EntityCount);
        }
        [TestMethod]
        public void ComponentTest()
        {
            //var compB = new CompB { Value = 11 };

            var entity = world.CreateEntity(new CompA { Value = 10 });
            world.AddComponent(entity, new CompB { Value = 11 });
            
            ref var compB1 = ref world.GetComponent<CompB>(entity);
            Assert.AreEqual(11, compB1.Value);

            var entity1 = world.CreateEntity();
            world.AddComponent(entity1, new CompA { Value = 20 });
            //var entity1 = world.CreateEntity(new CompA { Value = 20 });
            world.AddComponent(entity1, new CompB { Value = 21 });
            ref var compA1 = ref world.GetComponent<CompA>(entity1);
            Assert.AreEqual(20, compA1.Value);
            compB1 = ref world.GetComponent<CompB>(entity1);
            Assert.AreEqual(21, compB1.Value);

            world.AddComponent(entity, new CompC { Value = 12 });
            compB1 = ref world.GetComponent<CompB>(entity);
            Assert.AreEqual(11, compB1.Value);

            world.AddComponent(entity, new CompD { Value = 13 });
            world.RemoveComponent<CompC>(entity);
            world.RemoveComponent<CompD>(entity);
            compB1 = ref world.GetComponent<CompB>(entity);
            Assert.AreEqual(11, compB1.Value);
        }
        [TestMethod]
        public void QueryTest()
        {
            var entityA = world.CreateEntity<CompA>();
            var entityB = world.CreateEntity<CompB>();
            var entityABD = world.CreateEntity<CompA, CompB, CompD>();
            var entityABC = world.CreateEntity<CompA, CompB, CompC>();
            var entityAC = world.CreateEntity<CompA, CompC>();
            var entityBD0 = world.CreateEntity<CompB, CompD>();
            var entityBD1 = world.CreateEntity<CompB, CompD>();
            var entityBC = world.CreateEntity<CompB, CompC>();
            var entityAB = world.CreateEntity<CompB, CompA>();
            var entityAD = world.CreateEntity<CompA, CompD>();

            var archetypeBD = world.GetArchetype<CompD, CompB>();
            Assert.AreEqual(2, archetypeBD.EntityCount);

            var queryDesc = world.CreateQueryDesc().WithAll<CompB, CompA>().WithNone<CompC>().Build();
            var query = world.GetQuery(queryDesc);
            Assert.AreEqual(1, world.QueryCount);
            //entityABD,entityAB
            Assert.AreEqual(2, query.GetEntityCount());
            Assert.IsFalse(query.Matchs(entityBC));
            Assert.IsTrue(query.Matchs(entityABD));

            //Set comp
            query.ForEach((Entity entity, ref CompB compB) =>
            {
                compB.Value = 13;
            });
            ref var compB = ref world.GetComponent<CompB>(entityABD);
            Assert.AreEqual(13, compB.Value);

            //Remove comp: entityABD -> entityBD
            query.ForEach((Entity entity, ref CompB compB) =>
            {
                if (world.HasComponent<CompD>(entity))
                {
                    world.RemoveComponent<CompA>(entity);
                }
            });
            Assert.AreEqual(1, query.GetEntityCount());
            Assert.AreEqual(3, archetypeBD.EntityCount);//BD

            Assert.IsFalse(world.HasComponent<CompA>(entityABD));
            world.AddComponent<CompA>(entityABD);
            Assert.IsTrue(query.Matchs(entityABD));
            //var entityABCD = world.CreateEntity(typeof(ComponentA), typeof(ComponentB), typeof(ComponentC), typeof(ComponentD));
            //entityAB,entityABD
            Assert.AreEqual(2, query.GetEntityCount());

            //TODO
            var index = 0;
            query.ForEach((Entity entity, ref CompB compB) =>
            {
                index++;
                world.CreateEntity<CompB, CompD, CompA>();
            });
            Assert.AreEqual(3, index);
            Assert.AreEqual(5, query.GetEntityCount());
        }

        //public class System : IEcsSystem
        //{
        //    private EcsQuery query;
        //    public void Init(EcsWorld world)
        //    {
        //        var queryDesc = world.CreateQueryDesc().WithAll<ComptA>().WithNone<CompB>().Build();
        //        query = world.GetQuery(queryDesc);
        //    }
        //    public void Dispose()
        //    {
        //    }
        //    public void Update()
        //    {
        //        query.ForEach((EcsEntity entity, ref ComptA compA) =>
        //        {
        //            compA.Value++;
        //        });
        //    }
        //}
    }
}