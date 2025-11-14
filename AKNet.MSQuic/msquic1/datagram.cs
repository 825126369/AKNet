/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:47
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Net.Sockets;
using System.Threading;

namespace MSQuic1
{
    internal class QUIC_DATAGRAM
    {
        public QUIC_SEND_REQUEST SendQueue;
        public QUIC_SEND_REQUEST PrioritySendQueueTail;
        public QUIC_SEND_REQUEST SendQueueTail;
        public QUIC_SEND_REQUEST ApiQueue;
        public readonly object ApiQueueLock = new object();
        public int MaxSendLength;
        public bool SendEnabled;

        public QUIC_CONNECTION mConnection;
    }

    internal static partial class MSQuicFunc
    {
        static void QuicDatagramInitialize(QUIC_DATAGRAM Datagram, QUIC_CONNECTION Connection)
        {
            Datagram.mConnection = Connection;
            Datagram.SendEnabled = true;
            Datagram.MaxSendLength = ushort.MaxValue;
            Datagram.PrioritySendQueueTail = Datagram.SendQueueTail = Datagram.SendQueue = null;
            QuicDatagramValidate(Datagram);
        }

        static void QuicDatagramUninitialize(QUIC_DATAGRAM Datagram)
        {
            NetLog.Assert(Datagram.SendQueue == null);
            NetLog.Assert(Datagram.ApiQueue == null);
        }

        static void QuicDatagramValidate(QUIC_DATAGRAM Datagram)
        {
            QUIC_CONNECTION Connection = QuicDatagramGetConnection(Datagram);
            if (QuicConnIsClosed(Connection))
            {
                NetLog.Assert(Datagram.SendQueue == null);
                NetLog.Assert((Connection.Send.SendFlags & QUIC_CONN_SEND_FLAG_DATAGRAM) == 0);
            }
            else if ((Connection.Send.SendFlags & QUIC_CONN_SEND_FLAG_DATAGRAM) != 0)
            {
                NetLog.Assert(Datagram.SendQueue != null);
            }
            else if (Connection.State.PeerTransportParameterValid)
            {
                NetLog.Assert(Datagram.SendQueue == null);
            }

            if (!Datagram.SendEnabled)
            {
                NetLog.Assert(Datagram.MaxSendLength == 0);
            }
            else
            {
                QUIC_SEND_REQUEST SendRequest = Datagram.SendQueue;
                while (SendRequest != null)
                {
                    NetLog.Assert(SendRequest.TotalLength <= Datagram.MaxSendLength);
                    SendRequest = SendRequest.Next;
                }
            }
        }

        static int QuicDatagramQueueSend(QUIC_DATAGRAM Datagram, QUIC_SEND_REQUEST SendRequest)
        {
            int Status;
            bool QueueOper = true;
            bool IsPriority = SendRequest.Flags.HasFlag(QUIC_SEND_FLAGS.QUIC_SEND_FLAG_PRIORITY_WORK);
            QUIC_CONNECTION Connection = QuicDatagramGetConnection(Datagram);

            Monitor.Enter(Datagram.ApiQueueLock);
            if (!Datagram.SendEnabled)
            {
                Status = QUIC_STATUS_INVALID_STATE;
            }
            else
            {
                if (SendRequest.TotalLength > Datagram.MaxSendLength)
                {
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                }
                else
                {
                    QUIC_SEND_REQUEST ApiQueueTail = Datagram.ApiQueue;
                    while (ApiQueueTail != null)
                    {
                        ApiQueueTail = ApiQueueTail.Next;
                        QueueOper = false;
                    }

                    ApiQueueTail = SendRequest;
                    Status = QUIC_STATUS_SUCCESS;
                }
            }
            Monitor.Exit(Datagram.ApiQueueLock);

            if (QUIC_FAILED(Status))
            {
                Connection.Partition.SendRequestPool.CxPlatPoolFree(SendRequest);
                goto Exit;
            }

            Status = QUIC_STATUS_PENDING;
            if (QueueOper)
            {
                QUIC_OPERATION Oper = QuicOperationAlloc(Connection.Partition, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL);
                if (Oper == null)
                {
                    goto Exit;
                }

                Oper.API_CALL.Context.Type = QUIC_API_TYPE.QUIC_API_TYPE_DATAGRAM_SEND;
                if (IsPriority)
                {
                    QuicConnQueuePriorityOper(Connection, Oper);
                }
                else
                {
                    QuicConnQueueOper(Connection, Oper);
                }
            }
        Exit:
            return Status;
        }

