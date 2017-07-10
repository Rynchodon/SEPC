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
    /// Provide thread-safe methods to send messages to registered handlers.
    /// </summary>
    public static class Messenger
    {
        /// <summary>
        /// The error throw when a message is too long
        /// </summary>
        public class MessageTooLongException : Exception
        {
            /// <summary>
            /// Constructs the error given the message length
            /// </summary>
            public MessageTooLongException(int length) : base("Length: " + length) { }
        }

        enum MessageDestination
        {
            None, Faction, Player, Server, All
        }

        static readonly Logable Log = new Logable("SEPC.Network.Messaging");

        /// <summary>
        /// Sends a message to all clients that can be handled by a registered handler. Thread-safe.
        /// </summary>
        public static void SendToAll(BitStream data, ushort domainId, ushort typeId, bool reliable = true)
        {
            Send(data, domainId, typeId, MessageDestination.All, 0, reliable);
        }

        /// <summary>
        /// Sends a message to a player that can be handled by a registered handler. Thread-safe.
        /// </summary>
        public static void SendToPlayer(BitStream data, ushort domainId, ushort typeId, ulong steamId, bool reliable = true)
        {
            Send(data, domainId, typeId, MessageDestination.Player, steamId, reliable);
        }

        /// <summary>
        /// Sends a message to the server that can be handled by a registered handler. Thread-safe.
        /// </summary>
        public static void SendToServer(BitStream data, ushort domainId, ushort typeId, bool reliable = true)
        {
            Send(data, domainId, typeId, MessageDestination.Server, 0, reliable);
        }

        /*
        public void SendToFaction(BitStream data, ushort domainId, ushort typeId, long factionId, bool reliable = true)
        {
            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetFactionById(factionId);
            if (faction == null)
                Log.Error("Failed to find faction " + factionId);
            else
                foreach (ulong steamId in faction.SteamIds())
                    SendToPlayer(data, domainId, typeId, steamId, reliable);
        }
        */

        static void Send(BitStream data, ushort domainId, ushort typeId, MessageDestination dest, ulong destId = 0, bool reliable = true)
        {
            Log.Trace($"Sending {data.ByteLength} bytes to {dest}/{destId}/{domainId}/{typeId}");
            ulong senderId = (MyAPIGateway.Session.Player != null) ? MyAPIGateway.Session.Player.SteamUserId : 0;
            byte[] bytes = FormatMessage(data, domainId, typeId, senderId);

            MainThread.TryOnMainThread(() =>
            {
                if (!SendMessageToDest(bytes, dest, destId, reliable))
                    Log.Error(new MessageTooLongException(bytes.Length));
            });
        }

        static byte[] FormatMessage(BitStream payload, ushort domainId, ushort typeId, ulong senderId)
        {
            Log.Entered();
            BitStream stream = new BitStream();
            stream.ResetWrite();
            HandlerRegistrar.SerializeHeaders(ref stream, ref domainId, ref typeId, ref senderId);
            Log.Trace($"Adding payload of length {payload.ByteLength} to formatted message of length {stream.ByteLength}");
            payload.ResetRead();
            stream.WriteBitStream(payload);
            Log.Trace($"Formatted message final length {stream.ByteLength}");
            return stream.ToBytes();
        }

        static bool SendMessageToDest(byte[] data, MessageDestination dest, ulong destId, bool reliable)
        {
            if (dest == MessageDestination.Server)
                return MyAPIGateway.Multiplayer.SendMessageToServer(HandlerRegistrar.HandlerId, data, reliable);
            else if (dest == MessageDestination.Player)
                return MyAPIGateway.Multiplayer.SendMessageTo(HandlerRegistrar.HandlerId, data, destId, reliable);
            else if (dest == MessageDestination.All)
                return MyAPIGateway.Multiplayer.SendMessageToOthers(HandlerRegistrar.HandlerId, data, reliable);
            else
                return true;
        }
    }

}