using System.Collections.Generic;

namespace LegacyCore.Custom
{
    public class CustomLevelCollectionSO : StandardLevelCollectionSO
    {
        public List<StandardLevelSO> levelsList { get; private set; }

        public void Init(StandardLevelSO[] levels)
            => levelsList = new(levels);
    }
}
