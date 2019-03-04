// Author: Dan Brunwasser (drb8650)

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ftp
{
	// Wrapper for data connection socket
	class DataSocket : IDisposable
	{
		Socket socket;
		Socket listener;

		// Creates a new socket wrapper using the given command connection
		public DataSocket(FTPSocket ftp)
		{
			if (Program.Passive)
			{
				// If in passive mode, send a PASV command
				ftp.Write("PASV");
				var (status, message) = ftp.ReadStatus();

				if (status != 227)
				{
					Console.WriteLine(message);
					return;
				}

				var match = Regex.Match(message, @"\((\d+,\d+,\d+,\d+),(\d+),(\d+)\)");
				var address = IPAddress.Parse(match.Groups[1].Value.Replace(',', '.'));
				var port = int.Parse(match.Groups[2].Value) * 256 + int.Parse(match.Groups[3].Value);

				socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				socket.Connect(address, port);
			}
			else
			{
				// In in active mode, create a listening server for the connection
				listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				var address = ftp.LocalIP;
				listener.Bind(new IPEndPoint(address, 0));
				listener.Listen(1);

				var ip = address.ToString().Replace('.', ',');
				var port = ((IPEndPoint)listener.LocalEndPoint).Port;

				ftp.Write("PORT {0},{1},{2}", ip, port / 256, port % 256);
				var (status, message) = ftp.ReadStatus();

				if (status != 200)
				{
					throw new Exception("Data connection failed");
				}
			}
		}

		// Prepares the data connection for transfer
		void Prepare()
		{
			if (listener != null)
			{
				socket = listener.Accept();
			}
		}

		// Reads text from the server over the data connection
		public string ReadText()
		{
			Prepare();

			var length = 0;
			var buffer = new byte[4096];
			var text = "";
			do
			{
				length = socket.Receive(buffer);
				text += Encoding.ASCII.GetString(buffer, 0, length);
			} while (length > 0);

			return text;
		}

		// Outputs the data connection stream into a file
		public void ReadToFile(string path)
		{
			Prepare();

			using (var file = File.Create(path))
			{
				var length = 0;
				var buffer = new byte[4096];
				do
				{
					length = socket.Receive(buffer);
					file.Write(buffer, 0, length);
				} while (length > 0);
			}
		}

		// Dispose of the data connection
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
