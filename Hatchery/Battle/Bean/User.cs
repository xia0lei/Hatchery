using System;

namespace Hatchery.Battle.Bean
{
	public enum GameStatus
	{
		Enter = 1,
		Playing = 2,
		Disconnected = 3
	}
	public class User
	{
		public Int64 Uid{get;set;}
		public Int32 Aid{get;set;}
		public Int32 BattleId{get;set;}
		public Int64 ConnectionId{get;set;}
		public int TcpObjectId{get;set;}
	}

	public class GameUser
	{
		public Int64 Uid{get;set;}
		public Int32 Aid{get;set;}
		public Int32 BattleId{get;set;}
		public Int64 ConnectionId{get;set;}
		public int TcpObjectId{get;set;}
		public GameStatus status{get;set;}
	}
}