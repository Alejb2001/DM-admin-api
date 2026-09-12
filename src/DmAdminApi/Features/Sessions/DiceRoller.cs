using DmAdminApi.Features.Sessions.Dtos;

namespace DmAdminApi.Features.Sessions;

public static class DiceRoller
{
    public static DiceResultDto Roll(DiceParseResult parsed, string formula)
    {
        var rolls = Enumerable.Range(0, parsed.NumDice)
            .Select(_ => Random.Shared.Next(1, parsed.DiceSides + 1))
            .ToList();

        List<int> kept;
        if (parsed.Advantage == "ventaja")
            kept = [rolls.Max()];
        else if (parsed.Advantage == "desventaja")
            kept = [rolls.Min()];
        else
            kept = [.. rolls];

        int total = kept.Sum() + parsed.Modifier;

        return new DiceResultDto(formula, parsed.Label, parsed.Modifier, parsed.Advantage, rolls, kept, total);
    }
}
