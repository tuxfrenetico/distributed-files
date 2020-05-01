using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using SharedFiles.Models;
using static System.Console;

namespace Client
{
  class Program
  {
    static void Main(string[] args)
    {
      try
      {
        if (Directory.Exists(Constants.ROOT_PATH))
          Directory.Delete(Constants.ROOT_PATH, true);
        TcpClient client = new TcpClient();
        client.Connect(args.Length > 0 ? args[0] : Constants.SERVER_IP, Constants.SERVER_PORT);
        FileManager fm = new FileManager(client.Client);
        FileWatcher fw = new FileWatcher();
        var tempReceivedFiles = new List<ChangedFileModel>();

        // Parar Watcher enquanto estiver recebendo arquivo
        fm.StartReceiveFile = (file) => fw.Pause();
        fm.EndReceiveFile = (file) =>
        {
          tempReceivedFiles.Add(file);
          fw.Resume();
        };

        fw.NotifyChange = (file) =>
        {
          var temp = tempReceivedFiles.FirstOrDefault(p => p.Path == file.Path);
          if (temp != null)
            tempReceivedFiles.Remove(temp);
          else
            fm.Send(file, client.Client);
        };

        fm.Start();
        fw.Start();
        WriteLine("Running...");
        Console.ReadKey();
        client.Client.Disconnect(false);
        fw.Stop();
        fm.Stop();
      }
      catch (Exception ex)
      {
        WriteLine($"ERRO: {ex.Message}");
        WriteLine($"{ex.StackTrace}");
      }
    }
  }
}