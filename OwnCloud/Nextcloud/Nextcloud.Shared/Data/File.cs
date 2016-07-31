using System;
using System.Collections.Generic;
using System.Text;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;
using Windows.ApplicationModel.Resources;
using Windows.Storage;

namespace Nextcloud.Data
{
    [Table("Files")]
    public class File : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int? FileId { get; set; }

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

        [NotNull]
        public bool IsDownloaded { get; set; }

        public string ETag { get; set; }

        public DateTime FileLastModified { get; set; }

        public DateTime FileCreated { get; set; }

        [ForeignKey(typeof(Account))]
        public int AccountId { get; set; }

        [ManyToOne]
        public Account Account { get; set; }

        static Dictionary<string, string> _iconCache;

        [Ignore]
        public string FileIcon
        {
            get
            {
                if (IsDirectory) {
                    if (IsRootItem) {
                        return _iconCache["up"];
                    } else {
                        return _iconCache["folder"];
                    }
                } else {
                    if (_iconCache.ContainsKey(Filetype.Replace("/", "_"))) {
                        return _iconCache[Filetype.Replace("/", "_")];
                    }
                    if (_iconCache.ContainsKey(Filetype.Split('/')[0])) {
                        return _iconCache[Filetype.Split('/')[0]];
                    }
                    return _iconCache["file"];
                }
            }
        }

        [Ignore]
        public string Size
        {
            get
            {
                if(!IsDirectory) {
                    return Utility.FormatBytes(Filesize);
                } else {
                    return "";
                } 
            }
        }

        public static async void FillIconCache() {
            _iconCache = _iconCache ?? new Dictionary<string, string>();
            StorageFolder installationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            StorageFolder assetsFolder = await installationFolder.GetFolderAsync("Assets");
            StorageFolder fileIconsFolder = await assetsFolder.GetFolderAsync("FileIcons");
#if WINDOWS_PHONE_APP
            string theme = App.Current.RequestedTheme.ToString();
            fileIconsFolder = await fileIconsFolder.GetFolderAsync(theme);
#endif
            IReadOnlyList<StorageFile> fileIcons = await fileIconsFolder.GetFilesAsync();
            foreach(StorageFile fileIcon in fileIcons) {
                if(!_iconCache.ContainsKey(fileIcon.DisplayName)) {
#if WINDOWS_PHONE_APP
                    _iconCache.Add(fileIcon.DisplayName, "/Assets/FileIcons/" + theme + "/" + fileIcon.DisplayName + fileIcon.FileType);
#else
                    _iconCache.Add(fileIcon.DisplayName, "/Assets/FileIcons/" + fileIcon.DisplayName + fileIcon.FileType);
#endif
                }
            }
        }
    }
}
