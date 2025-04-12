namespace Congen.Storage.Business
{
    public interface IStorageRepo
    {
        public string SaveFile(string container, Stream file, string extension);
    }
}