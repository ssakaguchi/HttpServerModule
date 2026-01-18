namespace HttpServerWPF.FileDialogService
{
    public interface IOpenFolderDialogService
    {
        public string Title { get; set; }
        public string FolderName { get; set; }

        public bool? OpenFolderDialog();
    }
}