        static void QuicDatagramSendShutdown(QUIC_DATAGRAM Datagram)
        {
            if (!Datagram.SendEnabled)
            {
                return;
            }

            QUIC_CONNECTION Connection = QuicDatagramGetConnection(Datagram);
            CxPlatDispatchLockAcquire(Datagram.ApiQueueLock);
            Datagram.SendEnabled = false;
            Datagram.MaxSendLength = 0;
            QUIC_SEND_REQUEST ApiQueue = Datagram.ApiQueue;
            Datagram.ApiQueue = null;
            CxPlatDispatchLockRelease(Datagram.ApiQueueLock);

            QuicSendClearSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_DATAGRAM);

            while (Datagram.SendQueue != null)
            {
                QUIC_SEND_REQUEST SendRequest = Datagram.SendQueue;
                Datagram.SendQueue = SendRequest.Next;
                QuicDatagramCancelSend(Connection, SendRequest);
            }
            Datagram.PrioritySendQueueTail = Datagram.SendQueueTail = Datagram.SendQueue = null;

            while (ApiQueue != null)
            {
                QUIC_SEND_REQUEST SendRequest = ApiQueue;
                ApiQueue = ApiQueue.Next;
                QuicDatagramCancelSend(Connection, SendRequest);
            }
            QuicDatagramValidate(Datagram);
        }

        static void QuicDatagramCancelSend(QUIC_CONNECTION Connection,QUIC_SEND_REQUEST SendRequest)
        {
            QuicDatagramIndicateSendStateChange(Connection, ref SendRequest.ClientContext, QUIC_DATAGRAM_SEND_STATE.QUIC_DATAGRAM_SEND_CANCELED);
            Connection.Partition.SendRequestPool.CxPlatPoolFree(SendRequest);
        }

        static void QuicDatagramIndicateSendStateChange(QUIC_CONNECTION Connection, ref object ClientContext, QUIC_DATAGRAM_SEND_STATE State)
        {
            QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
            Event.Type =  QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_DATAGRAM_SEND_STATE_CHANGED;
            Event.DATAGRAM_SEND_STATE_CHANGED.ClientContext = ClientContext;
            Event.DATAGRAM_SEND_STATE_CHANGED.State = State;

            QuicConnIndicateEvent(Connection, ref Event);
            ClientContext = Event.DATAGRAM_SEND_STATE_CHANGED.ClientContext;
        }

