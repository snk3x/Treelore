using Android.Graphics;
using Android.Media;

namespace TreeMobile;

/// <summary>Reads phone photos: upright pixels for the model, smaller copies to keep, and saved GPS positions.</summary>
public static class PhotoDecoder
{
	/// <summary>
	/// Decodes a photo to packed RGB, turned upright, with its shorter side at least <paramref name="minShorter"/>.
	/// Big camera photos are subsampled while decoding so a 12-megapixel shot doesn't use 48 MB of memory.
	/// </summary>
	public static (byte[] rgb, int width, int height) DecodeForModel(string path, int minShorter)
	{
		using var bitmap = Load(path, minShorter);
		int w = bitmap.Width, h = bitmap.Height;
		var pixels = new int[w * h];
		bitmap.GetPixels(pixels, 0, w, 0, 0, w, h);
		var rgb = new byte[w * h * 3];
		for (int i = 0; i < pixels.Length; i++)
		{
			int p = pixels[i];
			rgb[i * 3] = (byte)(p >> 16);
			rgb[i * 3 + 1] = (byte)(p >> 8);
			rgb[i * 3 + 2] = (byte)p;
		}
		return (rgb, w, h);
	}

	/// <summary>Saves an upright JPEG copy no larger than about <paramref name="maxSide"/> pixels.</summary>
	public static void SaveCopy(string source, string dest, int maxSide = 1600)
	{
		using var bitmap = Load(source, maxSide / 2);
		float scale = Math.Min(1f, (float)maxSide / Math.Max(bitmap.Width, bitmap.Height));
		using var sized = scale < 1
			? Bitmap.CreateScaledBitmap(bitmap, (int)(bitmap.Width * scale), (int)(bitmap.Height * scale), true)
			: Bitmap.CreateBitmap(bitmap);
		using var stream = File.Create(dest);
		sized.Compress(Bitmap.CompressFormat.Jpeg, 85, stream);
	}

	/// <summary>The GPS position stored in the photo, if the camera saved one.</summary>
	public static (double lat, double lng)? GpsOf(string path)
	{
		try
		{
			var exif = new ExifInterface(path);
			var latLng = new float[2];
			if (exif.GetLatLong(latLng)) return (latLng[0], latLng[1]);
		}
		catch { /* no EXIF data */ }
		return null;
	}

	static Bitmap Load(string path, int minShorter)
	{
		var bounds = new BitmapFactory.Options { InJustDecodeBounds = true };
		BitmapFactory.DecodeFile(path, bounds);
		if (bounds.OutWidth <= 0) throw new InvalidOperationException("That file isn't a photo this phone can read.");
		int shorter = Math.Min(bounds.OutWidth, bounds.OutHeight), sample = 1;
		while (shorter / (sample * 2) >= minShorter) sample *= 2;
		var options = new BitmapFactory.Options { InSampleSize = sample, InPreferredConfig = Bitmap.Config.Argb8888 };
		var bitmap = BitmapFactory.DecodeFile(path, options);

		int degrees = 0;
		try
		{
			degrees = new ExifInterface(path).GetAttributeInt(ExifInterface.TagOrientation, 1) switch
			{
				6 => 90, 3 => 180, 8 => 270, _ => 0,
			};
		}
		catch { /* no EXIF data: assume upright */ }
		if (degrees == 0) return bitmap;

		var matrix = new Matrix();
		matrix.PostRotate(degrees);
		var rotated = Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matrix, true);
		bitmap.Recycle();
		bitmap.Dispose();
		return rotated;
	}
}
