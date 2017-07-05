using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game.ModAPI;

using SEPC.Components.Attributes;
using SEPC.Extensions;
using SEPC.Logging;
using SEPC.HUD;
using SEPC.Diagnostics;

namespace SEPC.Components.Descriptions
{
    /// <summary>
    /// Holds the details of an action that handles component events
    /// </summary>
    public struct ComponentEventAction
    {
        public readonly Action Action;
        public readonly Assembly Assembly;
        public readonly bool Debug;
        public readonly string EventName;
        public readonly uint Frequency;
        public readonly string MethodName;
        public readonly int Order;
        public readonly bool Profile;

        public ComponentEventAction(Action action, Assembly assembly, string eventName, uint frequency, string methodName, int order)
        {
            Action = action;
            Assembly = assembly;
            Debug = assembly.IsDebugDefined();
            EventName = eventName;
            Frequency = frequency;
            MethodName = methodName;
            Order = order;
            Profile = assembly.IsProfileDefined();
            //Logger.DebugLog($"ComponentEventAction() - assembly: {assembly.GetName().Name}, IsProfileDefined: {assembly.IsProfileDefined()}, Profile: {Profile} ");
        }

        public override string ToString()
        {
            return $"<ComponentEventHandler Debug: {Debug}, EventName: {EventName}, Frequency: {Frequency}, MethodName: {MethodName}, Order: {Order}. Profile: {Profile}>";
        }

        public bool TryInvoke()
        {
            //Logger.DebugLog($"TryInvoke() - Profile? {Profile}");
            bool success = false;
            if (Profile)
                Profiler.StartProfileBlockForAction(Action, Assembly);
            try
            {
                Action.Invoke();
                success = true;
            }
            catch (Exception error)
            {
                Logger.Log($"Error invoking {this}: {error}", Severity.Level.ERROR);
                if (Debug)
                    Notification.Notify($"Error running {MethodName} for {EventName}.", 10000, Severity.Level.ERROR);
            }
            if (Profile)
                Profiler.EndProfileBlock();
            return success;
        }
    }

    /// <summary>
    /// Holds the details of an instantiated component
    /// </summary>
    public class ComponentInstanceDescription
    {
        public Type ComponentClass;
        public object ComponentInstance; // can be null for static session components
        public List<ComponentEventAction> EventActions;

        public override string ToString()
        {
            return $"<ComponentInstanceDescription ComponentClass: {ComponentClass}, ComponentInstance: {ComponentInstance}, EventActions: {EventActions}>";
        }
    }

    /// <summary>
    /// Contains a set of each type of component
    /// </summary>
    public struct ComponentDescriptionCollection
    {
        public static ComponentDescriptionCollection FromAssembly(Assembly assembly)
        {
            return new ComponentDescriptionCollection(
                assembly.GetName().Name,
                EntityComponentDescription<IMyCubeBlock>.AllFromAssembly(assembly),
                EntityComponentDescription<IMyCharacter>.AllFromAssembly(assembly),
                EntityComponentDescription<IMyCubeGrid>.AllFromAssembly(assembly),
                SessionComponentDescription.AllFromAssembly(assembly)
            );
        }

        public readonly string AssemblyName;
        public readonly List<EntityComponentDescription<IMyCubeBlock>> BlockComponents;
        public readonly List<EntityComponentDescription<IMyCharacter>> CharacterComponents;
        public readonly List<EntityComponentDescription<IMyCubeGrid>> GridComponents;
        public readonly List<SessionComponentDescription> SessionComponents;

        public ComponentDescriptionCollection(
            string assemblyName,
            List<EntityComponentDescription<IMyCubeBlock>> blockComponents,
            List<EntityComponentDescription<IMyCharacter>> characterComponents,
            List<EntityComponentDescription<IMyCubeGrid>> gridComponents,
            List<SessionComponentDescription> sessionComponents
        )
        {
            AssemblyName = assemblyName;
            BlockComponents = blockComponents;
            CharacterComponents = characterComponents;
            GridComponents = gridComponents;
            SessionComponents = sessionComponents;
        }

        public ComponentDescriptionCollection SelectGroup(int groupdId)
        {
            return new ComponentDescriptionCollection(
                AssemblyName,
                BlockComponents.Where(x => x.GroupId == groupdId).ToList(),
                CharacterComponents.Where(x => x.GroupId == groupdId).ToList(),
                GridComponents.Where(x => x.GroupId == groupdId).ToList(),
                SessionComponents.Where(x => x.GroupId == groupdId).ToList()
            );
        }

        public override string ToString()
        {
            return $"<ComponentDescriptionCollection AssemblyName: { AssemblyName }, BlockComponents: {BlockComponents.Count}, CharacterComponents: {CharacterComponents.Count}, GridComponents: {GridComponents.Count}, SessionComponents: {SessionComponents.Count} >";
        }
    }

