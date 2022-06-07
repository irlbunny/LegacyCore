namespace LegacyCore.Custom
{
    public class CustomLevelCollectionsForGameplayModes : LevelCollectionsForGameplayModes
    {
        public void SetCollections(LevelCollectionForGameplayMode[] collections)
            => _collections = collections;

        public override StandardLevelSO[] GetLevels(GameplayMode gameplayMode)
        {
            foreach (var levelCollectionForGameplayMode in _collections)
            {
                if (levelCollectionForGameplayMode.gameplayMode == gameplayMode)
                {
                    if (levelCollectionForGameplayMode is CustomLevelCollectionForGameplayMode customLevelCollectionForGameplayMode)
                    {
                        if (customLevelCollectionForGameplayMode.levelCollection is CustomLevelCollectionSO customLevelCollection)
                            return customLevelCollection.levelsList.ToArray();
                    }

                    return levelCollectionForGameplayMode.levelCollection.levels;
                }
            }

            return null;
        }
    }
}