        static bool QuicDatagramWriteFrame(QUIC_DATAGRAM Datagram, QUIC_PACKET_BUILDER Builder)
        {
            QUIC_CONNECTION Connection = QuicDatagramGetConnection(Datagram);
            NetLog.Assert(Datagram.SendEnabled);
            bool Result = false;
            QuicDatagramValidate(Datagram);

            while (Datagram.SendQueue != null)
            {
                QUIC_SEND_REQUEST SendRequest = Datagram.SendQueue;

                if (Builder.Metadata.Flags.KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT && !SendRequest.Flags.HasFlag(QUIC_SEND_FLAGS.QUIC_SEND_FLAG_ALLOW_0_RTT))
                {
                    NetLog.Assert(false);
                    Result = false;
                    goto Exit;
                }

                NetLog.Assert(SendRequest.TotalLength <= Datagram.MaxSendLength);
                QUIC_SSBuffer mBuf = Builder.GetDatagramCanWriteSSBufer();
                bool HadRoomForDatagram = QuicDatagramFrameEncodeEx(
                        SendRequest.Buffers,
                        SendRequest.BufferCount,
                        SendRequest.TotalLength,
                        ref mBuf);
                Builder.SetDatagramOffset(mBuf);

                if (!HadRoomForDatagram)
                {
                    NetLog.Assert(Builder.Datagram.Length < Datagram.MaxSendLength || Builder.Metadata.FrameCount != 0 || Builder.PacketStart != 0);
                    Result = true;
                    goto Exit;
                }
                
                Datagram.SendQueue = SendRequest.Next;
                if (Datagram.PrioritySendQueueTail == SendRequest) //当优先级队列 消耗完后，把他和对头保持一样，恢复成默认
                {
                    Datagram.PrioritySendQueueTail = Datagram.SendQueue;
                }
                if (Datagram.SendQueueTail == SendRequest)
                {
                    Datagram.SendQueueTail = Datagram.SendQueue;
                }

                Builder.Metadata.Flags.IsAckEliciting = true;
                Builder.Metadata.Frames[Builder.Metadata.FrameCount].Type = QUIC_FRAME_TYPE.QUIC_FRAME_DATAGRAM;
                Builder.Metadata.Frames[Builder.Metadata.FrameCount].DATAGRAM.ClientContext = SendRequest.ClientContext;
                QuicDatagramCompleteSend(Connection, SendRequest, ref Builder.Metadata.Frames[Builder.Metadata.FrameCount].DATAGRAM.ClientContext);
                if (++Builder.Metadata.FrameCount == QUIC_MAX_FRAMES_PER_PACKET)
                {
                    Result = true;
                    goto Exit;
                }
            }

            if(Datagram.SendQueue == null)
            {
                Datagram.PrioritySendQueueTail = Datagram.SendQueueTail = Datagram.SendQueue = null;
            }

        Exit:
            if (Datagram.SendQueue == null)
            {
                Connection.Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_DATAGRAM;
            }

            QuicDatagramValidate(Datagram);
            return Result;
        }

        static void QuicDatagramCompleteSend(QUIC_CONNECTION Connection,QUIC_SEND_REQUEST SendRequest, ref object ClientContext)
        {
            ClientContext = SendRequest.ClientContext;
            QuicDatagramIndicateSendStateChange(Connection, ref ClientContext, QUIC_DATAGRAM_SEND_STATE.QUIC_DATAGRAM_SEND_SENT);
            Connection.Partition.SendRequestPool.CxPlatPoolFree(SendRequest);
        }

        static int QuicCalculateDatagramLength(AddressFamily Family, ushort Mtu, int CidLength)
        {
            return MaxUdpPayloadSizeForFamily(Family, Mtu) - QUIC_DATAGRAM_OVERHEAD(CidLength) - CXPLAT_ENCRYPTION_OVERHEAD;
        }
        
