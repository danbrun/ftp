// Author: Dan Brunwasser (drb8650)

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftpServer
{
	class FTPSocket : IDisposable
	{
		Socket socket;

		// Gets the IP address of the lcoal connection
		public IPAddress LocalIP
		{
			get
			{
				return ((IPEndPoint)socket.LocalEndPoint).Address;
			}
		}

		// Creates a wrapped socket using the given internal socket
		public FTPSocket(Socket underlying)
		{
			socket = underlying;

			Write("220 {0}", Program.Welcome);
		}

		// Reads a command from the client connection
		public string Read()
		{
			var length = 0;
			var buffer = new byte[4096];
			var text = "";

			do
			{
				length = socket.Receive(buffer);
				text += Encoding.ASCII.GetString(buffer, 0, length);
			} while (length == 4096);

			text = text.Substring(0, text.Length - 2);

			if (Program.Debug)
			{
				Console.WriteLine("<-- {0}", text);
			}

			return text;
		}

		// Writes a response back to the client
		public void Write(string text)
		{
			socket.Send(Encoding.ASCII.GetBytes(text + "\r\n"));

			if (Program.Debug)
			{
				Console.WriteLine("--> {0}", text);
			}
		}

		// Writes a response with string formatting
		public void Write(string format, params object[] args)
		{
			Write(string.Format(format, args));
		}

		// Disposes of the connection
		public void Dispose()
		{
			socket.Dispose();
		}
	}
}
