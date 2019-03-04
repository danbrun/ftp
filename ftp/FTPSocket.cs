// Author: Dan Brunwasser (drb8650)

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp
{
	// FTP Socket class wraps command connection
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

		// Creates command connection with the given host and port
		public FTPSocket(string host, int port)
		{
			var ip = Dns.GetHostAddresses(host)[0];

			socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Connect(ip, port);

			// Print welcome message
			Console.WriteLine();
			Console.WriteLine(Read());

			// Authenticate username
			while (true)
			{
				Console.Write("    Username: ");
				var user = Console.ReadLine();

				Write("USER {0}", user);
				var (status, message) = ReadStatus();

				if (status == 331)
				{
					break;
				}

				Console.WriteLine(message);
			}

			// Authenticate password
			while (true)
			{
				Console.Write("    Password: ");
				var pass = Console.ReadLine();

				Write("PASS {0}", pass);
				var (status, message) = ReadStatus();

				if (status == 230)
				{
					break;
				}

				Console.WriteLine(message);
			}
		}

		// Reads a response from the command connection
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

			if (text.Substring(0, 3) == "421")
			{
				throw new TimeoutException();
			}

			return text;
		}

		// Reads the command and isolates the status code
		public (int, string) ReadStatus()
		{
			var text = Read();
			var status = int.Parse(text.Substring(0, 3));

			return (status, text);
		}

		// Writes a command to the server
		public void Write(string text)
		{
			socket.Send(Encoding.ASCII.GetBytes(text + "\r\n"));

			if (Program.Debug)
			{
				Console.WriteLine("--> {0}", text);
			}
		}

		// Writes with string formatting
		public void Write(string format, params object[] args)
		{
			Write(string.Format(format, args));
		}

		// Disposes of the command connection
		public void Dispose()
		{
			socket.Dispose();
		}
	}
}
