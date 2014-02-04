﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using Windows.Storage;
using System.IO;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;

namespace FilterExplorer.Models
{
    public class PhotoLibraryModel
    {
        public static async Task<StorageFolder> PickPhotoFolderAsync()
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.ViewMode = PickerViewMode.Thumbnail;

            return await picker.PickSingleFolderAsync();
        }

        public static async Task<StorageFile> PickPhotoFileAsync()
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.ViewMode = PickerViewMode.Thumbnail;

            return await picker.PickSingleFileAsync();
        }

        public static async Task<List<FilteredPhotoModel>> GetPhotosFromFolderAsync(StorageFolder folder)
        {
            var list = new List<FilteredPhotoModel>();
            var files = await folder.GetFilesAsync();

            foreach (var file in files)
            {
                var properties = await file.GetBasicPropertiesAsync();

                if (properties.Size > 0 && file.ContentType == "image/jpeg")
                {
                    list.Add(new FilteredPhotoModel(file));
                }
            }

            return list;
        }

        public static async Task<StorageFile> SavePhotoAsync(FilteredPhotoModel photo)
        {
            var filenameFormat = new Windows.ApplicationModel.Resources.ResourceLoader().GetString("PhotoSaveFilenameFormat");
            var filename = String.Format(filenameFormat, DateTime.Now.ToString("yyyyMMddHHmmss"));

            var picker = new FileSavePicker();
            picker.SuggestedFileName = filename;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeChoices.Add(".jpg", new List<string>() { ".jpg" });

            var file = await picker.PickSaveFileAsync();

            if (file != null)
            {
                file = await SavePhotoAsync(photo, file);
            }

            return file;
        }

        internal static async Task<StorageFile> SaveTemporaryPhotoAsync(FilteredPhotoModel photo)
        {
            var filenameFormat = new Windows.ApplicationModel.Resources.ResourceLoader().GetString("PhotoSaveFilenameFormat");
            var filename = Application.Current.Resources["PhotoSaveTemporaryFilename"] as string;
            var folder = ApplicationData.Current.TemporaryFolder;
            var file = await folder.CreateFileAsync(filename, Windows.Storage.CreationCollisionOption.ReplaceExisting);

            return await SavePhotoAsync(photo, file);
        }

        private static async Task<StorageFile> SavePhotoAsync(FilteredPhotoModel photo, StorageFile file)
        {
            CachedFileManager.DeferUpdates(file);

            using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
            using (var photoStream = await photo.GetFilteredPhotoAsync())
            using (var reader = new DataReader(photoStream))
            using (var writer = new DataWriter(fileStream))
            {
                await reader.LoadAsync((uint)photoStream.Size);
                var buffer = reader.ReadBuffer((uint)photoStream.Size);

                writer.WriteBuffer(buffer);
                await writer.StoreAsync();
                await writer.FlushAsync();
            }

            var status = await CachedFileManager.CompleteUpdatesAsync(file);

            if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
            {
                return file;
            }
            else
            {

                return null;
            }
        }
    }
}
