using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace RemoteTerminal.Model
{
    /// <summary>
    /// The data source for private keys.
    /// </summary>
    internal class PrivateKeysDataSource
    {
        /// <summary>
        /// An observable collection containing the private keys.
        /// </summary>
        private ObservableCollection<PrivateKeyData> privateKeys = new ObservableCollection<PrivateKeyData>();

        /// <summary>
        /// Gets the observable collection containing the private keys.
        /// </summary>
        public ObservableCollection<PrivateKeyData> PrivateKeys
        {
            get
            {
                return this.privateKeys;
            }
        }

        /// <summary>
        /// Reads all private keys from the local app data store.
        /// </summary>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        public async Task GetPrivateKeys()
        {
            var privateKeysFolder = await GetPrivateKeysFolder();
            var privateKeyFiles = await privateKeysFolder.GetFilesAsync(CommonFileQuery.OrderByName);

            foreach (var privateKeyFile in privateKeyFiles)
            {
                PrivateKeyData privateKeyData = new PrivateKeyData();
                privateKeyData.FileName = privateKeyFile.Name;
                privateKeyData.Data = (await FileIO.ReadBufferAsync(privateKeyFile)).ToArray();

                this.privateKeys.Add(privateKeyData);
            }
        }

        /// <summary>
        /// Reads the local "PrivateKeys" app data folder.
        /// </summary>
        /// <returns>The local "PrivateKeys" app data folder.</returns>
        public static async Task<StorageFolder> GetPrivateKeysFolder()
        {
            return await ApplicationData.Current.LocalFolder.CreateFolderAsync("PrivateKeys", CreationCollisionOption.OpenIfExists);
        }

        /// <summary>
        /// Adds or updates a private key.
        /// </summary>
        /// <param name="privateKeyData">The data of the private key to add/update.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        public async Task AddOrUpdate(PrivateKeyData privateKeyData)
        {
            var privateKeysFolder = await GetPrivateKeysFolder();
            var privateKeyFile = await privateKeysFolder.CreateFileAsync(privateKeyData.FileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBytesAsync(privateKeyFile, privateKeyData.Data);

            this.privateKeys.Remove(GetPrivateKey(privateKeyData.FileName));
            this.privateKeys.Add(privateKeyData);
        }

        /// <summary>
        /// Returns the private key that has the specified file name.
        /// </summary>
        /// <param name="fileName">The file name of the private key to return.</param>
        /// <returns>The found private key or null if there is none with the specified file name.</returns>
        public static PrivateKeyData GetPrivateKey(string fileName)
        {
            // Simple linear search is acceptable for small data sets
            var privateKeysDataSource = App.Current.Resources["privateKeysDataSource"] as PrivateKeysDataSource;

            var matches = privateKeysDataSource.PrivateKeys.Where((feed) => feed.FileName.Equals(fileName));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        /// <summary>
        /// Removes a private key.
        /// </summary>
        /// <param name="privateKeyData">The data of the private key to remove.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        internal async Task Remove(PrivateKeyData privateKeyData)
        {
            var privateKeysFolder = await GetPrivateKeysFolder();
            var privateKeyFile = await privateKeysFolder.GetFileAsync(privateKeyData.FileName);
            await privateKeyFile.DeleteAsync();

            this.privateKeys.Remove(GetPrivateKey(privateKeyData.FileName));
        }
    }
}
