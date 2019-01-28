[unity]:https://github.com/outerminds/Entia.Unity
[logo]:https://github.com/outerminds/Entia/blob/master/Resources/Logo.png
[releases]:https://github.com/outerminds/Entia/releases
[wiki]:https://github.com/outerminds/Entia/wiki/Home
[wiki/entity]:https://github.com/outerminds/Entia/wiki/Entity
[wiki/component]:https://github.com/outerminds/Entia/wiki/Component
[wiki/system]:https://github.com/outerminds/Entia/wiki/System
[wiki/world]:https://github.com/outerminds/Entia/wiki/World
[wiki/resource]:https://github.com/outerminds/Entia/wiki/Resource
[wiki/group]:https://github.com/outerminds/Entia/wiki/Group
[wiki/message]:https://github.com/outerminds/Entia/wiki/Message
[wiki/node]:https://github.com/outerminds/Entia/wiki/Node
[wiki/controller]:https://github.com/outerminds/Entia/wiki/Controller
[wiki/phase]:https://github.com/outerminds/Entia/wiki/Phase
[wiki/queryable]:https://github.com/outerminds/Entia/wiki/Queryable
[wiki/injectable]:https://github.com/outerminds/Entia/wiki/Injectable

# ![Entia][logo]

**Entia** is a free, open-source, data-oriented, highly performant, parallelized and extensible **ECS** framework writtten in C# especially for game development. It takes advantage of the latest C#7+ features to represent state exclusively with contiguous _structs_. No indirection, no boxing, no garbage collection and no cache misses.

For the full Unity game engine integration of the framework, see [**Entia.Unity**][unity].

