
using UnityEngine;

public class HeightMapGenerator : MonoBehaviour {
    public int seed;
    public bool randomizeSeed;

    public int numOctaves = 7;
    public float persistence = .5f;
    public float lacunarity = 2;
    public float initialScale = 2;
    public int pow = 1;

    float rand(Vector2 uv)
    {
        var val = Mathf.Sin(Vector3.Dot(uv, new Vector2(12.9898f, 78.233f))) * 43758.5453123f;
        return val % 1;
    }

    float value_noise(Vector2 uv)
    {
        Vector2 ipos = new Vector2(Mathf.Floor(uv.x), Mathf.Floor(uv.y));
        Vector2 fpos = new Vector2(uv.x % 1, uv.y % 1);

        float o = rand(ipos);
        float x = rand(ipos + new Vector2(1, 0));
        float y = rand(ipos + new Vector2(0, 1));
        float xy = rand(ipos + new Vector2(1, 1));

        Vector2 smooth = new Vector2(Mathf.SmoothStep(0, 1, fpos.x), Mathf.SmoothStep(0, 1, fpos.y));
        return Mathf.Lerp(Mathf.Lerp(o, x, smooth.x),
                     Mathf.Lerp(y, xy, smooth.x), smooth.y);
    }

    float fractal_noise(Vector2 uv)
    {
        float n = 0;

        n = (1 / 2.0f) * value_noise(uv * 1);
        n += (1 / 4.0f) * value_noise(uv * 2);
        n += (1 / 8.0f) * value_noise(uv * 4);
        n += (1 / 16.0f) * value_noise(uv * 8);

        return n;
    }
    float rand2dTo1d(Vector2 value, Vector2 dotDir)
    {
        Vector2 smallValue = new Vector2(Mathf.Sin(value.x), Mathf.Sin(value.y));
        float random = Vector2.Dot(smallValue, dotDir);
        random = (Mathf.Sin(random) * 143758.5453f)%1;
        return random;
    }

    Vector2 rand2dTo2d(Vector2 value)
    {
        return new Vector2(
            rand2dTo1d(value, new Vector2(12.989f, 78.233f)),
            rand2dTo1d(value, new Vector2(39.346f, 11.135f))
        );
    }
    float voronoiNoise(Vector2 value)
    {
        Vector2 baseCell = new Vector2(Mathf.Floor(value.x), Mathf.Floor(value.y));

        float minDistToCell = 10;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2 cell = baseCell + new Vector2(x, y);
                Vector2 cellPosition = cell + rand2dTo2d(cell);
                Vector2 toCell = cellPosition - value;
                float distToCell = toCell.magnitude;
                if (distToCell < minDistToCell)
                {
                    minDistToCell = distToCell;
                }
            }
        }
        return minDistToCell;
    }

    public float[] Generate (Vector2Int mapSize) {
        //var map = new float[mapSize.x * mapSize.y];
        var map = new float[mapSize.y * mapSize.y];
        seed = (randomizeSeed) ? Random.Range (-10000, 10000) : seed;
        var prng = new System.Random (seed);

        Vector2[] offsets = new Vector2[numOctaves];
        for (int i = 0; i < numOctaves; i++) {
            offsets[i] = new Vector2 (prng.Next (-1000, 1000), prng.Next (-1000, 1000));
        }

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        //for (int y = 0; y < mapSize.x; y++) {
        for (int y = 0; y < mapSize.y; y++) {
            for (int x = 0; x < mapSize.y; x++) {
                float noiseValue = 0;
                float scale = initialScale;
                float weight = 1;
                for (int i = 0; i < numOctaves; i++) {
                    //Vector2 p = offsets[i] + new Vector2 (x / (float) mapSize.y, y / (float) mapSize.x) * scale;
                    //noiseValue += Mathf.PerlinNoise (p.x, p.y) * weight;
                    Vector2 p = offsets[i] + new Vector2(x / (float)mapSize.y, y / (float)mapSize.y) * scale;
                    noiseValue += Mathf.PerlinNoise(p.x, p.y) * weight;
                    weight *= persistence;
                    scale *= lacunarity;
                }
                map[y * mapSize.y + x] = Mathf.Pow(noiseValue, pow);
                minValue = Mathf.Min (noiseValue, minValue);
                maxValue = Mathf.Max (noiseValue, maxValue);

                //map[y * mapSize.y + x] = fractal_noise(new Vector2(x / (float)mapSize.y, y / (float)mapSize.y) * initialScale);

                //map[y * mapSize.y + x] = voronoiNoise(new Vector2(x / (float)mapSize.y, y / (float)mapSize.y) * initialScale);
                //map[y * mapSize.y + x] = Mathf.Pow(map[y * mapSize.y + x], pow);
            }
        }

        // Normalize
        if (maxValue != minValue) {
            for (int i = 0; i < map.Length; i++) {
                //map[i] = (map[i] - minValue) / (maxValue - minValue);
            }
        }

        return map;
    }
}