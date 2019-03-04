// Author: Dan Brunwasser (drb8650)

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace ftpServer
{
	// Server class allows for multiple connected clients
	class FTPServer
	{
		Socket _socket;
		FTPSocket ftp;
		string path;
		DataSocket data;
		bool user = false;
		bool pass = false;

		// Creates a server class with the given socket and starting in the current directory
		public FTPServer(Socket socket)
		{
			_socket = socket;
			path = "/";
		}

		// Listens for commands from client on a loop
		public void Run()
		{
			Console.WriteLine("New connection");

			// Create a wrapped socekt for sending and receivng
			using (ftp = new FTPSocket(_socket))
			{
				try
				{
					// Run until something goes wrong or the quit command is received
					var stop = false;
					while (!stop)
					{
						stop = HandleCommand();
					}
				}
				catch
				{
					Console.WriteLine("Unexpect error occured");
				}
			}
			Console.WriteLine("Connection closed");
		}

		// Helper function for keeping the path simple
		static string Normalize(string path)
		{
			var allDirs = path.Split("/");
			var dirs = new List<string>();
			foreach (string dir in allDirs)
			{
				if (dir == "..")
				{
					if (dirs.Count > 0)
					{
						dirs.RemoveAt(dirs.Count - 1);
					}
				}
				else if (dir != "" && dir != ".")
				{
					dirs.Add(dir);
				}
			}

			path = "";
			foreach (string dir in dirs)
			{
				path += "/" + dir;
			}

			if (path.Length == 0)
			{
				path = "/";
			}
			return path;
		}

		// Listens and responds to commands from the client
		public bool HandleCommand()
		{
			// Reads and extracts the command and arguments
			var input = ftp.Read();
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

			// Stops if the client is not authenticated or authenticating
			if ((!user && command != "USER") || (!pass && command != "USER" && command != "PASS"))
			{
				ftp.Write("530 Please login with USER and PASS.");
				return false;
			}

			switch (command)
			{
				// Moves the current path up 1 directory
				case "CDUP":
					path = Normalize(Path.Combine(".", path, ".."));
					ftp.Write("250 Directory successfully changed.");
					break;

				// Moves to the specified directory
				case "CWD":
					if (arguments.Length > 0)
					{
						var temp = Path.Combine(".", path, arguments);
						if (Directory.Exists(temp))
						{
							path = Normalize(temp);
							ftp.Write("250 Directory successfully changed.");
							break;
						}
					}
					ftp.Write("550 Failed to change directory.");
					break;

				// Lists files in the current directory
				case "LIST":
					if (data == null)
					{
						ftp.Write("425 Use PASV or PORT first.");
						break;
					}

					ftp.Write("150 Here comes the directory listing.");
					data.SendDir(path);
					ftp.Write("226 Directory send OK.");
					data = null;
					break;

				// Accepts a password from the client
				case "PASS":
					ftp.Write("230 Login successful.");
					pass = true;
					break;

				// Sets up a passive mode data connection
				case "PASV":
					data = new DataSocket(ftp, true, null);
					break;

				// Sets up an active mode data connection
				case "PORT":
					data = new DataSocket(ftp, false, arguments);
					break;

				// Prints the working directory to the client
				case "PWD":
					ftp.Write("257 \"{0}\"", path);
					break;

				// Lets the client quit the connection
				case "QUIT":
					ftp.Write("221 Goodbye.");
					return true;

				// Sends a file back to the client
				case "RETR":
					if (data == null)
					{
						ftp.Write("425 Use PASV or PORT first.");
						break;
					}

					var file = Path.GetRelativePath(".", Path.Combine(path, arguments));

					if (!File.Exists(file))
					{
						ftp.Write("550 Failed to open file.");
						break;
					}

					ftp.Write("150 Opening BINARY mode data connection for {0}", arguments);
					data.SendFile(file);
					ftp.Write("226 Transfer complete.");
					data = null;
					break;

				// Sets the file transfer encoding
				case "TYPE":
					if (arguments == "I")
					{
						ftp.Write("200 Switching to Binary mode.");
						break;
					}

					ftp.Write("200 Only Binary mode is supported.");
					break;

				// Accepts usernames for authentication
				case "USER":
					if (arguments == "ftp" || arguments == "anonymous")
					{
						ftp.Write("331 Please specify the password.");
						user = true;
						break;
					}

					ftp.Write("530 This FTP server is anonymous only.");
					break;

				// Handles unknown commands
				default:
					ftp.Write("200 Command not supported.");
					break;
			}

			return false;
		}
	}
}
