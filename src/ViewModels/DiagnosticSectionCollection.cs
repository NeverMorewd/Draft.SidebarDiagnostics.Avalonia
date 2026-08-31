using System.Collections.ObjectModel;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.ViewModels;

internal sealed class DiagnosticSectionCollection
{
    private readonly Dictionary<string, DiagnosticSectionViewModel> _sectionsById =
        new(StringComparer.Ordinal);

    public ObservableCollection<DiagnosticSectionViewModel> Items { get; } = [];

    public void Update(IReadOnlyList<DiagnosticSection> snapshots)
    {
        var activeIds = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < snapshots.Count; index++)
        {
            var snapshot = snapshots[index];
            if (!activeIds.Add(snapshot.Id))
            {
                continue;
            }

            if (!_sectionsById.TryGetValue(snapshot.Id, out var section))
            {
                section = new DiagnosticSectionViewModel(snapshot);
                _sectionsById.Add(snapshot.Id, section);
                Items.Insert(Math.Min(index, Items.Count), section);
                continue;
            }

            section.Apply(snapshot);
            var currentIndex = Items.IndexOf(section);
            if (currentIndex >= 0 && currentIndex != index)
            {
                Items.Move(currentIndex, index);
            }
        }

        for (var index = Items.Count - 1; index >= 0; index--)
        {
            var section = Items[index];
            if (activeIds.Contains(section.Id))
            {
                continue;
            }

            Items.RemoveAt(index);
            _sectionsById.Remove(section.Id);
        }
    }

    public void RefreshThemeResources()
    {
        foreach (var section in Items)
        {
            section.RefreshThemeResource();
        }
    }
}
