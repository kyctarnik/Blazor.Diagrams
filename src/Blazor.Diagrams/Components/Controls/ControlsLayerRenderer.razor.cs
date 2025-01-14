using System;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Controls;
using Blazor.Diagrams.Core.Extensions;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models.Base;
using Blazor.Diagrams.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Blazor.Diagrams.Components.Controls;

public partial class ControlsLayerRenderer : IDisposable
{
    private bool _shouldRender;

    [CascadingParameter] public BlazorDiagram BlazorDiagram { get; set; } = null!;

    [Parameter] public bool Svg { get; set; }

    public void Dispose()
    {
        BlazorDiagram.Controls.ChangeCaused -= OnControlsChangeCaused;
    }

    protected override void OnInitialized()
    {
        BlazorDiagram.Controls.ChangeCaused += OnControlsChangeCaused;
	}

    protected override bool ShouldRender()
    {
        if (!_shouldRender)
            return false;

        _shouldRender = false;
        return true;
    }

    private void OnControlsChangeCaused(Model cause)
    {
        if (Svg != cause.IsSvg())
            return;

        _shouldRender = true;
        InvokeAsync(StateHasChanged);
    }

    private RenderFragment RenderControl(Model model, Control control, Point position, bool svg)
	{
		var componentType = BlazorDiagram.GetComponent(control.GetType()) ?? throw new BlazorDiagramsException($"A component couldn't be found for the user action {control.GetType().Name}");

		return builder =>
		{
			var index = 0;
			builder.OpenElement(index, svg ? "g" : "div");
			builder.AddAttribute(++index, "class", $"{(control is ExecutableControl ? "executable " : "")}diagram-control {control.GetType().Name}");
			if (svg)
			{
				builder.AddAttribute(++index, "transform", $"translate({position.X.ToInvariantString()} {position.Y.ToInvariantString()})");
			}
			else
			{
				builder.AddAttribute(++index, "style", $"top: {position.Y.ToInvariantString()}px; left: {position.X.ToInvariantString()}px");
			}

			if (control is ExecutableControl ec)
			{
				builder.AddAttribute(++index, "onpointerdown", EventCallback.Factory.Create<PointerEventArgs>(this, e => OnPointerDown(e, model, ec)));
				builder.AddEventStopPropagationAttribute(++index, "onpointerdown", true);
			}

			builder.OpenComponent(++index, componentType);
			builder.AddAttribute(++index, "Control", control);
			builder.AddAttribute(++index, "Model", model);
			builder.CloseComponent();
			builder.CloseElement();
		};
	}

    private async Task OnPointerDown(PointerEventArgs e, Model model, ExecutableControl control)
    {
        if (e.Button == 0 || e.Buttons == 1) await control.OnPointerDown(BlazorDiagram, model, e.ToCore());
    }
}