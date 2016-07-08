using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;

namespace Nextcloud.Data
{
    [Table("Files")]
    class File : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int FileId { get; set; }

        [NotNull]
        public string Filename { get; set; }

        [NotNull]
        public string Filepath { get; set; }

        public long Filesize { get; set; }

        public string FileType { get; set; }

        [NotNull]
        public bool IsDirectory { get; set; }

        public string ETag { get; set; }
    }
}
