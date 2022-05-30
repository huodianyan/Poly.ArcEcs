# ArcEcs
A minimal archetypal ECS focused on compatibility and performance.

## Features
- Zero dependencies
- Minimal ECS core
- Lightweight and fast
- Zero/small memory allocations/footprint
- Struct based Entity/Component
- Entity/Component managed by archetypes and archetypal graph
- Query traverse archetype, No entity cache
- 255 components, unlimited entity, archetype, query, system
- Support Remove component (eg. event) in the end of the world system update
- Adapted to all C# game engine
- Support Web by same framework*
- Support serialzation, network*

# Table of content
- [Installation](#installation)
- [Overview](#overview)
  - [Entity](#entity)
  - [Component](#component)

# Installation

# Overview

```csharp

struct CompA { public int Value; }
struct CompB { public int Value; }

var world = new World();
world.CreateEntity<CompA, CompB>();

var queryDesc = world.CreateQueryDesc().WithAll<CompA>().WithNone<CompB>().Build();
var query = world.GetQuery(queryDesc);
query.ForEach((int entity, ref CompA compA) =>
{
    compA.Value++;
});

world.Dispose();

```
## Entity

## Component

## System

## Query

## Archetype

# Extensions
- [huodianyan/Poly.AcrEcs.Unity](https://github.com/huodianyan/Poly.AcrEcs.Unity)

# License
The software is released under the terms of the [MIT license](./LICENSE.md).

# FAQ

# References

## Documents
- [Building an ECS #2: Archetypes and Vectorization](https://medium.com/@ajmmertens/building-an-ecs-2-archetypes-and-vectorization-fe21690805f9)
- [ECS back and forth Part 2 - Where are my entities?](https://skypjack.github.io/2019-03-07-ecs-baf-part-2/)
- [Introducing ECSY: an Entity Component System framework for the Web](https://blog.mozvr.com/introducing-ecsy/)

## Projects
- [skypjack/entt](https://github.com/skypjack/entt)
- [SanderMertens/flecs](https://github.com/SanderMertens/flecs)
- [LeoECSCommunity/ecslite](https://github.com/LeoECSCommunity/ecslite)
- [LeoECSCommunity/ecs](https://github.com/LeoECSCommunity/ecs)
- [voledyhil/MiniEcs](https://github.com/voledyhil/MiniEcs)
- [craftworkgames/MonoGame.Extended](https://github.com/craftworkgames/MonoGame.Extended)
	- [Entities](https://www.monogameextended.net/docs/features/entities/entities)
- [sschmid/Entitas-CSharp](https://github.com/sschmid/Entitas-CSharp)
	- [rocwood/Entitas-Lite](https://github.com/rocwood/Entitas-Lite)
- [EcsRx/ecsrx](https://github.com/EcsRx/ecsrx)
- [Unity-Technologies/EntityComponentSystemSamples](https://github.com/Unity-Technologies/EntityComponentSystemSamples)

## Benchmarks
- [Doraku/Ecs.CSharp.Benchmark](https://github.com/Doraku/Ecs.CSharp.Benchmark)
- [noctjs/ecs-benchmark](https://github.com/noctjs/ecs-benchmark)
- [ddmills/js-ecs-benchmarks](https://github.com/ddmills/js-ecs-benchmarks)

## Tech Docs
- [Write safe and efficient C# code](https://docs.microsoft.com/en-us/dotnet/csharp/write-safe-efficient-code)
- [Boxing and Unboxing (C# Programming Guide)](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing)
