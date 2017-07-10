using System;
using System.Collections.Generic;

using Sandbox.ModAPI;
using VRage;
using VRage.Game.ModAPI;
using VRage.Library.Collections;


using SEPC.Components.Attributes;
using SEPC.Extensions;
using SEPC.Logging;
using SEPC.Threading;

namespace SEPC.Network.Messaging
{
    /// <summary>
    /// <para>Provides thread-safe, double-namespaced handler registration.</para>
    /// <para>Handlers receive messages and their senderId on the game thread.</para>
    /// <para>Handlers are automatically detatched on errors and session close.</para>
    /// <para>Dependent classes should be initialized AFTER and closed AFTER this component.</para>
    /// </summary>
    [IsSessionComponent]
    public class HandlerRegistrar
    {
        /// <summary>
        /// The signature expected of message handlers
        /// </summary>
        public delegate void MessageHandler(BitStream message, ulong senderId);

        #region Static

        /// <summary>Components containing handlers should be closed after this.</summary>
        public const int OnSessionCloseOrder = int.MinValue;

        /// <summary>The ID HandlerRegistrar uses to register with MyAPIGateway.Multiplayer</summary>
        public static readonly ushort HandlerId = (ushort)"SEPC".GetHashCode();

        static readonly Logable Log = new Logable("SEPC.Network.Messaging");
        static HandlerRegistrar Static;

        /// <summary>
        /// Register a Handler for messages with a given type and domain. 
        /// Throws an exception if already registered.
        /// </summary>
        public static void Register(ushort domainId, ushort typeId, MessageHandler handler)
        {
            Static?.AddHandler(domainId, typeId, handler);
        }

        /// <summary>
        /// Write headers to or read them from a given stream, depending on its status.
        /// </summary>
        public static void SerializeHeaders(ref BitStream stream, ref ushort domainId, ref ushort typeId, ref ulong senderId)
        {
            stream.Serialize(ref domainId);
            stream.Serialize(ref typeId);
            stream.Serialize(ref senderId);
        }

        #endregion
        #region Instance

        readonly Dictionary<ushort, Dictionary<ushort, HashSet<MessageHandler>>> Handlers = new Dictionary<ushort, Dictionary<ushort, HashSet<MessageHandler>>>();
        readonly FastResourceLock HandlersLock = new FastResourceLock();

        /// <summary>
        /// Initialized by ComponentSession
        /// </summary>
        public HandlerRegistrar()
        {
            Log.Entered();
            MyAPIGateway.Multiplayer.RegisterMessageHandler(HandlerId, HandleMessage);
            Static = this;
        }

        [OnSessionClose(order: OnSessionCloseOrder)]
        void Terminate()
        {
            Log.Entered();
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(HandlerId, HandleMessage);
            Static = null;
        }

        void AddHandler(ushort domainId, ushort typeId, MessageHandler handler)
        {
            Exceptions.ThrowIf<ArgumentNullException>(handler == null, "handler");
            Log.Trace($"Registering handler for message type {typeId} in domain {domainId}");

            using (HandlersLock.AcquireExclusiveUsing())
            {
                Dictionary<ushort, HashSet<MessageHandler>> handlersByType;
                if (!Handlers.TryGetValue(domainId, out handlersByType))
                    handlersByType = Handlers[domainId] = new Dictionary<ushort, HashSet<MessageHandler>>();

                HashSet<MessageHandler> handlersForType;
                if (!handlersByType.TryGetValue(typeId, out handlersForType))
                    handlersForType = handlersByType[typeId] = new HashSet<MessageHandler>();

                handlersForType.Add(handler);
            }
        }

        void HandleMessage(byte[] bytes)
        {
            try
            {
                Log.Trace("Received message of length " + bytes.Length);

                ushort domainId = 0;
                ushort typeId = 0;
                ulong senderId = 0;
                BitStream stream = bytes.ToBitStream();
                stream.ResetRead();
                SerializeHeaders(ref stream, ref domainId, ref typeId, ref senderId);
                
                using (HandlersLock.AcquireExclusiveUsing())
                {
                    Dictionary<ushort, HashSet<MessageHandler>> handlersByType;
                    if (!Handlers.TryGetValue(domainId, out handlersByType))
                        throw new Exception($"Received message for unhandled domain {domainId}.");

                    HashSet<MessageHandler> handlersForType;
                    if (!handlersByType.TryGetValue(typeId, out handlersForType))
                        throw new Exception($"Received message for unhandled type {typeId} in domain {domainId}.");

                    handlersForType.RemoveWhere(handler => !TryInvokeHandler(handler, stream, senderId));
                }
            }
            catch (Exception e)
            {
                Log.Error("Error handling message: " + e);
            }
        }

        bool TryInvokeHandler(MessageHandler handler, BitStream stream, ulong senderId)
        {
            try
            {
                handler(stream, senderId);
                return true;
            }
            catch (Exception e)
            {
                Log.Error("Error running handler: " + e);
                return false;
            }
        }

        #endregion
    }
}