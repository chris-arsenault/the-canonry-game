#pragma warning disable CA1305
using Engine.Projection;

using Godot;

namespace TheCanonryGame.UI;

public partial class EntityBrowser
{
    private void RebuildFilterOptions()
    {
        if (_projection is null)
        {
            return;
        }

        _updatingFilterControls = true;
        var entities = _projection.Entities.Values.ToArray();
        RebuildKindFilterOptions(entities);
        RebuildCultureFilterOptions(entities);
        RebuildStatusFilterOptions(entities);
        RebuildSlotFilterOptions(entities);
        RebuildWorkTierFilterOptions(entities);
        RebuildSortOptions();
        RebuildGeneratorFilterOptions(entities);
        if (_searchEdit is not null && !string.Equals(_searchEdit.Text, _searchText, StringComparison.Ordinal))
        {
            _searchEdit.Text = _searchText;
        }

        _updatingFilterControls = false;
    }

    private void RebuildKindFilterOptions(UiEntityProjection[] entities)
    {
        if (_kindFilterOption is null || _projection is null)
        {
            return;
        }

        var options = entities
            .GroupBy(static entity => entity.KindId)
            .Select(group => new OptionData(
                group.Key,
                $"{ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Kind, group.Key, $"Kind {group.Key}")} ({group.Count()})"))
            .OrderBy(static option => option.Text, StringComparer.Ordinal)
            .ToList();
        options.Insert(0, new OptionData(AllFilterId, $"All kinds ({entities.Length})"));
        _selectedKindFilterId = RebuildOption(_kindFilterOption, options, _selectedKindFilterId, AllFilterId);
    }

    private void RebuildCultureFilterOptions(UiEntityProjection[] entities)
    {
        if (_cultureFilterOption is null || _projection is null)
        {
            return;
        }

        var options = entities
            .GroupBy(static entity => entity.CultureId)
            .Select(group => new OptionData(
                group.Key,
                $"{ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Culture, group.Key, $"Culture {group.Key}")} ({group.Count()})"))
            .OrderBy(static option => option.Text, StringComparer.Ordinal)
            .ToList();
        options.Insert(0, new OptionData(AllFilterId, $"All cultures ({entities.Length})"));
        _selectedCultureFilterId = RebuildOption(_cultureFilterOption, options, _selectedCultureFilterId, AllFilterId);
    }

    private void RebuildStatusFilterOptions(UiEntityProjection[] entities)
    {
        if (_statusFilterOption is null || _projection is null)
        {
            return;
        }

        var active = entities.Count(static entity => entity.StatusCode == EntityStatusCodes.Active);
        var historical = entities.Length - active;
        var options = new List<OptionData>
        {
            new(AllFilterId, $"All states ({entities.Length})"),
            new(StatusActiveFilterId, $"Active ({active})"),
            new(StatusHistoricalFilterId, $"Historical ({historical})"),
        };
        _selectedStatusFilterId = RebuildOption(_statusFilterOption, options, _selectedStatusFilterId, StatusActiveFilterId);
    }

    private void RebuildSlotFilterOptions(UiEntityProjection[] entities)
    {
        if (_slotFilterOption is null || _projection is null)
        {
            return;
        }

        var occupied = entities.Count(static entity => entity.CultureSlotCount > 0);
        var options = new List<OptionData>
        {
            new(AllFilterId, $"Any slot ({entities.Length})"),
            new(SlotOccupiedFilterId, $"Slotted ({occupied})"),
            new(SlotEmptyFilterId, $"Unslotted ({entities.Length - occupied})"),
            new(SlotPositiveFilterId, $"Positive ({entities.Count(static entity => entity.CultureSlotPolarityCode == RelationshipPolarityCodes.Positive)})"),
            new(SlotNeutralFilterId, $"Neutral ({entities.Count(static entity => entity.CultureSlotPolarityCode == RelationshipPolarityCodes.Neutral)})"),
            new(SlotNegativeFilterId, $"Negative ({entities.Count(static entity => entity.CultureSlotPolarityCode == RelationshipPolarityCodes.Negative)})"),
        };
        _selectedSlotFilterId = RebuildOption(_slotFilterOption, options, _selectedSlotFilterId, AllFilterId);
    }

