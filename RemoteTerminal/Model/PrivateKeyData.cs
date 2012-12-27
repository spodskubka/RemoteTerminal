using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI;

namespace RemoteTerminal.Model
{
    public class PrivateKeyData
    {
        public string FileName { get; set; }
        public byte[] Data { get; set; }
        public int LinkedFavorites
        {
            get
            {
                var favoritesDataSource = App.Current.Resources["favoritesDataSource"] as FavoritesDataSource;
                return (from favorite in favoritesDataSource.Favorites
                        where favorite.Authentication == AuthenticationType.PrivateKey
                        where favorite.PrivateKeyName == this.FileName
                        select favorite).Count();
            }
        }
    }
}
