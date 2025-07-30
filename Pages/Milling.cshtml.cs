using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Text.Json;
using CncGcodeSimulator.Models;

namespace CncGcodeSimulator.Pages
{
    public class MillingModel : PageModel
    {
        [BindProperty]
        public string UserGCode { get; set; } = "";

        public string MillingCoordinatesJson { get; set; } = "[]";

        public void OnGet()
        {
            if (string.IsNullOrWhiteSpace(UserGCode))
                UserGCode = "G0 X0 Y0\nG1 X10 Y10\nG1 X20 Y0";
        }

        public void OnPost()
        {
            if (string.IsNullOrWhiteSpace(UserGCode))
                return;

            var millingPoints = ParseMillingGCode(UserGCode);
            MillingCoordinatesJson = JsonSerializer.Serialize(millingPoints);
        }

        private List<Point2D> ParseMillingGCode(string gcode)
        {
            var points = new List<Point2D>();
            double x = 0, y = 0;
            points.Add(new Point2D(250 + x * 5, 250 - y * 5));

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
                if (part.StartsWith("X") && double.TryParse(part.Substring(1), out var valX))
                    x = valX;
                else if (part.StartsWith("Y") && double.TryParse(part.Substring(1), out var valY))
                    y = valY;
            }
            return (x, y);
        }
    }
}