___
### Table of contents
<!--ts-->
   * [Download][releases]
   * [Getting Started](#getting-started)
   * [The Basics](#the-basics)
   * [More Concepts](#more-concepts)
   * [Recurrent Usage Patterns](#recurrent-patterns)
   * [Wiki][wiki]
<!--te-->

# Getting Started
- [Download][releases] the most recent stable version of Entia.
- Add _Entia.dll_ and _Entia.Core.dll_ as dependencies in your _csproj_ file.
- Optionally install the Visual Studio extension _Entia.Analyze.vsix_ to get _Entia_ specific code analysis.
- Here is a snippet of code to get you up and running:
```csharp
using Entia;
using Entia.Core;
using Entia.Modules;
using static Entia.Nodes.Node;

var world = new World();
var controllers = world.Controllers();
var resources = world.Resources();

// As the name suggest, this execution node will execute its children in sequential order.
var node = Sequence(
	// Insert systems here using 'System<T>()' where 'T' is your system type.
	System<Systems.Motion>(),
	System<Systems.ControllerInput>(),
	System<Systems.Health>());
	
// A controller is built from the node to allow the execution of your systems.
if (controllers.Control(node).TryValue(out var controller))
{
	// This resource will allow the application to close.
	ref var game = ref resources.Get<Game>();
	
	// A typical execution routine.
	controller.Initialize();
	while (!game.Quit) controller.Run();
	controller.Dispose();
}
```
- For more details, please consult the [wiki][wiki].

# The Basics
**ECS** stands for **E**ntity, **C**omponent, **S**ystem. I will go into details about what each of those are, but they are going to be the elements that allow you to program efficiently and pleasantly in a data-oriented style. But what does 'data-oriented' mean, you ask? Well, data-oriented programming (DOP) is another paradigm just like object-oriented programming (OOP). It is just a different way to solve the same problems that OOP can already solve but in a different style and (hopefully) with less hassle. An essential difference between the two is that DOP separates _data_ and _logic_ rather than unifying them within the same container. Now I won't go into details about what DOP is or why it is a good idea since many more knowledgeable articles already exist on the subject, but it has some important implications for performance and for the design of programs. Unfortunately for us, C# is much more of a OOP language than a DOP language, so we have to build tooling to make it possible and easy to do, thus the framework.

Ok, back to **ECS**. Most programmers have heard at some point that it is better to use composition over inheritance. That is because inheritance, even when well designed, is more rigid and harder to change after the fact compared to composed equivalent and in game development, things tend to change all the time in unexpected ways. **ECS** takes the idea of composition to the extreme by removing inheritance completely. This effectively flattens the structure of programs and makes them much more granular and easier to assemble. So here's a brief definition the essential elements of **ECS**:

-   [**E**ntity][wiki/entity]: an unique identifier (implemented as a simple integer).
-   [**C**omponent][wiki/component]: a chunk of data associated with an **E**ntity.
-   [**S**ystem][wiki/system]: a piece of logic that processes a subset of all **E**ntities and their associated **C**omponents.

So an **E**ntity can be conceptually thought of as a container for **C**omponents, but this could be misleading since an **E**ntity does not contain anything as it is only an identifier. All it does is group **C**omponents together such that they can be queried by systems and as such it relates more to a key in a database table where the columns would be the **C**omponents than it does to a bag of **C**omponents.

As for **C**omponents, they must remain inert, meaning that they do not hold any logic whatsoever. No methods, no constructors, no destructors, no properties, no events, no nothing except plain and simple _public_ fields. This ensures that **C**omponents are easy to understand, predictable, easy to serialize and have a good memory layout. This might be surprising and/or worrying for a mind that is used to control the access to their object's data with properties and/or methods but I'm telling you that as soon as you let go of the idea of protecting/hiding data you eventually realize that it was not strictly necessary and that everything is actually alright.

Since **E**ntities are just identifiers and **C**omponents are just inert chunks of data, we need something to actually does something in this program. **S**ystems are the last missing piece. They are conceptually the equivalent to a function that take all the existing **E**ntities and **C**omponents, filters the ones it is interested in and processes them in some way. This means that ideally, **S**ystems do not hold any state since all the game state exists exclusively within **C**omponents. I say ideally because for optimization purposes, some **S**ystem local state may be needed.

# More Concepts
**E**ntities, **C**omponents and **S**ystems are the minimal set of concepts that the framework needs to be useful, but additional ones have been added for convenience and performance. I will very briefly expose them here, but each of these will be described in much more details in the [wiki][wiki].

-   [World][wiki/world]: the collection of all existing **E**ntities and **C**omponents. It is the giant block of data that manages all the other blocks of data. The World is also the basis for extensibility because it holds the framework's modules and allows anyone to add one. This makes the framework extremely adaptable to any game development setup.
-   [Message][wiki/message]: a temporary **C**omponent that is not attached to an **E**ntity and that simplifies **S**ystem communication.
-   [Resource][wiki/resource]: a World-wide **C**omponent that is not attached to an **E**ntity that holds global data.
-   [Group][wiki/group]: a list of all the **E**ntities in a given World that conform to a query.
-   [Node][wiki/node]: a data wrapper around **S**ystems that allow to define execution behaviour and order.
-   [Controller][wiki/controller]: a wrapper around **S**ystems that executes them based on the behaviour defined in nodes and that controls their state.
-   [Phase][wiki/phase]: a data type that is associated with a phase of execution. It allows to run systems at different times such as initialization time, run time, dispose time or any other time that you care to define.

# Recurrent Usage Patterns
I will specify here recurrent usage patterns that are used in the framework.

### When in doubt, use a _struct_.
-   Almost everything you will implement when using the framework will be a _struct_. **E**ntities are _struct_, **C**omponents are _struct_, **S**ystems are _struct_ and other concepts such as Messages, Resources, Queryables and Injectables are all _structs_.
-   The framework will enforce the usage of _structs_.
-   This (almost abusive) usage of _structs_ is deliberate.
    -   It prevents any kind of OOP-inheritance-impulses that one could have since _structs_ cannot inherit from anything.
    -   _Structs_ correspond much more appropriately to plain and inert data.
    - _Structs_ don't require useless indirection and null checking when accessing members.
    -   The cost of passing (copying) _structs_ around is nullified by C#7's _ref returns_.

### Most concepts have an empty associated _interface._
-   **C**omponents must implement _IComponent_, **S**ystems must implement _ISystem_, Messages must implement _IMessage_ and so on.
-   These interfaces are all empty but they enforce users to be explicit about what the type is and how it should be used.
-   The framework will enforce that you use the correct _interface_ with the appropriate functionality.

### Most things are extensible.
-   Whether you want to add a new way to query **E**ntities or add you custom serialization module, there is a way to extend the framework to accommodate you.
-   The whole framework is implemented as extensions to the World which means that it is so extensible that you could, in principle, implement another **ECS** within **Entia**.
-   Most extensions use interfaces and/or attributes to allow efficient, flexible and AOT (ahead of time compilation) friendly extensions.
    -   For example, to implement a new kind of query, you would have to define an _struct_ that implements the IQueryable<T> (an empty _interface_) where T is an IQuerier and define a type that implements IQuerier (a non-empty _interface_ that builds queries).
    -   This _interface_-linking pattern (an _interface_ that has for main purpose to link a type 'T') is used a lot in the framework. It makes it explicit what the default implementation for concepts are and ensures that those linked types are properly AOT compiled even when using generic types.
    - AOT support is essential since some target platforms (such as iOS) require it.
-   Most existing implementations can be replaced with your own.
    -   If you don't like how the framework parallelizes your **S**ystems, you can replace the threading model by your own.
    -   Most modules are implemented as a map between a type and an _interface_ and expose a 'Set' method such that implementations can be replaced.
-   It is to be noted that extensions require a fair amount of knowledge of how the framework works to make them work properly. I have tried to make relatively small modules such that extending one doesn't require too much knowledge, but still consider this as an advanced feature.
