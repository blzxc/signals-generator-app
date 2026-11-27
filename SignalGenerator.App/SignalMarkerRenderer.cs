using SignalProcessing.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGenerator.App
{
    public sealed class SignalMarkerRenderer
    {
        private ScottPlot.Plot _plot;
        private List<ScottPlot.Plottables.Marker> _markers = new List<ScottPlot.Plottables.Marker>();

        public SignalMarkerRenderer(ScottPlot.Plot plot)
        {
            if (plot == null)
            {
                throw new ArgumentNullException(nameof(plot));
            }
            _plot = plot;
        }
        public void AddMarker(
            float time, float value, CrossingDirection direction)
        {
            ScottPlot.Plottables.Marker marker = _plot.Add.Marker(time, value);
            marker.Size = 9;
            if (direction == CrossingDirection.Up)
            {
                marker.Color = ScottPlot.Colors.Green;
            }
            else
            {
                marker.Color = ScottPlot.Colors.Red;
            }
            _markers.Add(marker);
        }
        public void ClearMarkers()
        {
            for (int i = 0; i < _markers.Count; i++)
            {
                _plot.Remove(_markers[i]);
            }
            _markers.Clear();
        }
    }
}
