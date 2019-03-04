// Author: Dan Brunwasser (drb8650)

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ftpServer
{
	// Wrapper for data connection sockets
	class DataSocket : IDisposable
	{
		Socket socket;
		Socket listener;
		IPAddress address;
		int port;

		// Creates a data connection using the given command socket, mode, and message from the client giving the port
		public DataSocket(FTPSocket ftp, bool passive, string message)
		{
			if (passive)
			{
				// Establishes a server for the client to connect to in passive mode
				listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				listener.Bind(new IPEndPoint(ftp.LocalIP, 0));
				listener.Listen(1);

				var local = (IPEndPoint)listener.LocalEndPoint;
				var ip = local.Address.ToString().Replace('.', ',');
				var port = local.Port;

				ftp.Write("227 Entering Passive Mode ({0},{1},{2}).", ip, port / 256, port % 256);
			}
			else
			{
				// Connects to the clients data connection in active mode
				var match = Regex.Match(message, @"(\d+,\d+,\d+,\d+),(\d+),(\d+)");
				address = IPAddress.Parse(match.Groups[1].Value.Replace(',', '.'));
				port = int.Parse(match.Groups[2].Value) * 256 + int.Parse(match.Groups[3].Value);

				socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				ftp.Write("200 PORT command successful. Consider using PASV.");
			}
		}

		// Sets up a data connection for transmission
		void Prepare()
		{
			if (listener == null)
			{
				socket.Connect(address, port);
			}
			else
			{
				socket = listener.Accept();
			}
		}

		// Sends a directory listing to the client
		public void SendDir(string path)
		{
			Prepare();

			var listing = "Directories:\r\n";
			var dirs = Directory.GetDirectories(path);
			foreach (string dir in dirs)
			{
				listing += dir + "\r\n";
			}

			listing += "\r\nFiles:\r\n";
			var files = Directory.GetFiles(path);
			foreach (string file in files)
			{
				listing += file + "(";
				listing += new FileInfo(Path.Combine(path, file)).Length;
				listing += " bytes)\r\n";
			}

			socket.Send(Encoding.ASCII.GetBytes(listing));
			socket.Disconnect(false);
		}

		// Sends a binary file to the client
		public void SendFile(string path)
		{
			Prepare();

			socket.SendFile(path);
			socket.Disconnect(false);
		}

		// Disposes of the data connection
		public void Dispose()
		{
			if (socket != null)
			{
				socket.Dispose();
			}
			if (listener != null)
			{
				listener.Dispose();
			}
		}
	}
}
