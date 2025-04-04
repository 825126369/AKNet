using AKNet.Common;
using AKNet.Udp4LinuxTcp.Common;
using System;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_TIMER_WHEEL
    {
        public long NextExpirationTime;
        public long ConnectionCount;
        public QUIC_CONNECTION NextConnection;

        public int SlotCount;
        public CXPLAT_LIST_ENTRY<QUIC_CONNECTION>[] Slots = null;
    }

    internal static partial class MSQuicFunc
    {
        static int TIME_TO_SLOT_INDEX(QUIC_TIMER_WHEEL TimerWheel, long TimeUs)
        {
            return (int)(TimeUs / 1000) % TimerWheel.SlotCount;
        }

        static ulong QuicTimerWheelInitialize(QUIC_TIMER_WHEEL TimerWheel)
        {
            TimerWheel.NextExpirationTime = long.MaxValue;
            TimerWheel.ConnectionCount = 0;
            TimerWheel.NextConnection = null;

            TimerWheel.Slots = new CXPLAT_LIST_ENTRY<QUIC_CONNECTION>[QUIC_TIMER_WHEEL_INITIAL_SLOT_COUNT];
            for (int i = 0; i < QUIC_TIMER_WHEEL_INITIAL_SLOT_COUNT; ++i)
            {
                CXPLAT_LIST_ENTRY<QUIC_CONNECTION> mEntry = new CXPLAT_LIST_ENTRY<QUIC_CONNECTION>(null);
                CxPlatListInitializeHead(mEntry);
                TimerWheel.Slots[i]= mEntry;
            }
            return QUIC_STATUS_SUCCESS;
        }

        static void QuicTimerWheelUpdate(QUIC_TIMER_WHEEL TimerWheel)
        {
            TimerWheel.NextExpirationTime = long.MaxValue;
            TimerWheel.NextConnection = null;

            for (int i = 0; i < TimerWheel.SlotCount; ++i)
            {
                if (!CxPlatListIsEmpty(TimerWheel.Slots[i]))
                {
                    QUIC_CONNECTION ConnectionEntry = CXPLAT_CONTAINING_RECORD<QUIC_CONNECTION>(TimerWheel.Slots[i].Flink);
                    long EntryExpirationTime = ConnectionEntry.EarliestExpirationTime;
                    if (EntryExpirationTime < TimerWheel.NextExpirationTime)
                    {
                        TimerWheel.NextExpirationTime = EntryExpirationTime;
                        TimerWheel.NextConnection = ConnectionEntry;
                    }
                }
            }
        }

        static void QuicTimerWheelResize(QUIC_TIMER_WHEEL TimerWheel)
        {
            int NewSlotCount = TimerWheel.SlotCount * 2;
            if (NewSlotCount <= TimerWheel.SlotCount)
            {
                //大于 int.Max 了
                return;
            }

            CXPLAT_LIST_ENTRY<QUIC_CONNECTION>[] NewSlots = new CXPLAT_LIST_ENTRY<QUIC_CONNECTION>[NewSlotCount];
            if(NewSlots == null)
            {
                return;
            }

            for (int i = 0; i < NewSlotCount; ++i)
            {
                NewSlots[i] = new CXPLAT_LIST_ENTRY<QUIC_CONNECTION>(null);
                CxPlatListInitializeHead(NewSlots[i]);
            }

            int OldSlotCount = TimerWheel.SlotCount;
            var OldSlots = TimerWheel.Slots;
            TimerWheel.SlotCount = NewSlotCount;
            TimerWheel.Slots = NewSlots;
            for (int i = 0; i < OldSlotCount; ++i)
            {
                while (!CxPlatListIsEmpty(OldSlots[i]))
                {
                    QUIC_CONNECTION Connection = CXPLAT_CONTAINING_RECORD<QUIC_CONNECTION>(CxPlatListRemoveHead(OldSlots[i]));
                    long ExpirationTime = Connection.EarliestExpirationTime;
                    NetLog.Assert(TimerWheel.SlotCount != 0);
                    int SlotIndex = TIME_TO_SLOT_INDEX(TimerWheel, ExpirationTime);
                    CXPLAT_LIST_ENTRY ListHead = TimerWheel.Slots[SlotIndex];
                    CXPLAT_LIST_ENTRY Entry = ListHead.Blink;

                    while (Entry != ListHead)
                    {
                        QUIC_CONNECTION ConnectionEntry = CXPLAT_CONTAINING_RECORD<QUIC_CONNECTION>(Entry);
                        long EntryExpirationTime = ConnectionEntry.EarliestExpirationTime;
                        if (ExpirationTime > EntryExpirationTime)
                        {
                            break;
                        }
                        Entry = Entry.Blink;
                    }
                    CxPlatListInsertHead(Entry, Connection.TimerLink);
                }
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
            NetLog.Assert(TimerWheel.SlotCount != 0);
            int SlotIndex = TIME_TO_SLOT_INDEX(TimerWheel, ExpirationTime);
            
            CXPLAT_LIST_ENTRY ListHead = TimerWheel.Slots[SlotIndex];
            CXPLAT_LIST_ENTRY Entry = ListHead.Blink;

            while (Entry != ListHead)
            {
                QUIC_CONNECTION ConnectionEntry = CXPLAT_CONTAINING_RECORD<QUIC_CONNECTION>(Entry);
                long EntryExpirationTime = ConnectionEntry.EarliestExpirationTime;
                if (ExpirationTime > EntryExpirationTime)
                {
                    break;
                }

                Entry = Entry.Blink;
            }
                
            CxPlatListInsertHead(Entry, Connection.TimerLink);
            
            if (ExpirationTime < TimerWheel.NextExpirationTime)
            {
                TimerWheel.NextExpirationTime = ExpirationTime;
                TimerWheel.NextConnection = Connection;
            }
            else if (Connection == TimerWheel.NextConnection)
            {
                QuicTimerWheelUpdate(TimerWheel);
            }
            
            if (TimerWheel.ConnectionCount > TimerWheel.SlotCount * QUIC_TIMER_WHEEL_MAX_LOAD_FACTOR)
            {
                QuicTimerWheelResize(TimerWheel);
            }
        }

        static void QuicTimerWheelGetExpired(QUIC_TIMER_WHEEL TimerWheel, long TimeNow, CXPLAT_LIST_ENTRY OutputListHead)
        {
            bool NeedsUpdate = false;
            for (int i = 0; i < TimerWheel.SlotCount; ++i)
            {
                CXPLAT_LIST_ENTRY ListHead = TimerWheel.Slots[i];
                CXPLAT_LIST_ENTRY Entry = ListHead.Flink;
                while (Entry != ListHead)
                {
                    QUIC_CONNECTION ConnectionEntry = CXPLAT_CONTAINING_RECORD<QUIC_CONNECTION>(Entry);
                    long EntryExpirationTime = ConnectionEntry.EarliestExpirationTime;
                    if (EntryExpirationTime > TimeNow)
                    {
                        break;
                    }
                    Entry = Entry.Flink;
                    CxPlatListEntryRemove(ConnectionEntry.TimerLink);
                    CxPlatListInsertTail(OutputListHead, ConnectionEntry.TimerLink);
                    if (ConnectionEntry == TimerWheel.NextConnection)
                    {
                        NeedsUpdate = true;
                    }

                    QuicConnAddRef(ConnectionEntry, QUIC_CONNECTION_REF.QUIC_CONN_REF_WORKER);
                    QuicConnRelease(ConnectionEntry, QUIC_CONNECTION_REF.QUIC_CONN_REF_TIMER_WHEEL);
                    TimerWheel.ConnectionCount--;
                }
            }

            if (NeedsUpdate)
            {
                QuicTimerWheelUpdate(TimerWheel);
            }
        }
    }
}
