namespace TreeId;

/// <summary>
/// Turns a decoded photo into the model's input, matching the training pipeline:
/// resize the shorter side to InputSize × ResizeRatio (area-averaged, like an antialiased resize),
/// center-crop InputSize × InputSize, then normalize each channel with the pack's mean and std.
/// </summary>
public static class Preprocess
{
	/// <param name="rgb">Packed 8-bit RGB pixels, row by row, 3 bytes per pixel.</param>
	/// <returns>A 1×3×S×S tensor in channel-first order.</returns>
	public static float[] ToTensor(byte[] rgb, int width, int height, Pack pack)
	{
		int s = pack.InputSize;
		int shorter = (int)(s * pack.ResizeRatio);
		// Same rounding as torchvision Resize(int): shorter side exact, longer side truncated
		int rw, rh;
		if (width <= height) { rw = shorter; rh = (int)((long)shorter * height / width); }
		else { rh = shorter; rw = (int)((long)shorter * width / height); }
		int left = (int)Math.Round((rw - s) / 2.0), top = (int)Math.Round((rh - s) / 2.0);

		var xTaps = Taps(width, rw, left, s);
		var yTaps = Taps(height, rh, top, s);

		// Horizontal pass over all source rows: width -> s columns
		var tmp = new float[height * s * 3];
		for (int y = 0; y < height; y++)
		{
			int row = y * width * 3;
			for (int x = 0; x < s; x++)
			{
				float r = 0, g = 0, b = 0;
				foreach (var (i, w) in xTaps[x])
				{
					int p = row + i * 3;
					r += rgb[p] * w; g += rgb[p + 1] * w; b += rgb[p + 2] * w;
				}
				int o = (y * s + x) * 3;
				tmp[o] = r; tmp[o + 1] = g; tmp[o + 2] = b;
			}
		}

		// Vertical pass, then normalize into channel-first layout
		var output = new float[3 * s * s];
		int plane = s * s;
		for (int y = 0; y < s; y++)
		{
			for (int x = 0; x < s; x++)
			{
				float r = 0, g = 0, b = 0;
				foreach (var (i, w) in yTaps[y])
				{
					int p = (i * s + x) * 3;
					r += tmp[p] * w; g += tmp[p + 1] * w; b += tmp[p + 2] * w;
				}
				int o = y * s + x;
				output[o] = (r / 255f - pack.Mean[0]) / pack.Std[0];
				output[plane + o] = (g / 255f - pack.Mean[1]) / pack.Std[1];
				output[2 * plane + o] = (b / 255f - pack.Mean[2]) / pack.Std[2];
			}
		}
		return output;
	}

	/// <summary>
	/// For each output pixel along one axis, the source pixels and weights that make it up.
	/// This is the same triangle (bilinear) filter Pillow uses for training images: when shrinking,
	/// the filter widens with the scale so every source pixel contributes (antialiasing).
	/// </summary>
	static (int index, float weight)[][] Taps(int src, int resized, int offset, int count)
	{
		double scale = (double)src / resized; // source pixels per resized pixel
		double support = Math.Max(scale, 1.0);
		var taps = new (int, float)[count][];
		for (int o = 0; o < count; o++)
		{
			double center = (o + offset + 0.5) * scale;
			int lo = Math.Max(0, (int)(center - support + 0.5));
			int hi = Math.Min(src, (int)(center + support + 0.5));
			var list = new List<(int, float)>();
			double total = 0;
			for (int i = lo; i < hi; i++)
			{
				double w = 1 - Math.Abs((i + 0.5 - center) / support);
				if (w > 0) { list.Add((i, (float)w)); total += w; }
			}
			for (int k = 0; k < list.Count; k++) list[k] = (list[k].Item1, (float)(list[k].Item2 / total));
			taps[o] = list.ToArray();
		}
		return taps;
	}
}
