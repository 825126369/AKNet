using AKNet.Common;
using Google.Protobuf;
namespace NetProtocols.SelectGate
{
	public sealed partial class packet_GateServerToSelectGateServer_Data : IProtobufResetInterface
	{
		public void Reset()
		{
			NServerId = default;
			ServerConnectStr = string.Empty;
			OnlinePlayerCount = default;
			CreatedPlayerCount = default;
		}
	}
	public sealed partial class packet_SelectGateServerToGateServer_Data : IProtobufResetInterface
	{
		public void Reset()
		{
			NServerId = default;
			DataBaseConnectStr = string.Empty;
		}
	}
	public sealed partial class packet_SelectGateServerToPlayer_Data : IProtobufResetInterface
	{
		public void Reset()
		{
			NServerId = default;
			ServerConnectStr = string.Empty;
			ServerName = string.Empty;
			NState = default;
		}
	}
	public sealed partial class packet_gsg_SendServerInfo : IProtobufResetInterface
	{
		public void Reset()
		{
			IMessagePool<NetProtocols.SelectGate.packet_GateServerToSelectGateServer_Data>.recycle(MServerInfo);
			MServerInfo = null;
		}
	}
	public sealed partial class packet_sgg_SendServerInfo_Result : IProtobufResetInterface
	{
		public void Reset()
		{
			IMessagePool<NetProtocols.SelectGate.packet_SelectGateServerToGateServer_Data>.recycle(MServerInfo);
			MServerInfo = null;
		}
	}
	public sealed partial class packet_cs_request_ServerList : IProtobufResetInterface
	{
		public void Reset()
		{
		}
	}
	public sealed partial class packet_sc_ServerList_Result : IProtobufResetInterface
	{
		public void Reset()
		{
			NErrorCode = default;
			foreach(var v in MServerInfoList)
			{
				IMessagePool<NetProtocols.SelectGate.packet_SelectGateServerToPlayer_Data>.recycle(v);
			}
			MServerInfoList.Clear();
		}
	}
}
namespace NetProtocols.Login
{
	public sealed partial class packet_cs_Login : IProtobufResetInterface
	{
		public void Reset()
		{
			Account = string.Empty;
			Password = string.Empty;
			NLoginType = default;
		}
	}
	public sealed partial class packet_sc_Login_Result : IProtobufResetInterface
	{
		public void Reset()
		{
			NErrorCode = default;
			NAccountId = default;
			SelectGateServerConnectStr = string.Empty;
			BanReason = string.Empty;
			ExpiryDate = default;
		}
	}
	public sealed partial class packet_cs_Register : IProtobufResetInterface
	{
		public void Reset()
		{
			Account = string.Empty;
			Password = string.Empty;
			NLoginType = default;
			SecretQuestion = string.Empty;
			SecretAnswer = string.Empty;
			EMailAddress = string.Empty;
		}
	}
	public sealed partial class packet_sc_Register_Result : IProtobufResetInterface
	{
		public void Reset()
		{
			NErrorCode = default;
		}
	}
	public sealed partial class packet_cs_ChangePassword : IProtobufResetInterface
	{
		public void Reset()
		{
			Account = string.Empty;
			CurrentPassword = string.Empty;
			NewPassword = string.Empty;
		}
	}
	public sealed partial class packet_sc_ChangePassword_Result : IProtobufResetInterface
	{
		public void Reset()
		{
			NErrorCode = default;
		}
	}
}
namespace NetProtocols.Gate
{
	public sealed partial class packet_IG_Register : IProtobufResetInterface
	{
		public void Reset()
		{
			NServerType = default;
			ServerConnectStr = string.Empty;
		}
	}
	public sealed partial class packet_GI_RegisterResult : IProtobufResetInterface
	{
		public void Reset()
		{
			NErrorCode = default;
		}
	}
	public sealed partial class packet_GIIG_ClientDisConnect : IProtobufResetInterface
	{
		public void Reset()
		{
			NClientId = default;
		}
	}
	public sealed partial class packet_CG_Register : IProtobufResetInterface
	{
		public void Reset()
		{
			NPlayerId = default;
		}
	}
	public sealed partial class packet_GC_RegisterResult : IProtobufResetInterface
	{
		public void Reset()
		{
			NErrorCode = default;
		}
	}
	public sealed partial class packet_data_RelayMsg : IProtobufResetInterface
	{
		public void Reset()
		{
			IdList.Clear();
			MMsg = ByteString.Empty;
			DisConnected = default;
		}
	}
}
namespace NetProtocols.Game
{
	public sealed partial class packet_cs_request_StartGame : IProtobufResetInterface
	{
		public void Reset()
		{
			NPlayerId = default;
		}
	}
	public sealed partial class packet_sc_request_StartGame_Result : IProtobufResetInterface
	{
		public void Reset()
		{
			NErrorCode = default;
			IMessagePool<NetProtocols.Game.packet_data_UserInfo>.recycle(UserInfo);
			UserInfo = null;
		}
	}
	public sealed partial class packet_cs_request_TurnDir : IProtobufResetInterface
	{
		public void Reset()
		{
			Direction = default;
		}
	}
	public sealed partial class packet_cs_request_Walk : IProtobufResetInterface
	{
		public void Reset()
		{
			Direction = default;
		}
	}
	public sealed partial class packet_cs_request_Run : IProtobufResetInterface
	{
		public void Reset()
		{
			Direction = default;
		}
	}
	public sealed partial class packet_sc_UserLocation : IProtobufResetInterface
	{
		public void Reset()
		{
			Direction = default;
			IMessagePool<NetProtocols.Game.packet_data_Vector3Int>.recycle(Location);
			Location = null;
		}
	}
	public sealed partial class packet_sc_broadcast_Location : IProtobufResetInterface
	{
		public void Reset()
		{
			ObjectID = default;
			Direction = default;
			IMessagePool<NetProtocols.Game.packet_data_Vector3Int>.recycle(Location);
			Location = null;
		}
	}
	public sealed partial class packet_cs_chat : IProtobufResetInterface
	{
		public void Reset()
		{
			Message = string.Empty;
			foreach(var v in LinkedItems)
			{
				IMessagePool<NetProtocols.Game.packet_data_cs_ChatInfo>.recycle(v);
			}
			LinkedItems.Clear();
		}
	}
	public sealed partial class packet_sc_chat : IProtobufResetInterface
	{
		public void Reset()
		{
			NType = default;
			Message = string.Empty;
		}
	}
	public sealed partial class packet_data_Vector3Int : IProtobufResetInterface
	{
		public void Reset()
		{
			X = default;
			Y = default;
			Z = default;
		}
	}
	public sealed partial class packet_data_Color : IProtobufResetInterface
	{
		public void Reset()
		{
			R = default;
			G = default;
			B = default;
			A = default;
		}
	}
	public sealed partial class packet_data_SelectRole_RoleInfo : IProtobufResetInterface
	{
		public void Reset()
		{
			NRoleId = default;
			Name = string.Empty;
			Gender = default;
			Class = default;
			Level = default;
			NLastLoginTime = default;
			NCreateTime = default;
		}
	}
	public sealed partial class packet_data_ItemInfo : IProtobufResetInterface
	{
		public void Reset()
		{
			NBagIndex = default;
			NSlotIndex = default;
			NItemId = default;
			NCount = default;
			NStarLevel = default;
			NDura = default;
		}
	}
	public sealed partial class packet_data_UserInfo : IProtobufResetInterface
	{
		public void Reset()
		{
			NMapObjectId = default;
			NPlayerId = default;
			Name = string.Empty;
			Class = default;
			Gender = default;
			NLevel = default;
			NLevelExp = default;
			NMapIndex = default;
			IMessagePool<NetProtocols.Game.packet_data_Vector3Int>.recycle(Location);
			Location = null;
			Direction = default;
			HP = default;
			MP = default;
			foreach(var v in BagList)
			{
				IMessagePool<NetProtocols.Game.packet_data_ItemInfo>.recycle(v);
			}
			BagList.Clear();
			foreach(var v in EquipList)
			{
				IMessagePool<NetProtocols.Game.packet_data_ItemInfo>.recycle(v);
			}
			EquipList.Clear();
			Gold = default;
			Credit = default;
		}
	}
	public sealed partial class packet_data_cs_ChatInfo : IProtobufResetInterface
	{
		public void Reset()
		{
			UniqueID = default;
			Title = string.Empty;
			Grid = default;
		}
	}
	public sealed partial class packet_cs_request_AllRoleInfo : IProtobufResetInterface
	{
		public void Reset()
		{
			NAccountId = default;
		}
	}
	public sealed partial class packet_sc_request_AllRoleInfo_Result : IProtobufResetInterface
	{
		public void Reset()
		{
			NErrorCode = default;
			foreach(var v in MRoleList)
			{
				IMessagePool<NetProtocols.Game.packet_data_SelectRole_RoleInfo>.recycle(v);
			}
			MRoleList.Clear();
		}
	}
	public sealed partial class packet_cs_request_CreateRole : IProtobufResetInterface
	{
		public void Reset()
		{
			NAccountId = default;
			Name = string.Empty;
			Gender = default;
			Class = default;
		}
	}
	public sealed partial class packet_sc_request_CreateRole_Result : IProtobufResetInterface
	{
		public void Reset()
		{
			NErrorCode = default;
			foreach(var v in MRoleList)
			{
				IMessagePool<NetProtocols.Game.packet_data_SelectRole_RoleInfo>.recycle(v);
			}
			MRoleList.Clear();
		}
	}
	public sealed partial class packet_cs_request_DeleteRole : IProtobufResetInterface
	{
		public void Reset()
		{
			NPlayerId = default;
		}
	}
	public sealed partial class packet_sc_request_DeleteRole_Result : IProtobufResetInterface
	{
		public void Reset()
		{
			NErrorCode = default;
			foreach(var v in MRoleList)
			{
				IMessagePool<NetProtocols.Game.packet_data_SelectRole_RoleInfo>.recycle(v);
			}
			MRoleList.Clear();
		}
	}
}
