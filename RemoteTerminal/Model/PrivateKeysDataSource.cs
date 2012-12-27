using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Popups;
using System.Runtime.InteropServices.WindowsRuntime;

namespace RemoteTerminal.Model
{
    internal class PrivateKeysDataSource
    {
        private ObservableCollection<PrivateKeyData> privateKeys = new ObservableCollection<PrivateKeyData>();
        public ObservableCollection<PrivateKeyData> PrivateKeys
        {
            get
            {
                return this.privateKeys;
            }
        }

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

        public static async Task<StorageFolder> GetPrivateKeysFolder()
        {
            return await ApplicationData.Current.LocalFolder.CreateFolderAsync("PrivateKeys", CreationCollisionOption.OpenIfExists);
        }

        public async Task AddOrUpdate(PrivateKeyData privateKeyData)
        {
            var privateKeysFolder = await GetPrivateKeysFolder();
            var privateKeyFile = await privateKeysFolder.CreateFileAsync(privateKeyData.FileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBytesAsync(privateKeyFile, privateKeyData.Data);

            this.privateKeys.Remove(GetPrivateKey(privateKeyData.FileName));
            this.privateKeys.Add(privateKeyData);
        }

        // Returns the privateKey that has the specified id.
        public static PrivateKeyData GetPrivateKey(string fileName)
        {
            // Simple linear search is acceptable for small data sets
            var privateKeysDataSource = App.Current.Resources["privateKeysDataSource"] as PrivateKeysDataSource;

            var matches = privateKeysDataSource.PrivateKeys.Where((feed) => feed.FileName.Equals(fileName));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        internal async Task Remove(PrivateKeyData privateKeyData)
        {
            var privateKeysFolder = await GetPrivateKeysFolder();
            var privateKeyFile = await privateKeysFolder.GetFileAsync(privateKeyData.FileName);
            await privateKeyFile.DeleteAsync();

            this.privateKeys.Remove(GetPrivateKey(privateKeyData.FileName));
        }
    }
}
