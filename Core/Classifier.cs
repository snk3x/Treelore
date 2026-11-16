using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace TreeId;

/// <summary>Runs the region pack's ONNX model on the device's CPU.</summary>
public sealed class Classifier : IDisposable
{
	readonly InferenceSession _session;
	readonly int _size;

	/// <param name="threads">2 keeps low-end phones responsive; more threads barely help this small model.</param>
	public Classifier(byte[] model, int inputSize, int threads = 2)
	{
		var options = new SessionOptions { IntraOpNumThreads = threads, GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL };
		_session = new InferenceSession(model, options);
		_size = inputSize;
	}

	/// <summary>Log-probabilities for every species in the pack, in label order.</summary>
	public float[] LogProbs(float[] tensor)
	{
		var input = new DenseTensor<float>(tensor, new[] { 1, 3, _size, _size });
		using var results = _session.Run(new[] { NamedOnnxValue.CreateFromTensor("image", input) });
		var logits = results[0].AsEnumerable<float>().ToArray();
		float max = logits.Max();
		double sum = 0;
		foreach (var l in logits) sum += Math.Exp(l - max);
		float logSum = (float)Math.Log(sum) + max;
		for (int i = 0; i < logits.Length; i++) logits[i] -= logSum;
		return logits;
	}

	public void Dispose() => _session.Dispose();
}
