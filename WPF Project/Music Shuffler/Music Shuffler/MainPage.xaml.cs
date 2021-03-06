﻿using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace Music_Shuffler {
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page {
        Playlist playlist;
        List<String> musicFileExtensions = new List<string>();

        public MainPage() {
            InitializeComponent();
            //Music_Shuffler.Properties.Settings.Default.Reset();
            txtExtensions.Text = Music_Shuffler.Properties.Settings.Default.extensions;
            txtOutputFolder.Text = Music_Shuffler.Properties.Settings.Default.output_folder;
            
        }

        /// <summary>
        /// Actually copies the music files
        /// </summary>
        public void btnMakePlaylistClicked(object sender, RoutedEventArgs ev) {
            //Make sure the directory is all G
            String outputFolder = txtOutputFolder.Text;
            if (outputFolder == "") {
                MessageBox.Show("Output Folder required", "Unable to make playlist");
                return;
            }
            if (!Directory.Exists(outputFolder)) {
                Directory.CreateDirectory(outputFolder);
            } else {
                if (Directory.GetFiles(outputFolder).Any(file => !musicFileExtensions.Contains(Path.GetExtension(file)))) {
                    MessageBox.Show("I will only clobber a folder if there are nothing but music files inside it.", "Music Shuffler");
                    return;
                } else {
                    Directory.Delete(outputFolder, recursive: true);
                    Directory.CreateDirectory(outputFolder);
                }
            }

            //Weed out the albums that aren't ticked to be included and randomise/sort the rest
            List<Album> albumsToInclude = new List<Album>();
            foreach (ListBoxItem albumItem in lstbxAlbums.Items) {
                Album album = albumItem.Tag as Album;

                //won't add unless combo is enabled
                if ((bool)(((Grid)albumItem.Content).Children[0] as ComboBox).IsEnabled) {
                    //check output/input collisions
                    if (Path.GetFullPath(album.albumRoot) == Path.GetFullPath(outputFolder)){
                        MessageBox.Show("The output directory cannot be an input album:\n" + 
                                         Path.GetFullPath(album.albumRoot), "Message Shuffler");
                        return;
                    }

                    //add album
                    albumsToInclude.Add(album);
                    album.randomiseSongs = (bool)(((Grid)albumItem.Content).Children[1] as CheckBox).IsChecked;
                    album.cursorIndex = (((Grid)albumItem.Content).Children[0] as ComboBox).SelectedIndex;
                }

            }

            if (albumsToInclude.Count == 0){
                MessageBox.Show("No albums selected!", "Message Shuffler");
                return;
            } 

            playlist.generatePlaylist(albumsToInclude, outputFolder);
            Music_Shuffler.Properties.Settings.Default.output_folder = txtOutputFolder.Text;
            Music_Shuffler.Properties.Settings.Default.Save();
        }


        /// <summary>
        /// Opens folder dialogue to assist in choosing an output folder
        /// </summary>
        public void btnChooseOutputClicked(object sender, RoutedEventArgs ev) {
            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog()) {
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                    txtOutputFolder.Text = dialog.FileName;
                }
            }
        }


        public void btnOpenClicked(object sender, RoutedEventArgs ev) {
            System.Diagnostics.Process.Start("explorer.exe", txtOutputFolder.Text);
        }

        /***********************************************************************************************/
        /*       Select All / Select None         and         Shuffle All / Shuffle None               */
        /***********************************************************************************************/
        public void chkSelectAllClicked(object sender, RoutedEventArgs ev) {
            foreach (ListBoxItem albumItem in lstbxAlbums.Items) {
                ComboBox box = (((Grid)albumItem.Content).Children[0] as ComboBox);
                box.IsEnabled = (bool)chkSelectAll.IsChecked;
                if (!(bool)box.IsEnabled) {
                    box.Foreground = Brushes.Gray;
                } else {
                    box.Foreground = Brushes.Black;
                }
            }
        }
        public void chkShuffleAllClicked(object sender, RoutedEventArgs ev) {
            foreach (ListBoxItem albumItem in lstbxAlbums.Items) {
                (((Grid)albumItem.Content).Children[1] as CheckBox).IsChecked = (bool)chkShuffleAll.IsChecked;
            }
        }
        /***********************************************************************************************/
        /***********************************************************************************************/


        /// <summary>
        /// Retrieves albums from root folder and calls populateGUIAlbums to populate gui
        /// </summary>
        public void btnGetAlbumsClicked(object sender, RoutedEventArgs ev) {
            //make extension list
            musicFileExtensions.Clear();
            String[] temp = txtExtensions.Text.Split(',', ';', '|');
            foreach (String ext in temp){
                musicFileExtensions.Add("." + ext.Trim());
            }


            List<String> roots = new List<string>();

            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog()) {
                dialog.IsFolderPicker = true;
                dialog.Multiselect = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                    roots = dialog.FileNames.ToList();
                }
            }
            if (roots.Count == 0) {
                return;
            }

            //generates albums
            playlist = new Playlist(roots, musicFileExtensions);

            this.clearGUIAlbums();
            this.populateGUIAlbums();
            Music_Shuffler.Properties.Settings.Default.extensions = txtExtensions.Text;
            Music_Shuffler.Properties.Settings.Default.Save();
        }

        public void clearGUIAlbums() {
            lstbxAlbums.Items.Clear();
        }


        /// <summary>
        /// creates a GUI element for each album and populates 
        /// the list box lstbxAlbums with them. 
        /// Also adds the album object as a tag to the listboxitem
        /// </summary>
        public void populateGUIAlbums() {
            foreach (Album album in playlist.albums) {
                //make combo box for each album
                ComboBox albumCombo = new ComboBox();
                albumCombo.Style = (Style)Resources["styleComboBox"];
                albumCombo.Items.Add(Path.GetFileName(album.albumRoot));
                foreach (String songPath in album.albumSongs) {
                    albumCombo.Items.Add(Path.GetFileName(songPath));
                }
                albumCombo.SelectedIndex = 0;

                albumCombo.ToolTip = album.albumRoot;

                CheckBox shuffleAlbum = new CheckBox();
                shuffleAlbum.Click += (o, e) => {
                    if (!(bool)shuffleAlbum.IsChecked) {
                        chkShuffleAll.IsChecked = false;
                    }
                };
                shuffleAlbum.Content = "Shuffle";
                shuffleAlbum.HorizontalAlignment = HorizontalAlignment.Right;
                shuffleAlbum.Style = (Style)Resources["styleCheckbox2"];


                //shuffle checkbox will not be enabled when the album is not being included
                Binding shuffletoIncludeAlbumBinding = new Binding();
                shuffletoIncludeAlbumBinding.Source = albumCombo;
                shuffletoIncludeAlbumBinding.Path = new PropertyPath("IsEnabled");
                BindingOperations.SetBinding(shuffleAlbum, CheckBox.IsEnabledProperty, shuffletoIncludeAlbumBinding);

                //Make grid to hold checkbox and combo
                Grid albumGrid = new Grid();
                ColumnDefinition c1 = new ColumnDefinition();
                c1.Width = new GridLength(3, GridUnitType.Star);
                ColumnDefinition c2 = new ColumnDefinition();
                c2.Width = new GridLength(2, GridUnitType.Star);
                albumGrid.ColumnDefinitions.Add(c1);
                albumGrid.ColumnDefinitions.Add(c2);
                //add the two checkboxes to the grid
                Grid.SetColumn(albumCombo, 0);
                albumGrid.Children.Add(albumCombo);
                Grid.SetColumn(shuffleAlbum, 1);
                albumGrid.Children.Add(shuffleAlbum);

                //Make ListBoxItem to hold grid
                ListBoxItem albumItem = new ListBoxItem();
                albumItem.Content = albumGrid;
                albumItem.Tag = album;
                albumItem.MouseRightButtonUp += (o, e) => {
                    if ((bool)albumCombo.IsEnabled) {
                       albumCombo.IsEnabled = false;
                       albumCombo.Foreground = Brushes.Gray;
                       chkSelectAll.IsChecked = false;
                    } else {
                        albumCombo.IsEnabled = true;
                        albumCombo.Foreground = Brushes.Black;
                    }
                };

                //add ListBoxItem to ListBox
                lstbxAlbums.Items.Add(albumItem);
            }
        }
    }
}
