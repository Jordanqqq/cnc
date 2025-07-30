using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Text.Json;
using System.Globalization;

namespace CncGcodeSimulator.Pages
{
    public class LinearMovementModel : PageModel
    {
        [BindProperty]
        public string UserGCode { get; set; } = "";

        public string PointsJson { get; set; } = "[]";

        public void OnPost()
        {
            if (string.IsNullOrWhiteSpace(UserGCode))
                return;

            var points = ParseLinearGCode(UserGCode);
            PointsJson = JsonSerializer.Serialize(points);
        }

        public record Point2D(double x, double y);

        private List<Point2D> ParseLinearGCode(string gcode)
        {
            var points = new List<Point2D>();
            double x = 0, y = 0;
            points.Add(new Point2D(250 + x * 5, 250 - y * 5)); // старт - центр канваса

            var lines = gcode.Split('\n');
            foreach (var lineRaw in lines)
            {
                var line = lineRaw.Trim().ToUpper();
                if (line.StartsWith("G0") || line.StartsWith("G1"))
                {
                    (x, y) = ParseXY(line, x, y);
                    points.Add(new Point2D(250 + x * 5, 250 - y * 5));
                }
            }
            return points;
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
