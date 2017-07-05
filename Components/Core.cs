using System;

namespace SEPC.Components
{
    [Flags]
    public enum RunLocation
    {
        None = 0,
        Client = 1,
        Server = 2,
        Both = Client | Server
    }

    /// <summary>
    /// A set of basic component event names.
    /// Can be easily extended by other Assemblies, because component event names are simple strings.
    /// By introducing custom event names and handlers, consumers can rely on event-driven architecture.
    /// </summary>
    public static class ComponentEventNames
    {
        public const string BlockGridChange = "BlockGridChange";
        public const string EntityClose = "EntityClose";
        public const string SessionClose = "SessionClose";
        public const string SessionSave = "SessionSave";
        public const string StaticSessionComponentInit = "StaticSessionComponentInit"; // static session components have no ctr
        public const string Update = "Update";
        public const string UpdatingStopped = "UpdatingStopped";
        public const string UpdatingResumed = "UpdatingResumed";
    }
}