    private void RebuildWorkTierFilterOptions(UiEntityProjection[] entities)
    {
        if (_workTierFilterOption is null || _projection is null)
        {
            return;
        }

        var options = entities
            .GroupBy(static entity => entity.WorkTier)
            .OrderBy(static group => group.Key)
            .Select(group => new OptionData(group.Key, $"Work {group.Key} ({group.Count()})"))
            .ToList();
        options.Insert(0, new OptionData(AnyWorkTierFilterId, $"Any work ({entities.Length})"));
        _selectedWorkTierFilterId = RebuildOption(
            _workTierFilterOption,
            options,
            _selectedWorkTierFilterId,
            AnyWorkTierFilterId);
    }

    private void RebuildSortOptions()
    {
        if (_sortOption is null)
        {
            return;
        }

        var options = new List<OptionData>
        {
            new((int)RegistrySortMode.Kind, "Sort: kind"),
            new((int)RegistrySortMode.Name, "Sort: name"),
            new((int)RegistrySortMode.Culture, "Sort: culture"),
            new((int)RegistrySortMode.HealthLow, "Health: low"),
            new((int)RegistrySortMode.HealthHigh, "Health: high"),
            new((int)RegistrySortMode.AgeOldest, "Age: old"),
            new((int)RegistrySortMode.AgeYoungest, "Age: young"),
            new((int)RegistrySortMode.WorkTier, "Work: high"),
            new((int)RegistrySortMode.RecentActivity, "Recent"),
        };
        var selected = RebuildOption(_sortOption, options, (int)_selectedSortMode, (int)RegistrySortMode.Kind);
        _selectedSortMode = (RegistrySortMode)selected;
    }

    private void RebuildGeneratorFilterOptions(UiEntityProjection[] entities)
    {
        if (_generatorFilterOption is null || _projection is null)
        {
            return;
        }

        var countsByGenerator = entities
            .Where(static entity => entity.CreatedByGeneratorId is not null)
            .GroupBy(static entity => entity.CreatedByGeneratorId!.Value)
            .ToDictionary(static group => group.Key, static group => group.Count());
        var seedCount = entities.Count(static entity => entity.CreatedByGeneratorId is null);
        var options = countsByGenerator.Keys
            .Select(generatorId => new OptionData(
                generatorId,
                $"{ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Generator, generatorId, $"Generator {generatorId}")} ({countsByGenerator[generatorId]})"))
            .OrderBy(static option => option.Text, StringComparer.Ordinal)
            .ToList();
        options.Insert(0, new OptionData(SeedGeneratorFilterId, $"Seeded ({seedCount})"));
        options.Insert(0, new OptionData(AllGeneratorFilterId, $"All sources ({entities.Length})"));
        _selectedGeneratorFilterId = RebuildOption(
            _generatorFilterOption,
            options,
            _selectedGeneratorFilterId,
            AllGeneratorFilterId);
    }

    private static int RebuildOption(
        OptionButton option,
        IReadOnlyList<OptionData> options,
        int selectedId,
        int fallbackId)
    {
        var hasSelected = options.Any(candidate => candidate.Id == selectedId);
        var effectiveSelected = hasSelected ? selectedId : fallbackId;
        option.Clear();
        foreach (var candidate in options)
        {
            option.AddItem(candidate.Text, candidate.Id);
        }

        var selectedIndex = FindOptionIndexById(option, effectiveSelected);
        if (selectedIndex < 0)
        {
            selectedIndex = 0;
            effectiveSelected = option.ItemCount > 0 ? option.GetItemId(0) : fallbackId;
        }

        if (selectedIndex >= 0)
        {
            option.Select(selectedIndex);
        }

        return effectiveSelected;
    }

    private static int FindOptionIndexById(OptionButton option, int id)
    {
        for (var i = 0; i < option.ItemCount; i++)
        {
            if (option.GetItemId(i) == id)
            {
                return i;
            }
        }

        return -1;
    }
}
