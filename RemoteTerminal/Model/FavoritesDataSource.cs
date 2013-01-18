using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;

namespace RemoteTerminal.Model
{
    internal class FavoritesDataSource
    {
        private ObservableCollection<ConnectionData> favorites = new ObservableCollection<ConnectionData>();
        public ObservableCollection<ConnectionData> Favorites
        {
            get
            {
                return this.favorites;
            }
        }

        public void GetFavorites()
        {
            var favorites = GetFavoritesSettings();
            foreach (var favorite in favorites)
            {
                ConnectionData connectionData = new ConnectionData();

                try
                {
                    string favoriteJsonString = (string)favorite.Value;
                    JsonObject jsonObject = JsonObject.Parse(favoriteJsonString);

                    connectionData.Id = favorite.Key;
                    connectionData.Name = jsonObject.GetNamedString("Name");
                    ConnectionType connectionType;
                    Enum.TryParse<ConnectionType>(jsonObject.GetNamedString("Type"), out connectionType);
                    connectionData.Type = connectionType;
                    connectionData.Host = jsonObject.GetNamedString("Host");
                    connectionData.Port = (int)jsonObject.GetNamedNumber("Port");
                    connectionData.Username = jsonObject.GetNamedString("Username");
                    AuthenticationType authenticationMethod;
                    Enum.TryParse<AuthenticationType>(jsonObject.GetNamedString("Authentication"), out authenticationMethod);
                    connectionData.Authentication = authenticationMethod;
                    connectionData.PrivateKeyName = jsonObject.GetNamedString("PrivateKeyName");
                    connectionData.PrivateKeyAgentForwarding = jsonObject.ContainsKey("PrivateKeyAgentForwarding") ? jsonObject.GetNamedBoolean("PrivateKeyAgentForwarding") : false;
                }
                catch (Exception)
                {
                    // A favorite seems to contain invalid data, ignore it, don't delete it.
                    // Maybe a future update is able to read the data.
                    continue;
                }

                this.favorites.Add(connectionData);
            }
        }

        private static IPropertySet GetFavoritesSettings()
        {
            var favoritesContainer = ApplicationData.Current.LocalSettings.CreateContainer("Favorites", ApplicationDataCreateDisposition.Always);
            return favoritesContainer.Values;
        }

        public void AddOrUpdate(ConnectionData connectionData)
        {
            var favorites = GetFavoritesSettings();

            if (connectionData.Id == null)
            {
                connectionData.Id = Guid.NewGuid().ToString();
            }

            JsonObject jsonObject = new JsonObject();
            jsonObject.Add("Name", JsonValue.CreateStringValue(connectionData.Name));
            jsonObject.Add("Type", JsonValue.CreateStringValue(connectionData.Type.ToString()));
            jsonObject.Add("Host", JsonValue.CreateStringValue(connectionData.Host));
            jsonObject.Add("Port", JsonValue.CreateNumberValue((double)connectionData.Port));
            jsonObject.Add("Username", JsonValue.CreateStringValue(connectionData.Username));
            jsonObject.Add("Authentication", JsonValue.CreateStringValue(connectionData.Authentication.ToString()));
            jsonObject.Add("PrivateKeyName", JsonValue.CreateStringValue(connectionData.PrivateKeyName));
            jsonObject.Add("PrivateKeyAgentForwarding", JsonValue.CreateBooleanValue(connectionData.PrivateKeyAgentForwarding));
            string favoriteJsonString = jsonObject.Stringify();

            favorites[connectionData.Id] = favoriteJsonString;

            this.favorites.Remove(GetFavorite(connectionData.Id));
            this.favorites.Add(connectionData);
        }

        // Returns the favorite that has the specified id.
        public static ConnectionData GetFavorite(string id)
        {
            // Simple linear search is acceptable for small data sets
            var favoritesDataSource = App.Current.Resources["favoritesDataSource"] as FavoritesDataSource;

            var matches = favoritesDataSource.Favorites.Where(favorite => favorite.Id.Equals(id));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        internal void Remove(ConnectionData connectionData)
        {
            var favorites = GetFavoritesSettings();

            favorites.Remove(connectionData.Id);

            this.favorites.Remove(GetFavorite(connectionData.Id));
        }
    }
}
