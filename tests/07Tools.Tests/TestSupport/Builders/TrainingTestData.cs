namespace RunescapeTools.Tests.TestSupport.Builders;
internal static class TrainingTestData
{
    public static TrainingRateBand TrainingBand(MainEhpCatalogue catalogue, string skill, long startExperience) =>
        catalogue.Skills.Single(definition => definition.Skill == skill)
            .Bands.Single(band => band.StartExperience == startExperience);

    public static TrainingResourceFlow Resource(TrainingRateBand band, int itemId) =>
        band.Economics?.Resources.Single(resource => resource.ItemId == itemId)
        ?? throw new InvalidOperationException($"{band.Method} is missing item {itemId}.");

    public static TrainingResourceFlow DirectedResource(
        TrainingRateBand band,
        int itemId,
        TrainingFlowDirection direction) =>
        band.Economics?.Resources.Single(resource =>
            resource.ItemId == itemId && resource.Direction == direction)
        ?? throw new InvalidOperationException($"{band.Method} is missing {direction} item {itemId}.");

}
