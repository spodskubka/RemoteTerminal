// Remote Terminal, an SSH/Telnet terminal emulator for Microsoft Windows
// Copyright (C) 2012-2015 Stefan Podskubka
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.IO;
using System.Reflection;
using System.Text;
using Windows.Storage;

namespace RemoteTerminal
{
    public static class ChangelogManager
    {
        /// <summary>
        /// The version of the most current changelog.
        /// </summary>
        private const string CurrentVersion = "1.9.2";

        /// <summary>
        /// The name of the setting containing the last read changelog version.
        /// </summary>
        private const string LastReadChangelogSettingName = "LastReadChangelog";

        /// <summary>
        /// This array contains, in reverse order, all previous versions of the app for which a changelog is available.
        /// </summary>
        /// <remarks><see cref="CurrentVersion"/> must not be included in this list.</remarks>
        private static readonly string[] PreviousVersions = new[]
        {
            "1.9.1",
            "1.9.0",
            "1.8.2",
        };

        /// <summary>
        /// Determines whether a changelog should be displayed at application start.
        /// </summary>
        /// <returns>A value indicating whether a changelog should be displayed at application start.</returns>
        public static bool ShouldDisplayChangelog()
        {
            string lastReadChangelog = ApplicationData.Current.LocalSettings.Values[LastReadChangelogSettingName] as string;
            return lastReadChangelog != CurrentVersion;
        }

        /// <summary>
        /// Produces a HTML changelog.
        /// </summary>
        /// <returns>The changelog in HTML format.</returns>
        /// <remarks>
        /// The return value contains the changelogs from all versions between the last read changelog and the changelog of the current version.
        /// If all changelogs have already been read the changelog of the current version is returned.
        /// </remarks>
        public static string ProduceChangelog()
        {
            string lastReadChangelog = ApplicationData.Current.LocalSettings.Values[LastReadChangelogSettingName] as string;

            StringBuilder changelog = new StringBuilder();

            changelog.Append(ReadHtmlFile("head")
                .Replace("{CurrentVersion}", CurrentVersion));

            changelog.Append(ReadHtmlFile(CurrentVersion));
            if (CurrentVersion != lastReadChangelog)
            {
                foreach (string version in PreviousVersions)
                {
                    if (version == lastReadChangelog)
                    {
                        break;
                    }

                    changelog.Append(ReadHtmlFile(version));
                }
            }

            changelog.Append(ReadHtmlFile("tail"));

            return changelog.ToString();
        }

        /// <summary>
        /// Confirms that the user has read the displayed changelog.
        /// </summary>
        public static void ConfirmRead()
        {
            ApplicationData.Current.LocalSettings.Values[LastReadChangelogSettingName] = CurrentVersion;
        }

        /// <summary>
        /// Reads the specified changelog HTML file.
        /// </summary>
        /// <param name="name">The name of the changelog HTML file to read.</param>
        /// <returns>The read HTML file.</returns>
        private static string ReadHtmlFile(string name)
        {
            using (Stream changelogStream = typeof(App).GetTypeInfo().Assembly.GetManifestResourceStream("RemoteTerminal.Changelogs." + name + ".htm"))
            using (StreamReader changelogStreamReader = new StreamReader(changelogStream))
            {
                return changelogStreamReader.ReadToEnd();
            }
        }
    }
}
