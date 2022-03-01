namespace QuickGrid;

using Microsoft.AspNetCore.Components;
using QuickGrid.Infrastructure;

public class TemplateColumn<TGridItem> : ColumnBase<TGridItem>
{
    private static readonly RenderFragment<TGridItem> s_emptyChildContent = _ => builder => { };

    [Parameter] public RenderFragment<TGridItem> ChildContent { get; set; } = s_emptyChildContent;
    [Parameter] public Func<IQueryable<TGridItem>, SortBy<TGridItem>>? SortBy { get; set; }

    protected override void OnParametersSet()
    {
        CellContent = ChildContent;
    }

    internal override bool CanSort => SortBy != null;

    internal override IQueryable<TGridItem> GetSortedItems(IQueryable<TGridItem> source, bool ascending)
        => SortBy == null ? source : SortBy(source).Apply(source, ascending);
}
