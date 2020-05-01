using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SharedFiles.Models
{
  public class FileWatcher
  {
    private Thread thread;
    public Action<ChangedFileModel> NotifyChange { get; set; }
    private List<FileModel> files;
    public List<FileModel> CurrentFiles { get { return files; } }

    private bool paused = true;
    public FileWatcher()
    {
      files = new List<FileModel>();
      if (!Directory.Exists(Constants.ROOT_PATH))
        Directory.CreateDirectory(Constants.ROOT_PATH);
    }

    public void Pause() => paused = true;
    public void Resume() => paused = false;

    public void Start()
    {
      Console.WriteLine("Watcher starting...");
      if (thread == null)
        thread = new Thread(new ThreadStart(Watch));
      if (thread.ThreadState != ThreadState.Running)
        thread.Start();
      paused = false;
      Console.WriteLine("Watcher started!");
    }

    public void Stop()
    {
      paused = true;
      thread.Interrupt();
      Console.WriteLine("Watcher stopped!");
    }

    private void Watch()
    {
      while (true)
      {
        if (!paused)
        {
          var currentFiles = Directory.GetFiles(Constants.ROOT_PATH);

          List<FileModel> addedFiles = currentFiles.Where(p => !files.Any(x => p == x.Path))
            .Select(p => new FileModel() { Path = p, Name = Path.GetFileName(p), ModifyDate = File.GetLastWriteTime(p) })
            .ToList();
          List<FileModel> excludedFiles = files.Where(p => !currentFiles.Any(x => x == p.Path)).ToList();
          List<FileModel> modifiedFiles = files.Where(p => currentFiles
                                                          .Any(x => x == p.Path && File.GetLastWriteTime(x) != p.ModifyDate)
                                                      ).ToList();
          addedFiles.ForEach(p =>
          {
            files.Add(p);
            FileSystemChanged(p, WatcherChangeTypes.Created);
          });

          excludedFiles.ForEach(p =>
          {
            files.Remove(files.First(x => x.Path == p.Path));
            FileSystemChanged(p, WatcherChangeTypes.Deleted);
          });

          modifiedFiles.ForEach(p =>
          {
            files.First(x => x.Path == p.Path).ModifyDate = File.GetLastWriteTime(p.Path);
            FileSystemChanged(p, WatcherChangeTypes.Changed);
          });
        }
        else
        {
          Console.WriteLine("Socket is paused!");
        }



        Thread.Sleep(1000);
      }
    }

    private void FileSystemChanged(FileModel model, WatcherChangeTypes type)
    {
      NotifyChange?.Invoke(new ChangedFileModel(model.Name, model.Path, type));
    }
  }
}