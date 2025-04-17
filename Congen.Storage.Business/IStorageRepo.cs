namespace Congen.Storage.Business
{
    public interface IStorageRepo
    {
        public string SaveFile(string container, Stream file, string extension);

        public Stream[] GetFiles(string container, string[] fileNames);

        public Stream GetFile(string container, string fileName);
    }
}