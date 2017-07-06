# SEPC Components

Space Engineers provides `MySessionComponent` and `MyEntityComponent` classes 
that run alongside the Session or an Entity.

SEPC improves on these with its own `SessionComponent` and `EntityComponent`
concepts. 

## Advantages over SE's components

Unlike their SE counterparts, SEPC's components:

* are instantiated after the session is fully initialized
* are reliably closed before game assets become unavailable
* receive updates as long as the attached session/entity is available
* can be attached to Characters 
* can be conditional on run location as well as an arbitrary function
* can be placed in groups that are loaded at specific times
* can be loaded in a specific order within their group
* don't need to inherit from any particular class

and their handlers:
* are discarded if they throw an exception
* can be attached to custom events
* can be attached to a subset of the component's run location
* can be profiled and debugged dependant on symbols from their assembly
* can be ordered (session events only)
* can be set to any frequency (updates only)

## Usage

### Attributes
Like their SE counterparts, SEPC's components must be tagged with a special
attribute to be discovered:

```c++
using SEPC.Components;
using SEPC.Components.Attributes;

/// <summary>
/// This class will only be loaded on the Client, as the 7th element of group 4.
/// </summary>
[IsSessionComponent(runsOn: RunLocation.Client, groupId: 4, order: 7)]
public class SomeSessionComponent 
{ 
  ... 
}

/// <summary>
/// Once group 1 is loaded, this will be instantiated for every block with an OxygenFarm or SolarPanel builder.
/// </summary>
[IsEntityComponent(typeof(IMyCubeBlock), new [] { typeof(MyObjectBuilder_OxygenFarm), typeof(MyObjectBuilder_SolarPanel) }, groupId: 1)]
public class SomeBlockComponent 
{ 
  ... 
}
```

SEPC doesn't require component classes to inherit from any particular class,
so event and update handlers are also discovered via attributes:

```c++
using SEPC.Components;
using SEPC.Components.Attributes;


[IsEntityComponent(typeof(IMyCharacter))]
public class SomeCharacterComponent 
{ 
	/// <summary>
	/// This method will be called every 100 frames, only on the server
	/// </summary>
	[OnEntityUpdate(100, RunLocation.Server)]
	public void Update100()
	{
		...
	} 
}
```

Attributes receive the full configuration of their component or handler as args.

For a full explanation of available attributes and their arguments,
see the comments in `SEPC.Components.Attributes`.

### Registration
Once your component classes have been appropriately tagged, you must inform
the `ComponentRegistrar` of your Assembly's availability and configuration. 
This needs to be done once, before a session is loaded. 
The `Init()` method of a plugin is a good option:

```c++
using SEPC.Components;
using System;
using VRage.Plugins;

/// <summary>
/// Loaded with the game and persists until game is closed.
/// Registers our Components.
/// </summary>
public class Plugin : IPlugin
{
	public void Dispose() { }

	public void Init(object gameInstance)
	{
		try
		{
			// Tell the Registrar if we have DEBUG defined
			ComponentRegistrar.DebugConditional();

			// Tell the Registrar if we have PROFILE defined
			ComponentRegistrar.ProfileConditional();

			// Tell the Registrar about all our components
			ComponentRegistrar.AddComponents();

			// Tell the Registrar to load group 0 automatically
			ComponentRegistrar.LoadOnInit(0);
		}
		catch (Exception error)
		{
			Logger.DebugLog($"Error registering components: {error}");
		}
	}

	public void Update() { }
}
```

### Delayed Initialization
Components that cannot be loaded as soon as the session is ready can be 
placed in other groups and manually loaded into the `ComponentSession` later.

A common pattern is to define a single "loader" SessionComponent that 


