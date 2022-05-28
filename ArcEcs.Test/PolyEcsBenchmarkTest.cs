using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Poly.ArcEcs.Test
{
    [TestClass]
    public partial class PolyEcsBenchmarkTest
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            SetupPolyEcs();
        }
        [ClassCleanup]
        public static void ClassCleanup()
        {
            world.Dispose();
        }
        [TestInitialize]
        public void TestInitialize()
        {
        }
        [TestCleanup]
        public void TestCleanup()
        {
        }

        private struct CompA
        {
            public int Value;
        }
        private struct CompB
        {
            public int Value;
        }
        private struct CompC
        {
            public int Value;
        }
        private struct CompD
        {
            public int Value;
        }
        private static EcsWorld world;
        public static void SetupPolyEcs()
        {
            world = new EcsWorld();
            //world.RegisterComponent<ComponentA>();
            //world.RegisterComponent<ComponentB>();
            //world.RegisterComponent<ComponentC>();
            //world.RegisterComponent<ComponentD>();

            for (int i = 0; i < 15000; ++i)
            {
                world.CreateEntity<CompA, CompB, CompD>();
                world.CreateEntity<CompA, CompC>();
                world.CreateEntity<CompB, CompD>();
                world.CreateEntity<CompB, CompD>();
                world.CreateEntity<CompC, CompB>();
                world.CreateEntity<CompA, CompB>();
                world.CreateEntity<CompA, CompD>();
                world.CreateEntity<CompA, CompB, CompC>();
            }
        }
        [TestMethod]
        public void PolyEcsStressTest()
        {
            //List<int> entities = new List<int>();
            EcsEntity[] entities = new EcsEntity[10000];
            int index = 0;

            for (int i = 0; i < 1000; i++)
            {
                var queryDescA = world.CreateQueryDesc().WithAll<CompA>().Build();
                var queryA = world.GetQuery(queryDescA);
                var queryDescB = world.CreateQueryDesc().WithAll<CompB>().Build();
                var queryB = world.GetQuery(queryDescB);
                var queryDescC = world.CreateQueryDesc().WithAll<CompC>().Build();
                var queryC = world.GetQuery(queryDescC);
                var queryDescD = world.CreateQueryDesc().WithAll<CompD>().Build();
                var queryD = world.GetQuery(queryDescD);

                var queryDescAB = world.CreateQueryDesc().WithAll<CompA, CompB>().Build();
                var queryAB = world.GetQuery(queryDescAB);
                var queryDescAC = world.CreateQueryDesc().WithAll<CompA, CompC>().Build();
                var queryAC = world.GetQuery(queryDescAC);
                var queryDescAD = world.CreateQueryDesc().WithAll<CompA, CompD>().Build();
                var queryAD = world.GetQuery(queryDescAD);

                var queryDescABC = world.CreateQueryDesc().WithAll<CompA, CompB, CompC>().Build();
                var queryABC = world.GetQuery(queryDescABC);
                var queryDescABD = world.CreateQueryDesc().WithAll<CompA, CompB, CompD>().Build();
                var queryABD = world.GetQuery(queryDescABD);

                var queryDescABCD = world.CreateQueryDesc().WithAll<CompA, CompB, CompC, CompD>().Build();
                var queryABCD = world.GetQuery(queryDescABCD);
            }

            for (int i = 0; i < 1000; i++)
            {
                var entityABD = world.CreateEntity<CompA, CompB, CompD>();
                var entityAC = world.CreateEntity<CompA, CompC>();
                var entityBD0 = world.CreateEntity<CompB, CompD>();
                var entityBD1 = world.CreateEntity<CompB, CompD>();
                var entityBC = world.CreateEntity<CompB, CompC>();
                var entityAB = world.CreateEntity<CompA, CompB>();
                var entityAD = world.CreateEntity<CompA, CompD>();
                var entityABC = world.CreateEntity<CompA, CompB, CompC>();
                var entityABCD = world.CreateEntity<CompA, CompB, CompD, CompC>();

                entities[index++] = entityABD;
                entities[index++] = entityAC;
                entities[index++] = entityBD0;
                entities[index++] = entityBD1;
                entities[index++] = entityBC;
                entities[index++] = entityAB;
                entities[index++] = entityAD;
                entities[index++] = entityABC;
                entities[index++] = entityABCD;
            }

            //for (int i = 0; i < 1000; i++)
            //{
            //    PolyEcsForEachOneComp();
            //    PolyEcsForEachTwoComp();
            //    PolyEcsForEachThreeComp();
            //    PolyEcsForEachFourComp();
            //}
            for (int i = 0; i < index; i++)
            {
                world.DestroyEntity(entities[i]);
            }
        }

        [TestMethod]
        public void PolyEcsForEachOneComp()
        {
            var queryDesc = world.CreateQueryDesc().WithAll<CompA>().WithNone<CompB, CompD>().Build();
            var query = world.GetQuery(queryDesc);
            query.ForEach((EcsEntity entity, ref CompA compA) =>
            {
                compA.Value++;
            });
        }

        [TestMethod]
        public void PolyEcsForEachTwoComp()
        {
            var queryDesc = world.CreateQueryDesc().WithAll<CompA, CompC>().WithNone<CompB, CompD>().Build();
            var query = world.GetQuery(queryDesc);
            query.ForEach((EcsEntity entity, ref CompA compA, ref CompC compC) =>
            {
                compA.Value++;
                compC.Value++;
            });
        }
        [TestMethod]
        public void PolyEcsForEachThreeComp()
        {
            var queryDesc = world.CreateQueryDesc().WithAll<CompA, CompB, CompC>().WithNone<CompD>().Build();
            var query = world.GetQuery(queryDesc);
            query.ForEach((EcsEntity entity, ref CompA compA, ref CompB compB, ref CompC compC) =>
            {
                compA.Value++;
                compB.Value++;
                compC.Value++;
            });
        }
        [TestMethod]
        public void PolyEcsForEachFourComp()
        {
            var queryDesc = world.CreateQueryDesc().WithAll<CompA, CompB, CompC, CompD>().Build();
            var query = world.GetQuery(queryDesc);
            //Console.WriteLine($"PolyEcsForEachFourComp: {query.GetEntityCount()}");
            query.ForEach((EcsEntity entity, ref CompA compA, ref CompB compB, ref CompC compC, ref CompD compD) =>
            {
                compA.Value++;
                compB.Value++;
                compC.Value++;
                compD.Value++;
            });
        }

        [TestMethod]
        public void PolyEcsQueryTest1()
        {
            var compAId = world.GetComponentId<CompA>();
            var compBId = world.GetComponentId<CompB>();
            var compCId = world.GetComponentId<CompC>();
            var compDId = world.GetComponentId<CompD>();

            var queryDescA = world.CreateQueryDesc().WithAll<CompA>().Build();
            var queryA = world.GetQuery(queryDescA);
            //queryA.ForEach((int entity, ref ComponentA compA) =>
            queryA.ForEach((EcsEntity entity) =>
            {
                //if(world.HasComponent<ComponentB>(entity) && world.HasComponent<ComponentC>(entity))
                world.CreateEntity(compAId, compBId, compCId, compDId);
            });

            var queryDescABCD = world.CreateQueryDesc().WithAll<CompA, CompB, CompC, CompD>().Build();
            var queryABCD = world.GetQuery(queryDescABCD);
            //queryABCD.ForEach((int entity, ref ComponentA compA, ref ComponentB compB, ref ComponentC compC, ref ComponentD compD) =>
            queryABCD.ForEach((EcsEntity entity) =>
            {
                //if(world.HasComponent<ComponentB>(entity) && world.HasComponent<ComponentC>(entity))
                world.DestroyEntity(entity);
            });
        }

        [TestMethod]
        public void PolyEcsQueryTest2()
        {
            var compDId = world.GetComponentId<CompD>();
            var queryDescABC = world.CreateQueryDesc().WithAll<CompA, CompB, CompC>().WithNone<CompD>().Build();
            var queryABC = world.GetQuery(queryDescABC);
            //Console.WriteLine($"{GetType().Name}.PolyEcsQueryTest2: {queryABC.GetEntityCount()}");
            //queryABC.ForEach((int entity, ref ComponentA compA, ref ComponentB compB, ref ComponentC compC) =>
            queryABC.ForEach((EcsEntity entity) =>
            {
                //world.AddComponent<ComponentD>(entity);
                world.AddComponent<CompD>(entity, default, compDId);
            });
            //Console.WriteLine($"{GetType().Name}.PolyEcsQueryTest2: {queryABC.GetEntityCount()}");

            var queryDescABCD = world.CreateQueryDesc().WithAll<CompA, CompB, CompC, CompD>().Build();
            var queryABCD = world.GetQuery(queryDescABCD);
            //Console.WriteLine($"{GetType().Name}.PolyEcsQueryTest2: {queryABCD.GetEntityCount()}");
            queryABCD.ForEach((EcsEntity entity) =>
            //queryABCD.ForEach((int entity, ref ComponentA compA, ref ComponentB compB, ref ComponentC compC, ref ComponentD compD) =>
            {
                //world.RemoveComponent<ComponentD>(entity);
                world.RemoveComponent(entity, compDId);
            });
            //Console.WriteLine($"{GetType().Name}.PolyEcsQueryTest2: {queryABCD.GetEntityCount()}");
        }
    }
}