    /// <summary>
    /// Derives the details of a Component from its attributes
    /// </summary>
    public abstract class ComponentDescription
    {
        protected struct ComponentEventMethod
        {
            public readonly Assembly Assembly;
            public readonly HandlesComponentEvents Attr;
            //public readonly bool Debug;
            public readonly string EventName;
            public readonly uint Frequency;
            public readonly MethodInfo Method;
            public readonly int Order;
            //public readonly bool Profile;
            public readonly RunLocation RunsOn;

            public ComponentEventMethod(HandlesComponentEvents attr, MethodInfo method, Assembly assembly)
            {
                Assembly = assembly;
                Attr = attr;
                //Debug = debug;
                EventName = attr.EventName;
                Frequency = attr.Frequency;
                Method = method;
                Order = attr.Order;
                //Profile = profile;
                RunsOn = attr.RunsOn;
            }

            public int CompareTo(ComponentEventMethod other)
            {
                if (EventName != other.EventName)
                    return EventName.CompareTo(other.EventName);
                if (Order != other.Order)
                    return Order.CompareTo(other.Order);
                if (RunsOn != other.RunsOn)
                    return other.RunsOn.CompareTo(RunsOn); // descending 
                return Method.GetHashCode().CompareTo(other.GetHashCode());
            }

            public bool ShouldRunOn(RunLocation location)
            {
                return (RunsOn & location) != 0;
            }

            public ComponentEventAction ToEventAction(object instance = null)
            {
                var action = instance == null ? MethodToAction(Method) : InstanceMethodToAction(Method, instance);
                return new ComponentEventAction(action, Assembly, Attr.EventName, Attr.Frequency, Method.DeclaringType.FullName + "." + Method.Name, Attr.Order);
            }
        }

        #region Static Reflection helpers

        protected static IEnumerable<MethodInfo> GetMethodsInTypeWith<TAttribute>(Type type, bool inherit = false) where TAttribute : Attribute
        {
            return type.GetMethods().Where(x => x.IsDefined(typeof(TAttribute), inherit));
        }

        protected static Action MethodToAction(MethodInfo method)
        {
            return (Action)Delegate.CreateDelegate(typeof(Action), method);
        }

        protected static Func<TOut> MethodToFunc<TOut>(MethodInfo method)
        {
            return (Func<TOut>)Delegate.CreateDelegate(typeof(Func<TOut>), method);
        }

        protected static Func<TIn, TOut> MethodToFunc<TIn, TOut>(MethodInfo method)
        {
            // Logger.DebugLog($"Casting {method.DeclaringType}.{method.Name} to Func<{typeof(TIn)},{typeof(TOut)}>");
            return (Func<TIn, TOut>)Delegate.CreateDelegate(typeof(Func<TIn, TOut>), method);
        }

        protected static Action InstanceMethodToAction(MethodInfo method, object instance)
        {
            return (Action)Delegate.CreateDelegate(typeof(Action), instance, method);
        }

        #endregion

        public readonly Type ComponentClass;
        public readonly bool Debug;
        public readonly int GroupId;
        public readonly bool IsStatic;
        public readonly int Order;
        public readonly bool Profile;
        public readonly RunLocation RunsOn;

        protected List<ComponentEventMethod> EventMethods;

        public ComponentDescription(IsComponent attribute, Type componentClass, Assembly assembly)
        {
            ComponentClass = componentClass;
            Debug = assembly.IsDebugDefined();
            GroupId = attribute.GroupId;
            IsStatic = attribute.IsStatic;
            Order = attribute.Order;
            Profile = assembly.IsProfileDefined();
            RunsOn = attribute.RunsOn;
            EventMethods = (
                from method in componentClass.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | (IsStatic ? BindingFlags.Static : BindingFlags.Instance))
                from attr in method.GetCustomAttributes<HandlesComponentEvents>(false)
                select new ComponentEventMethod(attr, method, assembly)
            ).ToList();
            EventMethods.Sort((x, y) => x.CompareTo(y));
            //Logger.DebugLog("Constructed " + ToString());
        }

        public int CompareTo(ComponentDescription other)
        {
            if (GroupId != other.GroupId)
                return GroupId.CompareTo(other.GroupId);
            if (Order != other.Order)
                return Order.CompareTo(other.Order);
            if (RunsOn != other.RunsOn)
                return other.RunsOn.CompareTo(RunsOn); // descending 
            return ComponentClass.GetHashCode().CompareTo(other.ComponentClass.GetHashCode());
        }

        public bool ShouldRunOn(RunLocation location)
        {
            return (RunsOn & location) != 0;
        }

        public override string ToString()
        {
            return $"<ComponentDescription ComponentClass: {ComponentClass}, Order: {Order}, GroupId: {GroupId}, IsStatic: {IsStatic}, RunsOn: {RunsOn}, EventMethods: {EventMethods.Count()} >";
        }