        static void QuicDatagramOnSendStateChanged(QUIC_DATAGRAM Datagram)
        {
            QUIC_CONNECTION Connection = QuicDatagramGetConnection(Datagram);
            bool SendEnabled = true;
            int NewMaxSendLength = ushort.MaxValue;
            if (Connection.State.PeerTransportParameterValid)
            {
                if (!BoolOk(Connection.PeerTransportParams.Flags & QUIC_TP_FLAG_MAX_DATAGRAM_FRAME_SIZE))
                {
                    SendEnabled = false;
                    NewMaxSendLength = 0;
                }
                else
                {
                    if (Connection.PeerTransportParams.MaxDatagramFrameSize < ushort.MaxValue)
                    {
                        NewMaxSendLength = Connection.PeerTransportParams.MaxDatagramFrameSize;
                    }
                }
            }

            if (SendEnabled)
            {
                int MtuMaxSendLength;
                if (!Connection.State.Started)
                {
                    MtuMaxSendLength = QuicCalculateDatagramLength(
                            AddressFamily.InterNetworkV6,
                            QUIC_DPLPMTUD_MIN_MTU,
                            QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH);
                }
                else
                {
                    QUIC_PATH Path = Connection.Paths[0];
                    MtuMaxSendLength = QuicCalculateDatagramLength(QuicAddrGetFamily(Path.Route.RemoteAddress), Path.Mtu, Path.DestCid.Data.Length);
                }
                if (NewMaxSendLength > MtuMaxSendLength)
                {
                    NewMaxSendLength = MtuMaxSendLength;
                }
            }

            if (SendEnabled == Datagram.SendEnabled)
            {
                if (!SendEnabled || NewMaxSendLength == Datagram.MaxSendLength)
                {
                    return;
                }
            }

            Datagram.MaxSendLength = NewMaxSendLength;

            if (Connection.State.ExternalOwner)
            {
                QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
                Event.Type =  QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_DATAGRAM_STATE_CHANGED;
                Event.DATAGRAM_STATE_CHANGED = new QUIC_CONNECTION_EVENT.DATAGRAM_STATE_CHANGED_DATA();
                Event.DATAGRAM_STATE_CHANGED.SendEnabled = SendEnabled;
                Event.DATAGRAM_STATE_CHANGED.MaxSendLength = NewMaxSendLength;
                QuicConnIndicateEvent(Connection, ref Event);
            }

            if (!SendEnabled)
            {
                QuicDatagramSendShutdown(Datagram);
            }
            else
            {
                if (!Datagram.SendEnabled)
                {
                    Datagram.SendEnabled = true; // This can happen for 0-RTT connections that didn't previously support Datagrams
                }
                QuicDatagramOnMaxSendLengthChanged(Datagram);
            }

            QuicDatagramValidate(Datagram);
        }

