// Author: Dan Brunwasser (drb8650)

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ftpServer
{
	// Main class for server program
	class Program
	{
		public static bool Debug { get; set; }

		public static string Welcome = "Welcome to the FTP server";

		// Main function for running the FTP server
		static void Main(string[] args)
		{
			Debug = true;

			// Creates listener port for accepting FTP connections
			var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			try
			{
				// Attempts to bind to port 2121
				listener.Bind(new IPEndPoint(IPAddress.Any, 2121));
			}
			catch
			{
				// Stops program if port 2121 is taken
				Console.WriteLine("Error: unable to bind to port");
				return;
			}
			listener.Listen(1);

			Console.WriteLine("Accepting clients");

			// For every new client, create a new server class and run it in a new thread
			while (true)
			{
				var socket = listener.Accept();
				var server = new FTPServer(socket);
				Task.Run(() => server.Run());
			}
		}
	}
}
