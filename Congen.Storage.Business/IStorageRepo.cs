namespace Congen.Storage.Business
{
    public interface IStorageRepo
    {
        public string SaveFile(Stream file, string extension);
    }
}