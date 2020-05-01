using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SharedFiles.Models;

namespace SharedFiles.Server
{
  public class Server
  {
    private List<ClientManager> clients;
    private FileWatcher watcher;
    private Thread threadListener;
    private static TcpListener listener;
    private bool listening;
    private FileManager fileManager;
    private List<ChangedFileModel> tempChangedFiles;

    public int receiving;

    public Server()
    {
      fileManager = new FileManager();
      watcher = new FileWatcher();
      clients = new List<ClientManager>();
      listener = new TcpListener(IPAddress.Parse(Constants.SERVER_IP), Constants.SERVER_PORT);
      tempChangedFiles = new List<ChangedFileModel>();
    }

    public void Start()
    {
      watcher.Start();
      watcher.NotifyChange = file =>
      {
        Console.WriteLine($"{file.TypeDescription}: {file.ToString()}");
        var fileChanged = tempChangedFiles.FirstOrDefault(p => p.Path == file.Path);
        if (fileChanged != null)
        {
          BroadcastFile(file);
        }
        else
          tempChangedFiles.Remove(fileChanged);
      };
      listening = true;
      if (threadListener == null)
        threadListener = new Thread(new ThreadStart(Listen));
      if (threadListener.ThreadState != ThreadState.Running)
        threadListener.Start();
    }

    public void Stop()
    {
      try
      {
        listening = false;
        Console.WriteLine("Stopping server...");
        watcher.Stop();
        threadListener.Interrupt();
        listener.Server.Close();
        listener.Stop();
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
      }
    }

    public void Listen()
    {
      listener.Start();
      Console.WriteLine($"Listen connections in {Constants.SERVER_IP}:{Constants.SERVER_PORT}");
      Console.WriteLine("Press any key for exit");
      while (listening)
      {
        try
        {
          var socket = listener.AcceptSocket();
          if (socket != null)
          {
            var cm = new ClientManager(socket);
            cm.StartReceiveFile = (file, clientId) =>
            {
              if (receiving == 0)
                watcher.Pause();
              receiving++;
            };
            cm.EndReceiveFile = (file, clientId) =>
            {
              Console.WriteLine($"Received: {file.ToString()}");
              receiving--;
              if (receiving == 0)
              {
                BroadcastFile(file, clientId);
                watcher.Resume();
              }
            };

            clients.Add(cm);
            watcher.CurrentFiles.ForEach(p => fileManager.Send(p, socket));
            cm.Start();
            Console.WriteLine($"New client: {cm.Id}");
          }
        }
        catch (ThreadInterruptedException) { }
        catch (Exception e)
        {
          Console.WriteLine($"CONNECTING CLIENT ERROR: {e.Message}");
          Console.WriteLine($"{e.StackTrace}");
        }
      }
    }

    private void BroadcastFile(ChangedFileModel file, int excludedClient = 0)
    {
      RemoveDisconnectedClients();
      var cs = clients.Where(p => p.Id != excludedClient).Select(p => p.Socket).ToList();
      if (cs.Count == 0)
        Console.WriteLine("Nenhum cliente para broadcast!");
      else
      {
        fileManager.Send(file, cs);
        Console.WriteLine($"Broadcast enviado para {cs.Count} clientes!");
      }
    }

    private void RemoveDisconnectedClients()
    {
      var disconnected = clients.Where(p => !p.IsConnected).ToList();
      clients = clients.Where(p => p.IsConnected).ToList();
      disconnected.ForEach(p => p.Dispose());
    }
  }
}