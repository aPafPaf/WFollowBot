using GameHelper;
using GameHelper.RemoteEnums;

namespace WFollowBot;

public static class TerrainInfo
{
    private static string _lastAreaHash = string.Empty;

    public static float[][] GridHeightData = [];
    public static byte[] GridWalkableData = [];
    public static int[][] ProcessedTerrainData = [];
    private static int bytesPerRow = 0;

    public static int BytesPerRow { get => bytesPerRow; set => bytesPerRow = value; }

    public static void Update()
    {
        if (Core.States.GameCurrentState is not (GameStateTypes.InGameState or GameStateTypes.EscapeState))
        {
            return;
        }

        var areaInstance = GameHelper.Core.States.InGameStateObject.CurrentAreaInstance;
        var areaHash = areaInstance.AreaHash;
        if (areaHash == _lastAreaHash && ProcessedTerrainData.Length > 0)
        {
            return;
        }

        Reset();
        GridHeightData = areaInstance.GridHeightData;
        GridWalkableData = areaInstance.GridWalkableData;
        BytesPerRow = areaInstance.TerrainMetadata.BytesPerRow;
        ProcessedTerrainData = ComputeGridWalkableLookup();

        // Only save the hash when we actually have valid terrain data.
        // This prevents a permanent stall where an early read (before the game
        // engine has populated the terrain arrays) saves the hash but leaves
        // ProcessedTerrainData empty, causing all subsequent Update() calls
        // to skip re-reading.
        if (ProcessedTerrainData.Length > 0 && ProcessedTerrainData[0].Length > 0)
        {
            _lastAreaHash = areaHash;
        }
    }

    public static int WalkableCellCount()
    {
        if (ProcessedTerrainData.Length == 0)
            return 0;

        var count = 0;
        for (var y = 0; y < ProcessedTerrainData.Length; y++)
        {
            for (var x = 0; x < ProcessedTerrainData[y].Length; x++)
            {
                if (ProcessedTerrainData[y][x] != 0)
                    count++;
            }
        }

        return count;
    }

    public static int TotalCellCount()
    {
        if (ProcessedTerrainData.Length == 0)
            return 0;

        var count = 0;
        for (var y = 0; y < ProcessedTerrainData.Length; y++)
            count += ProcessedTerrainData[y].Length;

        return count;
    }

    public static int[][] ComputeGridWalkableLookup()
    {
        if (GridHeightData.Length == 0 || GridWalkableData.Length == 0 || bytesPerRow <= 0)
            return [];

        var rows = GridHeightData.Length;
        var cols = GridHeightData[0].Length;
        var result = new int[rows][];

        for (var y = 0; y < rows; y++)
        {
            result[y] = new int[cols];
            for (var x = 0; x < cols; x++)
            {
                var index = (y * bytesPerRow) + (x / 2);
                if (index >= GridWalkableData.Length)
                    continue;
                var shift = (x % 2 == 0) ? 0 : 4;
                result[y][x] = (GridWalkableData[index] >> shift) & 0xF;
            }
        }

        return result;
    }

    public static bool IsBorder(int x, int y)
    {
        var index = (y * bytesPerRow) + (x / 2); // (x / 2) => since there are 2 data points in 1 byte.
        var (oneIfFirstNibbleZeroIfNot, zeroIfFirstNibbleOneIfNot) = NibbleHandler(x);
        var shiftIfSecondNibble = zeroIfFirstNibbleOneIfNot * 0x4;

        var currentTile = GetTileValueAt(index, shiftIfSecondNibble);

        // we add the extra condition if currentTile != 1 to make the border thicker.
        if (currentTile != 1 && CanWalk(currentTile))
        {
            return false;
        }

        var upTile = GetTileValueAt(index + bytesPerRow, shiftIfSecondNibble);
        if (CanWalk(upTile))
        {
            return true;
        }

        var downTile = GetTileValueAt(index - bytesPerRow, shiftIfSecondNibble);
        if (CanWalk(downTile))
        {
            return true;
        }

        var shiftIfFirstNibble = oneIfFirstNibbleZeroIfNot * 0x4;
        var leftTile = GetTileValueAt(index - oneIfFirstNibbleZeroIfNot, shiftIfFirstNibble);
        if (CanWalk(leftTile))
        {
            return true;
        }

        var rightTile = GetTileValueAt(index + zeroIfFirstNibbleOneIfNot, shiftIfFirstNibble);
        return CanWalk(rightTile);
    }

    private static bool CanWalk(int tileValue)
        => tileValue != 0;

    private static (int oneIfFirstNibbleZeroIfNot, int zeroIfFirstNibbleOneIfNot) NibbleHandler(int x)
        => x % 2 == 0 ? (1, 0) : (0, 1);

    private static int GetTileValueAt(int index, int shiftAmount)
    {
        if ((uint)index >= (uint)GridWalkableData.Length)
        {
            return 0;
        }

        var data = GridWalkableData[index];
        return (data >> shiftAmount) & 0xF;
    }

    public static void Reset()
    {
        GridHeightData = [];
        GridWalkableData = [];
        ProcessedTerrainData = [];
        BytesPerRow = 0;
        _lastAreaHash = string.Empty;
    }
}
