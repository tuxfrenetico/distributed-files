using System;
using System.IO;
using System.Text;

namespace SharedFiles.Models
{
  [Serializable]
  public class ChangedFileModel
  {
    public string Name { get; set; }
    public string Path { get; set; }
    public byte[] File { get; set; }
    public long Size { get; set; }
    public WatcherChangeTypes Type { get; set; }
    public string TypeDescription
    {
      get
      {
        switch (Type)
        {
          case WatcherChangeTypes.Created:
            return "Criado";
          case WatcherChangeTypes.Deleted:
            return "Deletado";
          case WatcherChangeTypes.Changed:
            return "Modificado";
          default:
            return "Tipo de alteração desconhecida";
        }
      }
    }
    public ChangedFileModel() { }
    public ChangedFileModel(string name, string path, WatcherChangeTypes type)
    {
      this.Name = name;
      this.Path = path;
      this.Type = type;
    }

    public byte[] ToStringBytes() => Encoding.ASCII.GetBytes(ToString());

    public override string ToString() => $"{Name};{Path};{Type.GetHashCode()};{Size};";
  }
}