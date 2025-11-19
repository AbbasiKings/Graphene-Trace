using GrapheneTrace.Core.Constants;
using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.Utils;

public static class FileProcessor
{
    public static int[,] ParseCsvMatrix(string csvData)
    {
        var rows = csvData
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(row => row.Split(',', StringSplitOptions.TrimEntries))
            .ToArray();

        if (rows.Length != AppConstants.CsvMatrixSize ||
            rows.Any(r => r.Length != AppConstants.CsvMatrixSize))
        {
            throw new InvalidOperationException("CSV data must represent a 32x32 matrix.");
        }

        var matrix = new int[AppConstants.CsvMatrixSize, AppConstants.CsvMatrixSize];

        for (var i = 0; i < AppConstants.CsvMatrixSize; i++)
        {
            for (var j = 0; j < AppConstants.CsvMatrixSize; j++)
            {
                matrix[i, j] = int.TryParse(rows[i][j], out var value) ? value : 0;
            }
        }

        return matrix;
    }

    public static double CalculatePeakPressureIndex(int[,] matrix)
    {
        var size = AppConstants.CsvMatrixSize;
        var visited = new bool[size, size];
        var filtered = (int[,])matrix.Clone();
        var max = 0;

        for (var row = 0; row < size; row++)
        {
            for (var col = 0; col < size; col++)
            {
                if (filtered[row, col] <= 0 || visited[row, col])
                {
                    continue;
                }

                var positions = new List<(int r, int c)>();
                var queue = new Queue<(int r, int c)>();
                queue.Enqueue((row, col));
                visited[row, col] = true;

                var componentMax = 0;

                while (queue.Count > 0)
                {
                    var (cr, cc) = queue.Dequeue();
                    positions.Add((cr, cc));

                    var value = filtered[cr, cc];
                    if (value > componentMax)
                    {
                        componentMax = value;
                    }

                    foreach (var (nr, nc) in GetNeighbors(cr, cc, size))
                    {
                        if (visited[nr, nc] || filtered[nr, nc] <= 0)
                        {
                            continue;
                        }

                        visited[nr, nc] = true;
                        queue.Enqueue((nr, nc));
                    }
                }

                if (positions.Count < AppConstants.MinPixelAreaForAlert)
                {
                    foreach (var (r, c) in positions)
                    {
                        filtered[r, c] = 0;
                    }
                }
                else
                {
                    if (componentMax > max)
                    {
                        max = componentMax;
                    }
                }
            }
        }

        return max;
    }

    public static double CalculateContactAreaPercent(int[,] matrix)
    {
        var total = matrix.Length;
        var contactPixels = matrix.Cast<int>().Count(v => v > AppConstants.PressureZeroForceValue);
        return total == 0 ? 0 : Math.Round((double)contactPixels / total * 100, 2);
    }

    public static RiskLevel DetermineRiskLevel(double peakPressureIndex)
    {
        if (peakPressureIndex >= AppConstants.PressureCriticalThreshold)
        {
            return RiskLevel.Critical;
        }

        if (peakPressureIndex >= AppConstants.PressureHighThreshold)
        {
            return RiskLevel.High;
        }

        if (peakPressureIndex >= AppConstants.PressureHighThreshold * 0.75)
        {
            return RiskLevel.Medium;
        }

        return RiskLevel.Low;
    }

    private static IEnumerable<(int r, int c)> GetNeighbors(int row, int col, int size)
    {
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };

        for (var i = 0; i < dr.Length; i++)
        {
            var nr = row + dr[i];
            var nc = col + dc[i];
            if (nr >= 0 && nr < size && nc >= 0 && nc < size)
            {
                yield return (nr, nc);
            }
        }
    }
}

