namespace MastersAlgorithms.ActorCritic
{
    public class ActorCriticPolicy
    {
        public Model Actor;
        public Model Critic;
        public string Device;
        public int ExpectedBatchCount;

        public ActorCriticPolicy(string modelDirectory, string device = "cpu", int expectedBatchCount = 1)
        {
            string actor_path = Path.Join(modelDirectory, "actor.onnx");
            string critic_path = Path.Join(modelDirectory, "critic.onnx");

            Device = device;
            ExpectedBatchCount = expectedBatchCount;
            Actor = new Model(actor_path, Device, ExpectedBatchCount);
            Critic = new Model(critic_path, Device, ExpectedBatchCount);
        }

        public float[] GetLogits(float[] input, int batchCount = 1)
        {
            return Actor.Forward(input, batchCount);
        }

        public float[] GetProbs(float[] input, int batchCount = 1)
        {
            return Utils.Softmax(GetLogits(input, batchCount), batchCount);
        }

        public float[] GetMaskedProbs(float[] input, bool[] mask, int batchCount = 1)
        {
            return Utils.MaskedSoftmax(GetLogits(input, batchCount), mask, batchCount);
        }

        public float[] GetValue(float[] input, int batchCount = 1)
        {
            return Critic.Forward(input, batchCount);
        }
    }
}