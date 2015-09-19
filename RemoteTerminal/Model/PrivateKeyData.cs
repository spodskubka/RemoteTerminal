using System.Linq;

namespace RemoteTerminal.Model
{
    /// <summary>
    /// Data model class for storing private key data.
    /// </summary>
    public class PrivateKeyData
    {
        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the actual private key data.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Gets the number of favorites that use this private key (displayed on the PrivateKeysPage).
        /// </summary>
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
