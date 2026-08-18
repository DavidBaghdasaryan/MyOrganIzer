using System.Windows.Controls;
using MyOrganizer.Wpf.Controls;
using MyOrganizer.Wpf.MVVM.ViewModels;

namespace MyOrganizer.Wpf.MVVM.Views.DentalChart;

public partial class SurfaceProcedureContextView : UserControl
{
    public SurfaceProcedureContextView()
    {
        InitializeComponent();
        SurfaceSelector.SurfacesChanged += OnSurfacesChanged;
        DataContextChanged += (_, _) =>
        {
            if (DataContext is SurfaceProcedureContextViewModel vm)
                SurfaceSelector.ToothNumber = vm.ToothNumber;
        };
    }

    private void OnSurfacesChanged(object? sender, ToothSurfaceEventArgs e)
    {
        if (DataContext is SurfaceProcedureContextViewModel vm)
            vm.OnSurfacesChanged(e.SelectedSurfaces, e.WholeTooth);
    }
}
