namespace MastersAlgorithms.ActorCritic
{
    public class ActorCriticPolicy
    {
        public Model? Actor;
        public Model? Critic;
        public UnifiedModel? ActorCritic;
        public string Device;
        public int ExpectedBatchCount;
        public bool Unified;

        public ActorCriticPolicy(
            string modelDirectory,
            string device = "cpu",
            int expectedBatchCount = 1,
            bool unified = true)
        {
            Device = device;
            ExpectedBatchCount = expectedBatchCount;
            Unified = unified;
            if (!Unified)
            {
                string actorPath = Path.Join(modelDirectory, "actor.onnx");
                string criticPath = Path.Join(modelDirectory, "critic.onnx");

                Actor = new Model(actorPath, Device, ExpectedBatchCount);
                Critic = new Model(criticPath, Device, ExpectedBatchCount);
            }
            else
            {
                string actorCriticPath = Path.Join(modelDirectory, "actor_critic.onnx");
                ActorCritic = new UnifiedModel(actorCriticPath, Device, ExpectedBatchCount);
            }
        }

        public float[] GetLogits(float[] input, int batchCount = 1)
        {
            if (Actor != null)
                return Actor.Forward(input, batchCount);
            else
                return ActorCritic!.Forward(input, batchCount).logits;
        }

        public float[] GetProbs(float[] input, int batchCount = 1, float t = 1.0f)
        {
            return Utils.Softmax(GetLogits(input, batchCount), batchCount, t);
        }

        public float[] GetMaskedProbs(float[] input, bool[] mask, int batchCount = 1, float t = 1.0f)
        {
            return Utils.MaskedSoftmax(GetLogits(input, batchCount), mask, batchCount, t);
        }

        public float[] GetValues(float[] input, int batchCount = 1)
        {
            if (Critic != null)
                return Critic.Forward(input, batchCount);
            else
                return ActorCritic!.Forward(input, batchCount).value;
        }

        public (float[] probs, float[] values) GetMaskedProbsAndValues(
            float[] input,
            bool[] mask,
            int batchCount = 1,
            float t = 1.0f)
        {
            float[] probs, values;
            if (!Unified)
            {
                probs = GetMaskedProbs(input, mask, batchCount);
                values = GetValues(input, batchCount);
            }
            else
            {
                (float[] logits, values) = ActorCritic!.Forward(input, batchCount);
                probs = Utils.MaskedSoftmax(logits, mask, batchCount, t);
            }
            return (probs, values);
        }
    }
}