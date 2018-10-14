namespace WPecanTests.Contracts
{
    public interface IFile
    {
        string FileName { set; get; }

        string FileDescription { set; get; }

        string FilePath { set; get; }

        string FileContent { set; get; }
    }
}