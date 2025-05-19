using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace MastersAlgorithms.ActorCritic
{
    public class Model
    {
        private InferenceSession _session;
        public string Device;
        public int ExpectedBatchCount;
        private int[] _inputDimensions;
        private int[] _outputDimensions;
        private float[] _bufferInput;
        private float[] _bufferOutput;
        readonly string[] _inputNames;
        readonly string[] _outputNames;
        FixedBufferOnnxValue[] _inputValues;
        FixedBufferOnnxValue[] _outputValues;

        public Model(string pathToModel, string device = "cpu", int expectedBatchCount = 1)
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

            int inputFlatLength = Utils.Product(_inputDimensions[1..]);
            int outputFlatLength = Utils.Product(_outputDimensions[1..]);

            _bufferInput = new float[inputFlatLength * ExpectedBatchCount];
            _bufferOutput = new float[outputFlatLength * ExpectedBatchCount];

            _inputDimensions[0] = ExpectedBatchCount;
            _outputDimensions[0] = ExpectedBatchCount;
            var tensorInput = new DenseTensor<float>(
                _bufferInput, _inputDimensions);
            var tensorOutput = new DenseTensor<float>(
                _bufferOutput, _outputDimensions);

            FixedBufferOnnxValue valueInput = FixedBufferOnnxValue.CreateFromTensor(tensorInput);
            FixedBufferOnnxValue valueOutput = FixedBufferOnnxValue.CreateFromTensor(tensorOutput);

            _inputValues = [valueInput];
            _outputValues = [valueOutput];
        }

        public float[] Forward(float[] input, int batchCount = 1)
        {
            if (batchCount == ExpectedBatchCount)
                return BufferedInference(input);

            _inputDimensions[0] = batchCount;
            var tensorInput = new DenseTensor<float>(input, _inputDimensions);
            var namedInput = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", tensorInput)
            };
            using var output = _session.Run(namedInput);
            return output.First().AsEnumerable<float>().ToArray();
        }

        private float[] BufferedInference(float[] input)
        {
            input.CopyTo(_bufferInput, 0);
            _session.Run(_inputNames, _inputValues, _outputNames, _outputValues);
            return _bufferOutput;
        }

    }
}