using System;

namespace SharedFiles.Models
{
  public class FileModel
  {
    public string Name { get; set; }
    public string Path { get; set; }
    public DateTime ModifyDate { get; set; }
  }
}