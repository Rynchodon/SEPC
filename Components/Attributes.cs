using System;

namespace SEPC.Components.Attributes
{
    #region Component Abstract Attributes

    /// <summary>
    /// Describes a class that contains event and update handlers.
    /// Implemented by IsEntityComponent and IsSessionComponent.
    /// </summary>
    public abstract class IsComponent : Attribute
    {
        public readonly int GroupId; // Optional id for a group of components that are loaded together
        public readonly int Order; // Optional order of loading within its group, sorted ascending
        public readonly bool IsStatic; // For SessionComponents
        public readonly RunLocation RunsOn;

        public IsComponent(bool isStatic, RunLocation runsOn, int order, int groupId)
        {
            GroupId = groupId;
            IsStatic = isStatic;
            Order = order;
            RunsOn = runsOn;
        }
    }

    /// <summary>
    /// Describes a method that handles component events.
    /// Implemented by all of the update and event handler tags.
    /// </summary>
    public abstract class HandlesComponentEvents : Attribute
    {
        public readonly uint Frequency; // For Update Events
        public readonly int Order; // For Session Events
        public readonly RunLocation RunsOn;
        public readonly string EventName;

        public HandlesComponentEvents(string eventName, RunLocation runsOn, int order, uint frequency)
        {
            Frequency = frequency;
            Order = order;
            RunsOn = runsOn;
            EventName = eventName;
        }
    }

    /// <summary>
    /// Describes a method that handles Entity component events, specifically.
    /// Helps differentiate between session and event handlers in classes that attach both.
    /// </summary>
    public abstract class HandlesEntityEvents : HandlesComponentEvents
    {
        public HandlesEntityEvents(string eventName, RunLocation runsOn, uint frequency = 1) : base(eventName, runsOn, 0, frequency) { }
    }

    /// <summary>
    /// Describes a method that handles Session component events, specifically.
    /// Helps differentiate between session and event handlers in classes that attach both.
    /// </summary>
    public abstract class HandlesSessionEvents : HandlesComponentEvents
    {
        public HandlesSessionEvents(string eventName, RunLocation runsOn, int order, uint frequency = 1) : base(eventName, runsOn, order, frequency) { }
    }

    #endregion
    #region SessionComponent Attributes

    /// <summary>
    /// Identifies a class that provides handlers for session updates and events.
    /// If isStatic is false, should provide a parameterless constructor and instance methods for handlers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class IsSessionComponent : IsComponent
    {
        /// <param name="runsOn">Where this should be loaded</param>
        /// <param name="isStatic">Whether static or instance methods are used as handlers</param>
        /// <param name="order">The order of registration within its component group, sorted ascending</param>
        /// <param name="groupId">An identifier for the group of components with which this should be loaded</param>
        public IsSessionComponent(RunLocation runsOn = RunLocation.Both, bool isStatic = false, int order = 0, int groupId = 0) : base(isStatic, runsOn, order, groupId) { }
    }

    /// <summary>
    /// Identifies a method that runs when the session is closed. Should be instance/static depending on if the component IsStatic.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OnSessionClose : HandlesSessionEvents
    {
        /// <param name="order">The GLOBAL order in which this handler should be called among all other handlers for this event</param>
        /// <param name="runsOn">Where this should run</param>
        public OnSessionClose(RunLocation runsOn = RunLocation.Both, int order = 0) : base(ComponentEventNames.SessionClose, runsOn, order) { }
    }

    /// <summary>
    /// Identifies a method that handles custom session events. Should be instance/static depending on if the component IsStatic.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OnSessionEvent : HandlesSessionEvents
    {
        /// <param name="eventName">The name of the event to handle</param>
        /// <param name="order">The GLOBAL order in which this handler should be called among all other handlers for this event</param>
        /// <param name="runsOn">Where this should run</param>
        public OnSessionEvent(string eventName, RunLocation runsOn = RunLocation.Both, int order = 0) : base(eventName, runsOn, order) { }
    }

    /// <summary>
    /// Identifies a method that runs when the session is saved. Should be instance/static depending on if the component IsStatic.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OnSessionSave : HandlesSessionEvents
    {
        /// <param name="order">The GLOBAL order in which this handler should be called among all other handlers for this event</param>
        /// <param name="runsOn">Where this should run</param>
        public OnSessionSave(RunLocation runsOn = RunLocation.Both, int order = 0) : base(ComponentEventNames.SessionSave, runsOn, order) { }
    }