        /// <summary>
        /// Returns a new component instance if it should be created, else null.
        /// </summary>
        public bool TryCreateInstance(RunLocation runningOn, object attachedTo, out ComponentInstanceDescription result)
        {
            result = null;
            if (!ShouldRunOn(runningOn)) return false;

            try { if (!InvokeConditionFunc(attachedTo)) return false; }
            catch (Exception e)
            {
                Logger.Log($"Error invoking condition func from {this}: {e}", Severity.Level.ERROR);
                if (Debug)
                    Notification.Notify($"Error invoking condition func for {ComponentClass.FullName}.", 10000, Severity.Level.ERROR);
                return false;
            }

            object instance;
            List<ComponentEventAction> actions;
            try
            {
                instance = CreateInstance(attachedTo);
                actions = EventMethods.Where(x => x.ShouldRunOn(runningOn)).Select(x => x.ToEventAction(instance)).ToList();
            }
            catch (Exception e)
            {
                Logger.Log($"Error instantiating component {this}: {e}", Severity.Level.ERROR);
                if (Debug)
                    Notification.Notify($"Error instantiating {ComponentClass.FullName}.", 10000, Severity.Level.ERROR);
                return false;
            }

            result = new ComponentInstanceDescription()
            {
                ComponentClass = ComponentClass,
                ComponentInstance = instance,
                EventActions = actions,
            };
            return true;
        }
        protected abstract object CreateInstance(object attachedTo);
        protected abstract bool InvokeConditionFunc(object attachedTo);
    }

    /// <summary>
    /// Contains the registration details of a EntityComponent defined via its attributes.
    /// </summary>
    /// <typeparam name="TEntity">Type of the instantiated entity to attach to.</typeparam>
    public class EntityComponentDescription<TEntity> : ComponentDescription where TEntity : IMyEntity
    {
        public static List<EntityComponentDescription<TEntity>> AllFromAssembly(Assembly assembly)
        { 
            var result = (
                from type in assembly.GetTypes()
                from attr in type.GetCustomAttributes<IsEntityComponent>(false)
                where attr.EntityType == typeof(TEntity)
                select new EntityComponentDescription<TEntity>(attr, type, assembly)
            ).ToList();
            result.Sort((x, y) => x.CompareTo(y));
            return result;
        }

        protected readonly List<MyObjectBuilderType> BuilderTypes;
        protected readonly Func<TEntity, bool> ConditionFunc;

        public EntityComponentDescription(IsEntityComponent attr, Type componentClass, Assembly assembly) : base(attr, componentClass, assembly)
        {
            BuilderTypes = attr.BuilderTypes.Select(x => (MyObjectBuilderType)x).ToList();
            BuilderTypes.Sort((x, y) => x.GetHashCode().CompareTo(y.GetHashCode()));
            ConditionFunc = GetMethodsInTypeWith<EntityComponentIf>(componentClass)
                .Where(method => method.IsStatic)
                .Select(method => MethodToFunc<TEntity, bool>(method))
                .DefaultIfEmpty(x => true)
                .First();
            EventMethods = EventMethods.Where(x => x.Attr is HandlesEntityEvents).ToList();
        }

        protected override object CreateInstance(object attachedTo)
        {
            return Activator.CreateInstance(ComponentClass, (TEntity)attachedTo);
        }

        protected override bool InvokeConditionFunc(object attachedTo)
        {
            if (!(attachedTo is TEntity))
                return false;

            var asBlock = attachedTo as IMyCubeBlock;
            if (asBlock != null && BuilderTypes != null)
                if (!BuilderTypes.Any(x => x == asBlock.BlockDefinition.TypeId))
                    return false;

            return ConditionFunc.Invoke((TEntity)attachedTo);
        }
    }

    /// <summary>
    /// Contains the registration details of a SessionComponent defined via its attributes.
    /// </summary>
    public class SessionComponentDescription : ComponentDescription
    {

        public static List<SessionComponentDescription> AllFromAssembly(Assembly assembly)
        {
            var result = (
                from type in assembly.GetTypes()
                from attr in type.GetCustomAttributes<IsSessionComponent>(false)
                select new SessionComponentDescription(attr, type, assembly)
            ).ToList();
            result.Sort((x, y) => x.CompareTo(y));
            return result;
        }

        protected readonly Func<bool> ConditionFunc;

        public SessionComponentDescription(IsSessionComponent attr, Type componentClass, Assembly assembly) : base(attr, componentClass, assembly)
        {
            ConditionFunc = GetMethodsInTypeWith<SessionComponentIf>(componentClass)
                .Where(x => x.IsStatic)
                .Select(x => MethodToFunc<bool>(x))
                .DefaultIfEmpty(() => true)
                .First();
            EventMethods = EventMethods.Where(x => x.Attr is HandlesSessionEvents).ToList();
        }

        protected override object CreateInstance(object attachedTo)
        {
            return IsStatic ? null : Activator.CreateInstance(ComponentClass);
        }

        protected override bool InvokeConditionFunc(object attachedTo)
        {
            return ConditionFunc.Invoke();
        }
    }
}
