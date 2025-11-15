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
        var max = 0;
        foreach (var value in matrix)
        {
            if (value > max)
            {
                max = value;
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

        if (peakPressureIndex >= AppConstants.PressureCriticalThreshold * 0.8)
        {
            return RiskLevel.High;
        }

        if (peakPressureIndex >= AppConstants.PressureCriticalThreshold * 0.5)
        {
            return RiskLevel.Medium;
        }

        return RiskLevel.Low;
    }
}

