using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SharedFiles.Server
{
  class Program
  {
    static void Main(string[] args)
    {
      try
      {
        Server server = new Server();
        server.Start();
        Console.ReadKey();
        Console.WriteLine("");
        server.Stop();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"ERRO: {ex.Message}");
        Console.WriteLine($"{ex.StackTrace}");
      }
    }
  }
}