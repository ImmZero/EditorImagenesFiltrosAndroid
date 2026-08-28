using SkiaSharp.Views.Maui;
using SkiaSharp;


namespace EditorImagenes
{

    public partial class MainPage : ContentPage
    {
        SKBitmap modifiedBitmap;
        SKBitmap originalBitmap;
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnSelectImageClicked(object sender, EventArgs e)
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Por favor selecciona una imagen",
                FileTypes = FilePickerFileType.Images,
            });

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                memoryStream.Position = 0;

                originalBitmap = SKBitmap.Decode(memoryStream);
                modifiedBitmap = originalBitmap.Copy(); // Inicializa modifiedBitmap con la imagen original

                selectedImage.Source = ImageSource.FromStream(() => new MemoryStream(memoryStream.ToArray()));

                colorFilterPicker.SelectedIndex = -1;
            }
        }

        public SKBitmap Erode(SKBitmap originalBitmap)
        {
            var width = originalBitmap.Width;
            var height = originalBitmap.Height;
            var erodedBitmap = new SKBitmap(width, height);

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    var pixelColor = originalBitmap.GetPixel(x, y);
                    bool isDarkerPixelFound = false;

                    // Revisar los vecinos
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue; // Saltar el píxel central

                            var neighborColor = originalBitmap.GetPixel(x + dx, y + dy);
                            if (neighborColor.Red < pixelColor.Red ||
                                neighborColor.Green < pixelColor.Green ||
                                neighborColor.Blue < pixelColor.Blue)
                            {
                                isDarkerPixelFound = true;
                                break;
                            }
                        }
                        if (isDarkerPixelFound) break;
                    }

                    erodedBitmap.SetPixel(x, y, isDarkerPixelFound ? SKColors.Black : pixelColor);
                }
            }

            return erodedBitmap;
        }


        public SKBitmap Dilate(SKBitmap originalBitmap)
        {
            var width = originalBitmap.Width;
            var height = originalBitmap.Height;
            var dilatedBitmap = new SKBitmap(width, height);

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    var pixelColor = originalBitmap.GetPixel(x, y);
                    bool isBrighterPixelFound = false;

                    // Revisar los vecinos
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue; // Saltar el píxel central

                            var neighborColor = originalBitmap.GetPixel(x + dx, y + dy);
                            if (neighborColor.Red > pixelColor.Red ||
                                neighborColor.Green > pixelColor.Green ||
                                neighborColor.Blue > pixelColor.Blue)
                            {
                                isBrighterPixelFound = true;
                                break;
                            }
                        }
                        if (isBrighterPixelFound) break;
                    }

                    dilatedBitmap.SetPixel(x, y, isBrighterPixelFound ? SKColors.White : pixelColor);
                }
            }

            return dilatedBitmap;
        }


        private SKBitmap OpenImage(SKBitmap originalBitmap)
        {
            var eroded = Erode(originalBitmap);
            var opened = Dilate(eroded);
            eroded.Dispose();  // Liberar recursos de la imagen erosionada
            return opened;
        }
        private void ApplyColorFilter(ColorFilterType filterType)
        {
            if (originalBitmap == null) return;

            var newBitmap = new SKBitmap(originalBitmap.Width, originalBitmap.Height);

            using (var canvas = new SKCanvas(newBitmap))
            {
                var paint = new SKPaint
                {
                    ColorFilter = GetColorFilter(filterType)
                };
                canvas.DrawBitmap(originalBitmap, 0, 0, paint);
            }

            using var image = SKImage.FromBitmap(newBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            selectedImage.Source = ImageSource.FromStream(() => new MemoryStream(newBitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray()));

            // Guardar la imagen modificada
            modifiedBitmap = newBitmap;
        }


        private SKColorFilter GetColorFilter(ColorFilterType filterType)
        {
            switch (filterType)
            {
                case ColorFilterType.Gray:
                    return SKColorFilter.CreateColorMatrix(new float[]
                    {
                        0.299f, 0.587f, 0.114f, 0, 0,
                        0.299f, 0.587f, 0.114f, 0, 0,
                        0.299f, 0.587f, 0.114f, 0, 0,
                        0,      0,      0,      1, 0,
                    });
                case ColorFilterType.Red:
                    // Define aquí el filtro rojo
                    return SKColorFilter.CreateColorMatrix(new float[]
                    {
                        1, 0, 0, 0, 0,
                        0, 0, 0, 0, 0,
                        0, 0, 0, 0, 0,
                        0, 0, 0, 1, 0,
                    });

                case ColorFilterType.Green:
                    // Define aquí el filtro verde
                    return SKColorFilter.CreateColorMatrix(new float[]
                    {
                        0, 0, 0, 0, 0,
                        0, 1, 0, 0, 0,
                        0, 0, 0, 0, 0,
                        0, 0, 0, 1, 0,
                    });
                case ColorFilterType.Blue:
                    // Define aquí el filtro azul
                    return SKColorFilter.CreateColorMatrix(new float[]
                    {
                        0, 0, 0, 0, 0,
                        0, 0, 0, 0, 0,
                        0, 0, 1, 0, 0,
                        0, 0, 0, 1, 0,
                    });
            }

            return null;
        }

        private void OnFilterSelected(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            var selectedItem = (string)picker.SelectedItem;

            switch (selectedItem)
            {
                case "Original":
                    RestoreOriginalImage();
                    ClearStatusLabel();

                    break;
                case "Gris":
                    ApplyColorFilter(ColorFilterType.Gray);
                    ClearStatusLabel();
                    break;
                case "Rojo":
                    ApplyColorFilter(ColorFilterType.Red);
                    ClearStatusLabel();
                    break;
                case "Verde":
                    ApplyColorFilter(ColorFilterType.Green);
                    ClearStatusLabel();
                    break;
                case "Azul":
                    ApplyColorFilter(ColorFilterType.Blue);
                    ClearStatusLabel();
                    break;
                case "Apertura":
                    ApplyOpeningFilter();
                    break;
                default:
                    RestoreOriginalImage();
                    ClearStatusLabel();
                    break;
            }
        }

        private void ClearStatusLabel()
        {
            statusLabel.Text = ""; // Limpia el mensaje del label
        }


        private async void ApplyOpeningFilter()
        {
            if (originalBitmap == null) return;
            statusLabel.Text = "Cargando..."; // Mostrar mensaje de carga
            await Task.Run(() =>
            {
                // Redimensionar la imagen para mejorar el rendimiento
                var resizedBitmap = ResizeImage(originalBitmap, 480, 320); // Ajusta estos valores según tus necesidades
                var openedBitmap = OpenImage(resizedBitmap);

                Dispatcher.Dispatch(() =>
                {
                    using var image = SKImage.FromBitmap(openedBitmap);
                    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                    selectedImage.Source = ImageSource.FromStream(() => data.AsStream());

                    // Actualizar modifiedBitmap con el resultado del filtro de apertura
                    modifiedBitmap = openedBitmap.Copy();

                    openedBitmap.Dispose();
                    resizedBitmap.Dispose();
                    statusLabel.Text = "Listo!"; // Cambiar a mensaje de completado
                });
            });
        }




        private void RestoreOriginalImage()
        {
            // Restaura la imagen original
            if (originalBitmap != null)
            {
                using var image = SKImage.FromBitmap(originalBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                selectedImage.Source = ImageSource.FromStream(() => data.AsStream());

                modifiedBitmap = originalBitmap.Copy(); // Restaura modifiedBitmap a la imagen original
            }
        }
        private void OnExitButtonClicked(object sender, EventArgs e)
        {
            // Cierra la aplicación
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }


        private void OnPickerButtonClicked(object sender, EventArgs e)
        {
            colorFilterPicker.Focus(); // Esto activará el Picker
        }
        public SKBitmap ResizeImage(SKBitmap originalBitmap, int maxWidth, int maxHeight)
        {
            float scale = Math.Min((float)maxWidth / originalBitmap.Width, (float)maxHeight / originalBitmap.Height);
            int newWidth = (int)(originalBitmap.Width * scale);
            int newHeight = (int)(originalBitmap.Height * scale);

            var resizedBitmap = new SKBitmap(newWidth, newHeight);

            using (var canvas = new SKCanvas(resizedBitmap))
            {
                var resizePaint = new SKPaint
                {
                    FilterQuality = SKFilterQuality.High // Esto mejora la calidad de la imagen redimensionada
                };
                canvas.DrawBitmap(originalBitmap, new SKRect(0, 0, newWidth, newHeight), resizePaint);
            }

            return resizedBitmap;
        }

        private async void OnShareImageButtonClicked(object sender, EventArgs e)
        {
            if (modifiedBitmap == null)
            {
                await DisplayAlert("Error", "No hay imagen para compartir.", "OK");
                return;
            }

            try
            {
                var encodedData = EncodeBitmap(modifiedBitmap, SKEncodedImageFormat.Png, 100);
                var filePath = await SaveImageToTemporaryFileAsync(encodedData, "CompartirImagen.png");

                // Crear una nueva ShareFileRequest
                var request = new ShareFileRequest
                {
                    Title = "Compartir Imagen",
                    File = new ShareFile(filePath)
                };

                // Llamar a Share.RequestAsync para compartir el archivo
                await Share.RequestAsync(request);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error al compartir", ex.Message, "OK");
            }
        }


        private async Task<string> SaveImageToTemporaryFileAsync(SKData data, string filename)
        {
            var tempFolder = FileSystem.CacheDirectory;
            var filePath = Path.Combine(tempFolder, filename);

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await data.AsStream().CopyToAsync(fileStream);
            }

            return filePath;
        }

        private SKData EncodeBitmap(SKBitmap bitmap, SKEncodedImageFormat format, int quality)
        {
            using (var image = SKImage.FromBitmap(bitmap))
            {
                return image.Encode(format, quality);
            }
        }

        // Ejemplo de uso para codificar como PNG
        // var encodedData = EncodeBitmap(modifiedBitmap, SKEncodedImageFormat.Png, 100);
        private async Task SaveImageToFileAsync(SKData data, string filename)
        {
            // Determinar la ruta de la carpeta de imágenes públicas
            var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            var folderPath = Path.Combine(picturesPath, "Editor de imagenes");

            // Crear la carpeta si no existe
            Directory.CreateDirectory(folderPath);

            // Guardar el archivo en la carpeta
            var filePath = Path.Combine(folderPath, filename);

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await data.AsStream().CopyToAsync(fileStream);
            }
        }
        // Ejemplo de uso
        // await SaveImageToFileAsync(encodedData, "imagen_guardada.png");

    }


    public enum ColorFilterType
    {
        Gray,
        Red,
        Green,
        Blue
        // ... otros tipos de filtro ...
    }

}