    /// <summary>
    /// Identifies a method that handles session updates. Should be instance/static depending on if the component IsStatic.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OnSessionUpdate : HandlesSessionEvents
    {
        /// <param name="frequency">The number of frames to wait between calls</param>
        /// <param name="runsOn">Where this should run</param>
        public OnSessionUpdate(uint frequency = 1, RunLocation runsOn = RunLocation.Both) : base(ComponentEventNames.Update, runsOn, 0, frequency) { }
    }

    /// <summary>
    /// Identifies a static method that runs when its static session component is loaded.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OnStaticSessionComponentInit : HandlesSessionEvents
    {
        /// <param name="runsOn">Where this should run</param>
        public OnStaticSessionComponentInit(RunLocation runsOn = RunLocation.Both) : base(ComponentEventNames.StaticSessionComponentInit, runsOn, 0) { }
    }

    /// <summary>
    /// Identifies a parameterless static method that decides if the component should be created (if not static) and loaded.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SessionComponentIf : Attribute { }

    #endregion
    #region EntityComponent Attributes

    /// <summary>
    /// Identifies a class that provides a constructor taking an entity of type EntityType and instance handlers for updates and entity events.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class IsEntityComponent : IsComponent
    {
        public readonly Type EntityType;
        public readonly Type[] BuilderTypes;

        /// <param name="entityType">The type of entity to attach to and receive in the constructor and condition method</param>
        /// <param name="builderTypes">Specific object builders to accept for the entity. Defaults to all. Should be provided for IMyCubeBlock components.</param>
        /// <param name="runsOn">Where this should run</param>
        /// <param name="order">The order of registration within its component group, sorted ascending</param>
        /// <param name="groupId">An identifier for the group of components with which this should be loaded</param>
        public IsEntityComponent(Type entityType, Type[] builderTypes, RunLocation runsOn = RunLocation.Both, int order = 0, int groupId = 0) : base(false, runsOn, order, groupId)
        {
            BuilderTypes = builderTypes;
            EntityType = entityType;
        }

        /// <param name="entityType">The type of entity to attach to and receive in the constructor and condition method</param>
        /// <param name="builderTypes">Specific object builder to accept for the entity. Defaults to all. Should be provided for IMyCubeBlock components.</param>
        /// <param name="runsOn">Where this should run</param>
        /// <param name="order">The order of registration within its component group, sorted ascending</param>
        /// <param name="groupId">An identifier for the group of components with which this should be loaded</param>
        public IsEntityComponent(Type entityType, Type builderType = null, RunLocation runsOn = RunLocation.Both, int order = 0, int groupId = 0) : base(false, runsOn, order, groupId)
        {
            BuilderTypes = builderType == null ? new Type[] { } : new Type[] { builderType };
            EntityType = entityType;
        }
    }

    /// <summary>
    /// Identifies an instance method that should run when the entity closes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OnEntityClose : HandlesEntityEvents
    {
        /// <param name="runsOn">Where this should run. Restricted first by Component's RunLocation.</param>
        public OnEntityClose(RunLocation runsOn = RunLocation.Both) : base(ComponentEventNames.EntityClose, runsOn) { }
    }

    /// <summary>
    /// Identifies an instance method that handles custom entity events.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OnEntityEvent : HandlesEntityEvents
    {
        /// <param name="eventName">The name of the event to handle</param>
        /// <param name="runsOn">Where this should run</param>
        public OnEntityEvent(string eventName, RunLocation runsOn = RunLocation.Both) : base(eventName, runsOn) { }
    }

    /// <summary>
    /// Identifies an instance method that handles updates for entities.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OnEntityUpdate : HandlesEntityEvents
    {
        /// <param name="frequency">The number of frames to wait between calls</param>
        /// <param name="runsOn">Where this should run. Restricted first by Component's RunLocation.</param>
        public OnEntityUpdate(uint frequency, RunLocation runsOn = RunLocation.Both) : base(ComponentEventNames.Update, runsOn, frequency) { }
    }

    /// <summary>
    /// Identifies a static method of an EntityComponent that takes an instance of its EntityType and decides if an instance of the component should be created and loaded.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class EntityComponentIf : Attribute { }

    #endregion
}
