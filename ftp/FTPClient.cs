// Author: Dan Brunwasser (drb8650)

using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace ftp
{
	class FTPClient
	{
		// Runs the server continuously until quit
		public static void Run(FTPSocket socket)
		{
			var stop = false;
			while (!stop)
			{
				stop = HandleCommand(socket);
			}
		}

		// Handles a command from the client using the given socket
		static bool HandleCommand(FTPSocket socket)
		{
			// Writes a user prompt and extracts the command and arguments from user input
			Console.Write("  > ");
			var input = Console.ReadLine();
			var match = Regex.Match(input, @"([^ ]+)(?: (.+))?");

			var command = "";
			var arguments = "";

			if (match.Groups[1].Success)
			{
				command = match.Groups[1].Value.ToUpper();
			}
			if (match.Groups[2].Success)
			{
				arguments = match.Groups[2].Value;
			}

			switch (command)
			{
				// Sets the server to transfer in ASCII mode
				case "ASCII":
					socket.Write("TYPE A");
					Console.WriteLine(socket.Read());
					break;

				// Makes the server transfer in binary mode
				case "BINARY":
					socket.Write("TYPE I");
					Console.WriteLine(socket.Read());
					break;

				// Changes the server to the given directory
				case "CD":
					if (arguments.Length == 0)
					{
						Console.WriteLine("    CD requires a path argument");
						break;
					}

					socket.Write("CWD {0}", arguments);
					Console.WriteLine(socket.Read());
					break;

				// Navigates up one directory on the server
				case "CDUP":
					socket.Write("CDUP");
					Console.WriteLine(socket.Read());
					break;

				// Endables debug mode, printing messages for each message sent over the data connection
				case "DEBUG":
					Program.Debug = !Program.Debug;
					if (Program.Debug)
					{
						Console.WriteLine("    Debug mode enabled");
					}
					else
					{
						Console.WriteLine("    Debug mode disabled");
					}
					break;

				// Prints a directory listing for the current directory
				case "DIR":
					using (var data = new DataSocket(socket))
					{
						socket.Write("LIST");
						var (status2, message) = socket.ReadStatus();
						Console.WriteLine(message);
						if (status2 == 150)
						{
							Console.WriteLine(data.ReadText());
							Console.WriteLine(socket.Read());
						}
					}
					break;

				// Gets the specified file from the server
				case "GET":
					using (var data = new DataSocket(socket))
					{
						socket.Write("RETR {0}", arguments);
						var (status2, message) = socket.ReadStatus();
						Console.WriteLine(message);
						if (status2 == 150)
						{
							var path = arguments.Split('/');
							data.ReadToFile(path[path.Length - 1]);
							Console.WriteLine(socket.Read());
						}
					}
					break;

				// Prints a help message
				case "HELP":
					Console.WriteLine("Available commands:");
					Console.WriteLine("ASCII: sets the server to ASCII transfer mode");
					Console.WriteLine("BINARY: sets the server to binary transfer mode");
					Console.WriteLine("CD <path>: navigates to the given path on the server");
					Console.WriteLine("CDUP: naigates up one directory on the server");
					Console.WriteLine("DEBUG: toggles debug mode, which prints all commands sent");
					Console.WriteLine("DIR: prints a directory listing for the current server directory");
					Console.WriteLine("GET <file>: downloads the specified file from the server");
					Console.WriteLine("HELP: prints this help message");
					Console.WriteLine("PASSIVE: toggles passive file transfer mode");
					Console.WriteLine("PWD: prints the current directory from the server");
					Console.WriteLine("QUIT: closes the FTP connection and exits");
					break;

				// Switches between passive and active transfer
				case "PASSIVE":
					Program.Passive = !Program.Passive;
					if (Program.Passive)
					{
						Console.WriteLine("    Passive mode enabled");
					}
					else
					{
						Console.WriteLine("    Passive mode disabled");
					}
					break;

				// Prints the current directory on the server
				case "PWD":
					socket.Write("PWD");
					Console.WriteLine(socket.Read());
					break;

				// Quits the FTP connetion
				case "QUIT":
					socket.Write("QUIT");
					Console.WriteLine(socket.Read());
					return true;

				// Handles unknown commands
				default:
					Console.WriteLine("    Command not recognized");
					break;
			}

			return false;
		}
	}
}
