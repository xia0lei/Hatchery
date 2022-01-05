using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Google.Protobuf;
using Hatchery.Framework.ProtoSchema;
using Hatchery.Framework.Service;
using Hatchery.Framework.Utility;
using Hatchery.Network;

namespace Hatchery.Battle.Test
{
	class Client
	{
		private Socket m_clientSocket;
		private const int MaxPacketSize = 64 * 1024;
		private const int PacketHeaderSize = 2;
		private byte[] m_writeCache;
		private OutboundPacketManager m_outBoundPacketManager = new OutboundPacketManager();
		private InboundPacketManager m_inBoundPacketManager = new InboundPacketManager();
		private SocketAsyncEventArgs m_writeEvent = new SocketAsyncEventArgs();
		private SocketAsyncEventArgs m_readEvent = new SocketAsyncEventArgs();
		private const Int32 ReceiveOperation = 1, SendOperateion = 2;
		private Boolean connected = false;
		private IPEndPoint m_hostEndPoint;
		private static AutoResetEvent autoConnectEvent = new AutoResetEvent(false);
		private static AutoResetEvent[] autoSendReceiveEvents = new AutoResetEvent[]{
			new AutoResetEvent(false),
			new AutoResetEvent(false)
		};

		public bool Connected { get => connected; set => connected = value; }

		internal Client(int addr, String host, Int32 port)
		{
			var address = IPAddress.Parse(host);
			m_hostEndPoint = new IPEndPoint(address, port);
			m_clientSocket = new Socket(m_hostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			m_writeCache = new byte[MaxPacketSize + PacketHeaderSize];
			m_outBoundPacketManager.Init(new BufferPool());
			m_inBoundPacketManager.Init(addr, 0, new BufferPool(), OnReadPacketComplete, null);

			m_writeEvent.Completed += IO_Complete;
			m_readEvent.Completed += IO_Complete;
		}

		private void IO_Complete(object sender, object o)
		{
			SocketAsyncEventArgs args = o as SocketAsyncEventArgs;
			switch (args.LastOperation)
			{
				case SocketAsyncOperation.Send:
					{
						OnWriteComplete(o);
					}
					break;
				case SocketAsyncOperation.Receive:
					{
						OnRecvComplete(o);
					}
					break;
				case SocketAsyncOperation.Connect:
					{
						OnConnectComplete(o);
					}
					break;
				default:
					{
						throw new Exception("Socket error:" + args.LastOperation);
					}
					break;
			}
		}

		private void OnConnectComplete(object o)
		{
			SocketAsyncEventArgs args = o as SocketAsyncEventArgs;
			if (args.SocketError == System.Net.Sockets.SocketError.Success)
			{
				m_writeEvent.RemoteEndPoint = null;
				connected = true;
				BeginRecv();
			}
			else
			{
			}
		}



		internal void Connect()
		{
			m_writeEvent.RemoteEndPoint = m_hostEndPoint;
			bool willRaiseEvent = m_clientSocket.ConnectAsync(m_writeEvent);
			if (!willRaiseEvent)
			{
				OnConnectComplete(m_writeEvent);
			}
		}

		internal bool Write(byte[] buffer)
		{
			if (this.connected == false)
			{
				Console.Write("Client disconnected");
				return false;
			}
			if (buffer.Length > MaxPacketSize)
			{
				Console.Write("buffer length is too large");
				return false;
			}

			int packetSize = buffer.Length;
			m_writeCache[0] = (byte)(packetSize >> 8);
			m_writeCache[1] = (byte)(packetSize & 0xff);
			buffer.CopyTo(m_writeCache, PacketHeaderSize);
			m_outBoundPacketManager.ProcessPacket(m_writeCache, packetSize + PacketHeaderSize);
			if (m_outBoundPacketManager.HeadBuffer == null)
			{
				BeginWrite();
			}
			return true;
		}

		private void BeginWrite()
		{
			if (m_outBoundPacketManager.HeadBuffer == null)
			{
				m_outBoundPacketManager.NextBuffer();
			}

			if (m_outBoundPacketManager.HeadBuffer == null)
			{
				if (this.connected == false)
				{
					Stop();
				}
				return;
			}

			var buf = m_outBoundPacketManager.HeadBuffer;
			m_writeEvent.SetBuffer(buf.Memory, buf.Begin, buf.End - buf.Begin);
			bool willRaiseEvent = m_clientSocket.SendAsync(m_writeEvent);
			if (!willRaiseEvent)
			{
				OnWriteComplete(m_writeEvent);
			}
		}

		private void OnWriteComplete(object o)
		{
			SocketAsyncEventArgs args = o as SocketAsyncEventArgs;
			if (args.SocketError == System.Net.Sockets.SocketError.Success)
			{
				if (args.BytesTransferred == 0)
				{
					Console.WriteLine("Write Disconnect");
				}
				else
				{
					Hatchery.Network.Buffer buf = m_outBoundPacketManager.HeadBuffer;
					buf.Begin += args.BytesTransferred;
					if (buf.Begin >= buf.End)
					{
						m_outBoundPacketManager.NextBuffer();
					}

					BeginWrite();
				}
			}
			else
			{
				if (this.connected == false)
				{
					LoggerHelper.Info(0, "Session Disconnected");
					Stop();
				}
				else
				{
					Console.WriteLine("Session SocketError");
				}
			}
		}

		private void Stop()
		{
			m_clientSocket.Close();
			m_writeEvent.Dispose();
			m_readEvent.Dispose();
			m_outBoundPacketManager.Stop();
		}


		private void OnRecvComplete(object o)
		{
			if (this.connected == false)
			{
				return;
			}

			SocketAsyncEventArgs args = o as SocketAsyncEventArgs;
			if (args.SocketError == System.Net.Sockets.SocketError.Success)
			{
				if (args.BytesTransferred == 0)
				{

				}
				else
				{
					m_inBoundPacketManager.ProcessPacket(m_inBoundPacketManager.InboundBuffer, args.BytesTransferred);
				}

				BeginRecv();
			}
			else
			{
			}
		}


		private void OnReadPacketComplete(int opaque, long sessionId, byte[] buffer, int packetSize)
		{
			LoggerHelper.Info(0, "OnReadPacketComplete");
			SocketData data = new SocketData();
			data.Connection = sessionId;
			data.Buffer = Convert.ToBase64String(buffer);

			Message msg = new Message();
			msg.Source = 0;
			msg.Destination = opaque;
			msg.Method = "SocketData";
			msg.Data = data.ToByteArray();
			msg.RPCSession = 0;
			msg.Type = MessageType.Socket;

			LoggerHelper.Info(0, String.Format("opaque {0}", opaque));
			ServiceContext service = ServiceSlots.GetInstance().Get(opaque);
			service.Push(msg);
		}

		private void OnSessionError(int opaque, long sessionId, string remoteEndPoint, int errorCode, string errorText)
		{

		}

		private void BeginRecv()
		{
			if (this.connected == false)
			{
				return;
			}

			m_readEvent.SetBuffer(m_inBoundPacketManager.InboundBuffer, 0, m_inBoundPacketManager.InboundBuffer.Length);
			bool willRaiseEvent = m_clientSocket.ReceiveAsync(m_readEvent);
			if (!willRaiseEvent)
			{
				OnRecvComplete(m_readEvent);
			}
		}
	}
}