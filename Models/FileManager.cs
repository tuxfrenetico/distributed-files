using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SharedFiles.Models
{
  public class FileManager
  {
    private Stream stream;

    private Thread thread;
    private Socket socket;

    public Action<ChangedFileModel> StartReceiveFile;
    public Action<ChangedFileModel> EndReceiveFile;

    public FileManager() { }

    public FileManager(Socket socket)
    {
      this.socket = socket;
      stream = new NetworkStream(socket);
      thread = new Thread(new ThreadStart(Receive));
    }

    public void Start() => thread.Start();

    public void Stop()
    {
      thread.Interrupt();
      Console.WriteLine("FileManager Stopped!");
    }

    public bool SocketIsConnected
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

    public void Receive()
    {
      byte[] buffer = new byte[Constants.BLOCK_SIZE];
      int nRead = 0;
      int shifts = 0;
      string strBuffer;

      while (SocketIsConnected)
      {
        try
        {
          if (shifts > 0)
          {
            ShiftArray(buffer, shifts);
            nRead -= shifts;
            strBuffer = ToStringBuffer(buffer, nRead);
            if (strBuffer.Split(';').Length < 4)
              nRead = stream.Read(buffer, nRead, Constants.BLOCK_SIZE - nRead);
          }
          else
          {
            nRead = stream.Read(buffer, 0, Constants.BLOCK_SIZE);
          }

          if (nRead > 0)
          {
            strBuffer = ToStringBuffer(buffer, nRead);
            string[] strArr = strBuffer.Split(';');
            var model = new ChangedFileModel()
            {
              Name = strArr[0],
              Path = strArr[1],
              Type = (WatcherChangeTypes)int.Parse(strArr[2]),
              Size = int.Parse(strArr[3])
            };

            StartReceiveFile?.Invoke(model);

            int lengthProperties = model.ToString().Length;

            if (model.Type == WatcherChangeTypes.Deleted)
            {
              File.Delete(model.Path);
              if (nRead > lengthProperties)
                shifts = lengthProperties;
              else
                shifts = 0;
              EndReceiveFile?.Invoke(model);
              continue;
            }

            if (nRead == (lengthProperties + model.Size))
            {
              ShiftArray(buffer, lengthProperties);
              nRead -= lengthProperties;
            }
            else if (nRead > lengthProperties)
            {
              nRead -= lengthProperties;
              ShiftArray(buffer, lengthProperties);
              if (model.Size > nRead)
              {
                int toRead = model.Size + nRead > Constants.BLOCK_SIZE - nRead
                            ? Constants.BLOCK_SIZE - nRead : (int)model.Size;
                nRead = stream.Read(buffer, nRead, Constants.BLOCK_SIZE - nRead) + nRead;
              }
            }
            else
              nRead = stream.Read(buffer, 0, Constants.BLOCK_SIZE);

            if (!Directory.Exists(Path.GetDirectoryName(model.Path)))
              Directory.CreateDirectory(Path.GetDirectoryName(model.Path));

            long missingBytes = model.Size;
            using (FileStream fs = new FileStream(model.Path, FileMode.OpenOrCreate))
            {
              if (missingBytes <= nRead)
              {
                fs.Write(buffer, 0, (int)missingBytes);
                shifts = (int)missingBytes;
              }
              else
              {
                while (missingBytes > 0)
                {
                  fs.Write(buffer, 0, nRead);
                  missingBytes -= nRead;
                  nRead = stream.Read(buffer, 0, missingBytes > Constants.BLOCK_SIZE ? Constants.BLOCK_SIZE : (int)missingBytes);
                }
                shifts = 0;
              }
            }
            Console.WriteLine($"Arquivo recebido: {model.ToString()}");
            EndReceiveFile?.Invoke(model);
          }
          else
            Console.WriteLine("Nada lido do socket!");
        }
        catch (Exception ex)
        {
          Console.WriteLine("ERRO:");
          Console.WriteLine(ex.Message);
          Console.WriteLine(ex.StackTrace);
        }
      }
      Console.WriteLine("Socket disconectado!");
    }

    /**
     * Move o array da posição indicada para o início do array.
     */
    private static void ShiftArray<T>(T[] arr, int count)
    {
      T[] temp = new T[arr.Length - count];
      Array.Copy(arr, count, temp, 0, temp.Length);
      Array.Copy(temp, 0, arr, 0, temp.Length);
    }

    private string ToStringBuffer(byte[] buffer, int nRead)
    {
      StringBuilder sb = new StringBuilder();
      buffer.ToList().ForEach(p => sb.Append((char)p));
      return sb.ToString().Substring(0, nRead);
    }

    public void Send(FileModel model, Socket socket)
    {
      Send(new ChangedFileModel()
      {
        Path = model.Path,
        Name = model.Name,
        Type = WatcherChangeTypes.Created
      }, new List<Socket>() { socket });
    }

    public void Send(ChangedFileModel model, Socket socket) => Send(model, new List<Socket>() { socket });

    public void Send(ChangedFileModel model, List<Socket> sockets = null)
    {
      try
      {
        if (model.Type != WatcherChangeTypes.Deleted)
          model.Size = new FileInfo(model.Path).Length;
        sockets.ForEach(s => s.Send(model.ToStringBytes()));
        if (model.Type != WatcherChangeTypes.Deleted)
        {
          using (FileStream fs = new FileStream(model.Path, FileMode.Open))
          {
            int nRead = 0;
            byte[] buffer = new byte[Constants.BLOCK_SIZE];
            nRead = fs.Read(buffer, 0, Constants.BLOCK_SIZE);
            while (nRead > 0)
            {
              foreach (Socket s in sockets)
              {
                try
                {
                  if (s.Connected)
                    s.Send(buffer, 0, nRead, SocketFlags.None);
                }
                catch (Exception e)
                {
                  Console.WriteLine($"ERRO ENVIANDO ARQUIVO: {e.Message}");
                }
              }
              nRead = fs.Read(buffer, 0, Constants.BLOCK_SIZE);
            }
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine($"ERRO AO NOTIFICAR ALTERAÇÃO DE ARQUIVO: {e.Message}");
      }
    }
  }
}