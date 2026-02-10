namespace Granite.Server.Services.Map;

/// <summary>
/// Box blur tool for hill-shading on map tiles.
/// </summary>
public static class BlurTool
{
    public static void Blur(byte[] data, int sizeX, int sizeZ, int range)
    {
        BoxBlurHorizontal(data, range, 0, 0, sizeX, sizeZ);
        BoxBlurVertical(data, range, 0, 0, sizeX, sizeZ);
    }

    private static void BoxBlurHorizontal(
        byte[] map,
        int range,
        int xStart,
        int yStart,
        int xEnd,
        int yEnd
    )
    {
        int width = xEnd - xStart;
        int halfRange = range / 2;
        int rowStart = yStart * width;
        byte[] temp = new byte[width];

        for (int y = yStart; y < yEnd; y++)
        {
            int count = 0;
            int sum = 0;

            for (int x = xStart - halfRange; x < xEnd; x++)
            {
                int removeIdx = x - halfRange - 1;
                if (removeIdx >= xStart)
                {
                    byte val = map[rowStart + removeIdx];
                    if (val != 0)
                    {
                        sum -= val;
                    }
                    count--;
                }

                int addIdx = x + halfRange;
                if (addIdx < xEnd)
                {
                    byte val = map[rowStart + addIdx];
                    if (val != 0)
                    {
                        sum += val;
                    }
                    count++;
                }

                if (x >= xStart)
                {
                    temp[x] = (byte)(sum / count);
                }
            }

            for (int x = xStart; x < xEnd; x++)
            {
                map[rowStart + x] = temp[x];
            }

            rowStart += width;
        }
    }

    private static void BoxBlurVertical(
        byte[] map,
        int range,
        int xStart,
        int yStart,
        int xEnd,
        int yEnd
    )
    {
        int width = xEnd - xStart;
        int height = yEnd - yStart;
        int halfRange = range / 2;
        byte[] temp = new byte[height];

        for (int x = xStart; x < xEnd; x++)
        {
            int count = 0;
            int sum = 0;
            int rowIdx = yStart * width - halfRange * width + x;

            for (int y = yStart - halfRange; y < yEnd; y++)
            {
                if (y - halfRange - 1 >= yStart)
                {
                    byte val = map[rowIdx + (-(halfRange + 1)) * width];
                    if (val != 0)
                    {
                        sum -= val;
                    }
                    count--;
                }

                if (y + halfRange < yEnd)
                {
                    byte val = map[rowIdx + halfRange * width];
                    if (val != 0)
                    {
                        sum += val;
                    }
                    count++;
                }

                if (y >= yStart)
                {
                    temp[y] = (byte)(sum / count);
                }

                rowIdx += width;
            }

            for (int y = yStart; y < yEnd; y++)
            {
                map[y * width + x] = temp[y];
            }
        }
    }
}
