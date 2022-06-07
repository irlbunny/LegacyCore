namespace LegacyCore.Custom
{
    public class CustomLevelCollectionForGameplayMode : LevelCollectionsForGameplayModes.LevelCollectionForGameplayMode
    {
        public CustomLevelCollectionForGameplayMode(GameplayMode gameplayMode, StandardLevelCollectionSO levelCollection)
        {
            _gameplayMode = gameplayMode;
            _levelCollection = levelCollection;
        }
    }
}
