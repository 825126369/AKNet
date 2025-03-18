using AKNet.Common;
using System.Collections.Generic;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_TIMER_WHEEL
    {
        public long NextExpirationTime;
        public long ConnectionCount;
        public QUIC_CONNECTION NextConnection;
        public readonly List<CXPLAT_LIST_ENTRY> Slots = new List<CXPLAT_LIST_ENTRY>();
    }

    internal static partial class MSQuicFunc
    {
        static void QuicTimerWheelUpdate(QUIC_TIMER_WHEEL TimerWheel)
        {
            TimerWheel.NextExpirationTime = long.MaxValue;
            TimerWheel.NextConnection = null;

            for (int i = 0; i < TimerWheel.Slots.Count; ++i)
            {
                if (!CxPlatListIsEmpty(TimerWheel.Slots[i]))
                {
                    QUIC_CONNECTION ConnectionEntry = CXPLAT_CONTAINING_RECORD(TimerWheel.Slots[i].Flink, QUIC_CONNECTION, TimerLink);
                    long EntryExpirationTime = ConnectionEntry.EarliestExpirationTime;
                    if (EntryExpirationTime < TimerWheel.NextExpirationTime)
                    {
                        TimerWheel.NextExpirationTime = EntryExpirationTime;
                        TimerWheel.NextConnection = ConnectionEntry;
                    }
                }
            }

            if (TimerWheel.NextConnection == null)
            {
                QuicTraceLogVerbose($"[time][{TimerWheel}] Next Expiration = null.");
            }
            else
            {
                QuicTraceLogVerbose($"[time][{TimerWheel}] Next Expiration = {TimerWheel.NextExpirationTime}, {TimerWheel.NextConnection}.");
            }
        }

        static void QuicTimerWheelUpdateConnection(QUIC_TIMER_WHEEL TimerWheel, QUIC_CONNECTION Connection)
        {
            long ExpirationTime = Connection.EarliestExpirationTime;

            if (Connection.TimerLink.Flink != null)
            {
                CxPlatListEntryRemove(Connection.TimerLink);

                if (ExpirationTime == long.MaxValue || Connection.State.ShutdownComplete)
                {
                    Connection.TimerLink.Flink = null;
                    if (Connection == TimerWheel.NextConnection)
                    {
                        QuicTimerWheelUpdate(TimerWheel);
                    }

                    QuicConnRelease(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_TIMER_WHEEL);
                    TimerWheel.ConnectionCount--;
                    return;
                }
            }
            else if (ExpirationTime != long.MaxValue && !Connection.State.ShutdownComplete)
            {
                TimerWheel.ConnectionCount++;
                QuicConnAddRef(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_TIMER_WHEEL);
            }
            else
            {
                return; // Ignore
            }

            NetLog.Assert(ExpirationTime != long.MaxValue);
            NetLog.Assert(!Connection.State.ShutdownComplete);
            NetLog.Assert(TimerWheel.Slots.Count != 0);
            uint SlotIndex = TIME_TO_SLOT_INDEX(TimerWheel, ExpirationTime);
            
            CXPLAT_LIST_ENTRY ListHead = &TimerWheel->Slots[SlotIndex];
            CXPLAT_LIST_ENTRY Entry = ListHead->Blink;

            while (Entry != ListHead)
            {
                QUIC_CONNECTION* ConnectionEntry =
                    CXPLAT_CONTAINING_RECORD(Entry, QUIC_CONNECTION, TimerLink);
                uint64_t EntryExpirationTime = ConnectionEntry->EarliestExpirationTime;

                if (ExpirationTime > EntryExpirationTime)
                {
                    break;
                }

                Entry = Entry->Blink;
            }

            //
            // Insert after the current entry.
            //
            CxPlatListInsertHead(Entry, &Connection->TimerLink);

            QuicTraceLogVerbose(
                TimerWheelUpdateConnection,
                "[time][%p] Updating Connection %p.",
                TimerWheel,
                Connection);

            //
            // Make sure the next expiration time/connection is still correct.
            //
            if (ExpirationTime < TimerWheel->NextExpirationTime)
            {
                TimerWheel->NextExpirationTime = ExpirationTime;
                TimerWheel->NextConnection = Connection;
                QuicTraceLogVerbose(
                    TimerWheelNextExpiration,
                    "[time][%p] Next Expiration = {%llu, %p}.",
                    TimerWheel,
                    ExpirationTime,
                    Connection);
            }
            else if (Connection == TimerWheel->NextConnection)
            {
                QuicTimerWheelUpdate(TimerWheel);
            }

            //
            // Resize the timer wheel if we have too many connections for the
            // current size.
            //
            if (TimerWheel->ConnectionCount >
                TimerWheel->SlotCount * QUIC_TIMER_WHEEL_MAX_LOAD_FACTOR)
            {
                QuicTimerWheelResize(TimerWheel);
            }
        }
    }
}
