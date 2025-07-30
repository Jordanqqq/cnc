using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Text.Json;
using System.Globalization;

namespace CncGcodeSimulator.Pages
{
    public class ArcInterpolationModel : PageModel
    {
        [BindProperty]
        public string UserGCode { get; set; } = "";

        public string ArcCoordinatesJson { get; set; } = "{}";

        public void OnPost()
        {
            if (string.IsNullOrWhiteSpace(UserGCode))
                return;

            var result = ParseGCodeWithArcs(UserGCode);
            ArcCoordinatesJson = JsonSerializer.Serialize(result);
        }

        public record Point2D(double x, double y);
        public record ArcData(double centerX, double centerY, double radius, double startAngle, double endAngle, bool clockwise);

        public class GCodeResult
        {
            public List<Point2D> points { get; set; } = new();
            public List<ArcData> arcs { get; set; } = new();
        }

        private GCodeResult ParseGCodeWithArcs(string gcode)
        {
            var res = new GCodeResult();
            double x = 0, y = 0;
            res.points.Add(new Point2D(250 + x * 5, 250 - y * 5)); // старт в центре

            var lines = gcode.Split('\n');
            foreach (var lineRaw in lines)
            {
                var line = lineRaw.Trim().ToUpper();
                if (line.StartsWith("G0") || line.StartsWith("G1"))
                {
                    (x, y) = ParseXY(line, x, y);
                    res.points.Add(new Point2D(250 + x * 5, 250 - y * 5));
                }
                else if (line.StartsWith("G2") || line.StartsWith("G3"))
                {
                    bool clockwise = line.StartsWith("G2");
                    var (newX, newY, iOffset, jOffset) = ParseXYIJ(line, x, y);
                    var centerX = x + iOffset;
                    var centerY = y + jOffset;

                    double startAngle = Math.Atan2(y - centerY, x - centerX);
                    double endAngle = Math.Atan2(newY - centerY, newX - centerX);

                    double radius = Math.Sqrt(iOffset * iOffset + jOffset * jOffset);

                    res.points.Add(new Point2D(250 + newX * 5, 250 - newY * 5));
                    res.arcs.Add(new ArcData(
                        250 + centerX * 5,
                        250 - centerY * 5,
                        radius * 5,
                        startAngle,
                        endAngle,
                        clockwise // передаём именно clockwise, в JS инвертируем
                    ));

                    x = newX;
                    y = newY;
                }
            }
            return res;
        }

        private (double x, double y, double i, double j) ParseXYIJ(string line, double oldX, double oldY)
        {
            double x = oldX, y = oldY, i = 0, j = 0;
            var parts = line.Split(' ');
            foreach (var part in parts)
            {
                if (part.StartsWith("X") && double.TryParse(part.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out var valX))
                    x = valX;
                else if (part.StartsWith("Y") && double.TryParse(part.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out var valY))
                    y = valY;
                else if (part.StartsWith("I") && double.TryParse(part.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out var valI))
                    i = valI;
                else if (part.StartsWith("J") && double.TryParse(part.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out var valJ))
                    j = valJ;
            }
            return (x, y, i, j);
        }

        private (double x, double y) ParseXY(string line, double oldX, double oldY)
        {
            double x = oldX;
            double y = oldY;
            var parts = line.Split(' ');
            foreach (var part in parts)
            {
                if (part.StartsWith("X") && double.TryParse(part.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out var valX))
                    x = valX;
                else if (part.StartsWith("Y") && double.TryParse(part.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out var valY))
                    y = valY;
            }
            return (x, y);
        }
    }
}
