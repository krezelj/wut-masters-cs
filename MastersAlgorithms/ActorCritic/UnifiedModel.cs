using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace MastersAlgorithms.ActorCritic
{
    public class UnifiedModel
    {
        private InferenceSession _session;
        public string Device;
        public int ExpectedBatchCount;
        private int[] _inputDimensions;
        private int[] _outputDimensions;
        readonly string[] _inputNames;
        readonly string[] _outputNames;

        public UnifiedModel(string pathToModel, string device = "cpu", int expectedBatchCount = 1)
        {
            Device = device;
            ExpectedBatchCount = expectedBatchCount;
            switch (Device)
            {
                case "cuda":
                    {
                        using var gpuSessionOptions = SessionOptions.MakeSessionOptionWithCudaProvider(0);
                        _session = new InferenceSession(pathToModel, gpuSessionOptions);
                        break;
                    }
                case "cpu":
                    _session = new InferenceSession(pathToModel);
                    break;
                default:
                    throw new Exception($"Unknown device '{Device}'");
            }

            var inputMetadata = _session.InputMetadata;
            _inputNames = inputMetadata.Keys.ToArray();
            _inputDimensions = inputMetadata[_inputNames[0]].Dimensions.ToArray();

            var outputMetadata = _session.OutputMetadata;
            _outputNames = outputMetadata.Keys.ToArray();
            _outputDimensions = outputMetadata[_outputNames[0]].Dimensions.ToArray();

            _inputDimensions[0] = ExpectedBatchCount;
            _outputDimensions[0] = ExpectedBatchCount;
        }

        public (float[] logits, float[] value) Forward(float[] input, int batchCount = 1)
        {
            _inputDimensions[0] = batchCount;
            var tensorInput = new DenseTensor<float>(input, _inputDimensions);
            var namedInput = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", tensorInput)
            };
            using var output = _session.Run(namedInput);
            float[] logits = output[0].AsEnumerable<float>().ToArray();
            float[] values = output[1].AsEnumerable<float>().ToArray();
            return (logits, values);
        }

    }
}