        static void QuicDatagramOnMaxSendLengthChanged(QUIC_DATAGRAM Datagram)
        {
            QUIC_CONNECTION Connection = QuicDatagramGetConnection(Datagram);
            QUIC_SEND_REQUEST SendQueue = Datagram.SendQueue;
            QUIC_SEND_REQUEST Last_SendQueue = null;
            while (SendQueue != null)
            {
                if (SendQueue.TotalLength > Datagram.MaxSendLength)
                {
                    QUIC_SEND_REQUEST SendRequest = SendQueue;
                    if (Datagram.PrioritySendQueueTail == SendRequest)
                    {
                        Datagram.PrioritySendQueueTail = Last_SendQueue;
                    }

                    if (Last_SendQueue == null)
                    {
                        Datagram.SendQueue = SendQueue.Next;
                    }
                    else
                    {
                        Last_SendQueue.Next = SendQueue.Next;
                    }
                    
                    SendQueue = SendRequest.Next;
                    QuicDatagramCancelSend(Connection, SendRequest);
                }
                else
                {
                    Last_SendQueue = SendQueue;
                    SendQueue = SendQueue.Next;
                }
            }
            Datagram.SendQueueTail = Last_SendQueue;
            if(Datagram.PrioritySendQueueTail == null)
            {
                Datagram.PrioritySendQueueTail = SendQueue;
            }

            if (Datagram.SendQueue != null)
            {
                QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_DATAGRAM);
            }
            else
            {
                QuicSendClearSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_DATAGRAM);
            }

            QuicDatagramValidate(Datagram);
        }

        static bool QuicDatagramProcessFrame(QUIC_DATAGRAM Datagram, QUIC_RX_PACKET Packet, QUIC_FRAME_TYPE FrameType, ref QUIC_SSBuffer Buffer)
        {
            QUIC_CONNECTION Connection = QuicDatagramGetConnection(Datagram);
            NetLog.Assert(Connection.Settings.DatagramReceiveEnabled);

            QUIC_DATAGRAM_EX Frame = new QUIC_DATAGRAM_EX();
            if (!QuicDatagramFrameDecode(FrameType, ref Buffer, ref Frame))
            {
                return false;
            }

            QUIC_BUFFER QuicBuffer = new QUIC_BUFFER()
            {
                Offset = 0,
                Length = Frame.Data.Length,
                Buffer = Frame.Data.Buffer,
            };

            QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
            Event.Type = QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_DATAGRAM_RECEIVED;
            Event.DATAGRAM_RECEIVED = new QUIC_CONNECTION_EVENT.DATAGRAM_RECEIVED_DATA();
            Event.DATAGRAM_RECEIVED.Buffer = QuicBuffer;
            if (Packet.EncryptedWith0Rtt)
            {
                Event.DATAGRAM_RECEIVED.Flags = QUIC_RECEIVE_FLAG_0_RTT;
            } 
            else
            {
                Event.DATAGRAM_RECEIVED.Flags = 0;
            }

            QuicConnIndicateEvent(Connection, ref Event);
            QuicPerfCounterAdd(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_APP_RECV_BYTES, QuicBuffer.Length);
            return true;
        }

        static void QuicDatagramSendFlush(QUIC_DATAGRAM Datagram)
        {
            CxPlatDispatchLockAcquire(Datagram.ApiQueueLock);
            QUIC_SEND_REQUEST ApiQueue = Datagram.ApiQueue;
            Datagram.ApiQueue = null;
            CxPlatDispatchLockRelease(Datagram.ApiQueueLock);
            long TotalBytesSent = 0;

            if (ApiQueue == null)
            {
                return;
            }

            QUIC_CONNECTION Connection = QuicDatagramGetConnection(Datagram);
            while (ApiQueue != null)
            {

                QUIC_SEND_REQUEST SendRequest = ApiQueue;
                ApiQueue = ApiQueue.Next;
                SendRequest.Next = null;

                NetLog.Assert(!SendRequest.Flags.HasFlag(QUIC_SEND_FLAGS.QUIC_SEND_FLAG_BUFFERED));
                NetLog.Assert(Datagram.SendEnabled);

                if (SendRequest.TotalLength > Datagram.MaxSendLength || QuicConnIsClosed(Connection))
                {
                    QuicDatagramCancelSend(Connection, SendRequest);
                    continue;
                }
                TotalBytesSent += SendRequest.TotalLength;

                if (SendRequest.Flags.HasFlag(QUIC_SEND_FLAGS.QUIC_SEND_FLAG_DGRAM_PRIORITY))
                {
                    //把这个请求加到优先级队列里
                    if (Datagram.PrioritySendQueueTail == null)
                    {
                        Datagram.SendQueue = Datagram.SendQueueTail = Datagram.PrioritySendQueueTail = SendRequest;
                    }
                    else
                    {
                        if (Datagram.SendQueueTail == Datagram.PrioritySendQueueTail)
                        {
                            Datagram.SendQueueTail = SendRequest;
                        }

                        var Ori = Datagram.PrioritySendQueueTail.Next;
                        Datagram.PrioritySendQueueTail.Next = SendRequest;
                        Datagram.PrioritySendQueueTail = SendRequest;
                        Datagram.PrioritySendQueueTail.Next = Ori;
                    }
                }
                else
                {
                    if (Datagram.SendQueueTail == null)
                    {
                        Datagram.SendQueue = Datagram.SendQueueTail = Datagram.PrioritySendQueueTail = SendRequest;
                    }
                    else
                    {
                        Datagram.SendQueueTail.Next = SendRequest;
                        Datagram.SendQueueTail = SendRequest;
                    }
                }
            }

            if (Connection.State.PeerTransportParameterValid && Datagram.SendQueue != null)
            {
                NetLog.Assert(Datagram.SendEnabled);
                QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_DATAGRAM);
            }

            QuicDatagramValidate(Datagram);
            QuicPerfCounterAdd(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_APP_SEND_BYTES, TotalBytesSent);
        }

    }
}
