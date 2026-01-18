using Microsoft.Win32;

namespace HttpServerWPF.FileDialogService
{
    public class OpenFolderDialogService : IOpenFolderDialogService
    {
        private OpenFolderDialog _openFolderDialog = new();

        public string Title
        {
            get { return _openFolderDialog.Title; }
            set { _openFolderDialog.Title = value; }
        }

        public string FolderName
        {
            get { return _openFolderDialog.FolderName; }
            set { _openFolderDialog.FolderName = value; }
        }

        public bool? OpenFolderDialog()
        {
            return _openFolderDialog.ShowDialog();
        }
    }
}
