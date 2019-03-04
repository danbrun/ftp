// Author: Dan Brunwasser (drb8650)

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ftp
{
	// Main class for the ftp client program
	class Program
	{
		public static bool Debug { get; set; }
		public static bool Passive { get; set; }

		// Main function accepts command line arguments
		static void Main(string[] args)
		{
			Debug = false;
			Passive = false;

			if (args.Length == 0 || args.Length > 2)
			{
				Console.WriteLine("Usage: ftp <server> [port]");
				return;
			}

			// Get host and port from arguments
			var host = args[0];
			var port = 21;
			if (args.Length == 2)
			{
				int.TryParse(args[1], out port);
			}

			try
			{
				// Creates an FTP command socket and runs the command handler with it
				using (var ftp = new FTPSocket(host, port))
				{
					FTPClient.Run(ftp);
				}
			}
			catch (SocketException)
			{
				Console.WriteLine("Unable to resolve or connect to host");
			}
			catch (TimeoutException)
			{
				Console.WriteLine("421 Timeout.");
			}
			catch
			{
				Console.WriteLine("An unexpected error occurred.");
			}
		}
	}
}
