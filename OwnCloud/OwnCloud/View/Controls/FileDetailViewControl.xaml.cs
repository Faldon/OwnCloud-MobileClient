using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Animation;
using OwnCloud.Data;

namespace OwnCloud.View.Controls
{
    public partial class FileDetailViewControl : UserControl
    {
        public FileDetailViewControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Holds the File-Properties only available if DataContext was set properly.
        /// </summary>
        public File FileProperties
        {
            get
            {
                return (File)DataContext;
            }
        }
    }
}
