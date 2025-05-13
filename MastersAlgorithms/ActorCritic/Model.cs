
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace MastersAlgorithms.ActorCritic
{
    public class Model
    {
        private InferenceSession _session;

        public Model(string pathToModel)
        {
            _session = new InferenceSession(pathToModel);
        }

        public float[] Forward(float[] input, int batchCount = 1)
        {
            var tensorInput = new DenseTensor<float>(input, [batchCount, input.Length / batchCount]);
            var namedInput = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", tensorInput)
            };
            using var output = _session.Run(namedInput);
            return output.First().AsEnumerable<float>().ToArray();
        }

    }
}