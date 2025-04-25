namespace MastersAlgorithms.Games
{
    public class ZobristHash
    {
        sbyte _nTypes;
        sbyte _nPositions;

        ulong[][] _keys;
        ulong _key;
        public ulong Key => _key;

        public ZobristHash(sbyte nTypes, sbyte nPositions)
        {
            _nTypes = nTypes;
            _nPositions = nPositions;
            _key = 0;

            _keys = new ulong[_nTypes][];
            Random rng = new Random(0);
            for (int type = 0; type < _nTypes; type++)
            {
                _keys[type] = new ulong[_nPositions];
                for (int i = 0; i < _nPositions; i++)
                {
                    _keys[type][i] = (ulong)rng.NextInt64();
                }
            }
        }

        public void UpdateKey(int type, ulong positions)
        {
            ulong[] typeKeys = _keys[type];
            while (positions > 0)
            {
                _key ^= typeKeys[positions.PopNextIndex()];
            }
        }

        public void ResetKey()
        {
            _key = 0UL;
        }
    }
}