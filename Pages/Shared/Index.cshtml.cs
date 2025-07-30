using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Collections.Generic;

namespace CncGcodeSimulator.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string UserGCode { get; set; } = "";

        [BindProperty]
        public string MachineType { get; set; } = "milling";

        public string MillingCoordinatesJson { get; set; } = "[]";

        public void OnPost()
        {
            if (string.IsNullOrWhiteSpace(UserGCode)) return;

            var points = ParseGCode(UserGCode);
            MillingCoordinatesJson = JsonSerializer.Serialize(points);
        }

        private List<Point2D> ParseGCode(string gcode)
        {
            var points = new List<Point2D>();
            double x = 0, y = 0;
            string lastCommand = "G0";

            points.Add(new Point2D(x, y)); // здесь без смещения

            var lines = gcode.Split('\n');
            foreach (var raw in lines)
            {
                var line = raw.Trim().ToUpper();
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("G")) lastCommand = line.Substring(0, 2);

                var newX = TryExtract('X', line, x);
                var newY = TryExtract('Y', line, y);
                var i = TryExtract('I', line, 0);
                var j = TryExtract('J', line, 0);

                if (lastCommand == "G0" || lastCommand == "G1")
                {
                    x = newX;
                    y = newY;
                    points.Add(new Point2D(x, y));
                }
                else if (lastCommand == "G2" || lastCommand == "G3")
                {
                    var arcPoints = GenerateArcPoints(x, y, newX, newY, i, j, lastCommand == "G2");
                    points.AddRange(arcPoints);
                    x = newX;
                    y = newY;
                }
            }

            return points;
        }

        private List<Point2D> GenerateArcPoints(double x0, double y0, double x1, double y1, double i, double j, bool clockwise)
        {
            var cx = x0 + i;
            var cy = y0 + j;
            var radius = System.Math.Sqrt(i * i + j * j);

            var startAngle = System.Math.Atan2(y0 - cy, x0 - cx);
            var endAngle = System.Math.Atan2(y1 - cy, x1 - cx);
            var points = new List<Point2D>();

            double angleStep = (clockwise ? -1 : 1) * System.Math.PI / 90;

            if (clockwise && endAngle > startAngle) endAngle -= 2 * System.Math.PI;
            if (!clockwise && endAngle < startAngle) endAngle += 2 * System.Math.PI;

            for (double angle = startAngle;
                clockwise ? angle >= endAngle : angle <= endAngle;
                angle += angleStep)
            {
                double px = cx + radius * System.Math.Cos(angle);
                double py = cy + radius * System.Math.Sin(angle);
                points.Add(new Point2D(px, py));
            }

            points.Add(new Point2D(x1, y1));
            return points;
        }

        private double TryExtract(char axis, string line, double fallback)
        {
            foreach (var part in line.Split(' '))
                if (part.StartsWith(axis) && double.TryParse(part.Substring(1), out var val))
                    return val;
            return fallback;
        }

        public record Point2D(double x, double y);
    }
}
