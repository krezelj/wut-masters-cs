namespace MastersAlgorithms.ActorCritic
{
    public class ActorCriticPolicy
    {
        public Model Actor;
        public Model Critic;

        public ActorCriticPolicy(string modelDirectory)
        {
            string actor_path = Path.Join(modelDirectory, "actor.onnx");
            string critic_path = Path.Join(modelDirectory, "critic.onnx");

            Actor = new Model(actor_path);
            Critic = new Model(critic_path);
        }

        public float[] GetLogits(float[] input)
        {
            return Actor.Forward(input);
        }

        public float[] GetProbs(float[] input)
        {
            return Utils.Softmax(GetLogits(input));
        }

        public float[] GetMaskedProbs(float[] input, bool[] mask)
        {
            return Utils.MaskedSoftmax(input, mask);
        }

        public float GetValue(float[] input)
        {
            return Critic.Forward(input)[0];
        }
    }
}