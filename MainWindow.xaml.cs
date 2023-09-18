using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace UnityGames
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string steamDirectoryPath = "";
        public MainWindow()
        {
            InitializeComponent();

            var steamDirectory = (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam", "SteamPath", "-");
            steamDirectoryPath = steamDirectory;

            if (steamDirectoryPath == "-")
            {
                unityGameCountLabel.Content = "You seem to not have Steam installed on your system?";
                unitySearchButton.Visibility = Visibility.Hidden;
                return;
            }

        }

        //- Actions

        private async void unitySearchButton_Click(object sender, RoutedEventArgs e)
        {
            var previousText = unitySearchButton.Content;
            unitySearchButton.IsEnabled = false;
            unitySearchButton.Content = "Searching for Unity games";

            var unityGames = await listOfSteamGamesMadeWithUnity();

            foreach (var directory in unityGames)
            {
                var dirInfo = new DirectoryInfo(directory);
                unityGamesList.Items.Add(dirInfo);
            }

            var sCount = unityGames.Count == 1 ? "" : "s";
            unityGameCountLabel.Content = string.Format("You seem to have {0} Unity game{1} installed", unityGames.Count, sCount);

            unitySearchButton.IsEnabled = true;
            unitySearchButton.Content = previousText;
        }

        //- Private

        private Task<List<string>> listOfSteamGamesMadeWithUnity()
        {
            return Task.Run<List<string>>(() =>
            {
                var libraryConfigPath = string.Format("{0}/config/libraryfolders.vdf", steamDirectoryPath).Replace("/", Path.DirectorySeparatorChar.ToString());
                var libraryConfigText = File.ReadAllLines(libraryConfigPath);

                List<string> libraries = new List<string>();
                foreach (var line in libraryConfigText)
                {
                    if (line.Contains("\"path\""))
                    {
                        var regexp = new Regex("\"path\"[^\"]+\"([^\"]+)\"");
                        var match = regexp.Match(line);
                        if (match.Success && match.Groups.Count == 2)
                        {
                            libraries.Add(match.Groups[1].Value.Replace("\\\\", Path.DirectorySeparatorChar.ToString()));
                        }
                    }
                }

                var gameFolders = new List<string>();
                foreach (var dir in libraries)
                {
                    gameFolders.AddRange(Directory.GetDirectories(String.Format("{0}\\steamapps\\common\\", dir)));
                }

                var unityGames = new List<string>();
                foreach (var dir in gameFolders)
                {
                    bool isUnityGame = false;
                    var files = Directory.GetFiles(dir);
                    foreach (var file in files)
                    {
                        if (file.Contains("UnityCrashHandler32") || file.Contains("UnityPlayer"))
                        {
                            isUnityGame = true;
                            break;
                        }
                    }

                    if (isUnityGame)
                    {
                        unityGames.Add(dir);
                    }
                }

                return unityGames;
            });
        }
    }
}
