using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;

namespace Nextcloud.Data
{
    [Table("Files")]
    public class File : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int FileId { get; set; }

        [NotNull]
        public string Filename { get; set; }

        [NotNull]
        public string Filepath { get; set; }

        [Ignore]
        public string ParentFilepath
        {
            get
            {
                string p = Filepath.TrimEnd('/');
                return p.Substring(0, p.LastIndexOf('/')) + '/';
            }
        }

        public long Filesize { get; set; }

        public string Filetype { get; set; }

        [NotNull]
        public bool IsDirectory { get; set; }

        [NotNull]
        public bool IsRootItem { get; set; }

        public string ETag { get; set; }

        public DateTime FileLastModified { get; set; }

        public DateTime FileCreated { get; set; }

        [ForeignKey(typeof(Account))]
        public int AccountId { get; set; }

        [ManyToOne]
        public Account Account { get; set; }

    }
}
