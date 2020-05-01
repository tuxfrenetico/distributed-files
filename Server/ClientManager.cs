using System;
using System.Net.Sockets;
using SharedFiles.Models;

namespace SharedFiles.Server
{
  public class ClientManager
  {
    private static int currentId = 0;
    private Socket socket;
    private FileManager manager;
    public int Id { get; set; }

    public Action<ChangedFileModel, int> StartReceiveFile { get; set; }
    public Action<ChangedFileModel, int> EndReceiveFile { get; set; }

    public Socket Socket { get { return socket; } }

    public ClientManager(Socket socket)
    {
      Id = ++currentId;
      this.socket = socket;
      manager = new FileManager(socket);
      manager.StartReceiveFile = (file) => StartReceiveFile?.Invoke(file, Id);
      manager.EndReceiveFile = (file) => EndReceiveFile?.Invoke(file, Id);
    }

    public bool IsConnected
    {
      get
      {
        bool part1 = socket.Poll(1000, SelectMode.SelectRead);
        bool part2 = (socket.Available == 0);
        if (part1 & part2)
          return false;
        else
          return true;
      }
    }

    public void Start() => manager.Start();

    public void Dispose()
    {
      manager.Stop();
      if (IsConnected)
        socket.Disconnect(false);
      socket.Dispose();
      this.Dispose();
    }
  }
}