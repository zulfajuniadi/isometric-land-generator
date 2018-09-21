using UnityEngine;

namespace TilemapGenerator
{
    public static class Utils
    {
        public static Mesh CreatePlane(float width, float length, bool createUV = true, int resX = 2, int resY = 2)
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[resX * resY];
            for (int y = 0; y < resY; y++)
            {
                float yPos = ((float) y / (resY - 1) - .5f) * length;
                for (int x = 0; x < resX; x++)
                {
                    float xPos = ((float) x / (resX - 1) - .5f) * width;
                    vertices[x + y * resX] = new Vector3(xPos, yPos, 0);
                }
            }

            int nbFaces = (resX - 1) * (resY - 1);
            int[] triangles = new int[nbFaces * 6];
            int t = 0;
            for (int face = 0; face < nbFaces; face++)
            {
                // Retrieve lower left corner from face ind
                int i = face % (resX - 1) + (face / (resY - 1) * resX);

                triangles[t++] = i + resX;
                triangles[t++] = i + 1;
                triangles[t++] = i;

                triangles[t++] = i + resX;
                triangles[t++] = i + resX + 1;
                triangles[t++] = i + 1;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;

            if (createUV)
            {
                Vector2[] uvs = new Vector2[vertices.Length];
                for (int v = 0; v < resY; v++)
                {
                    for (int u = 0; u < resX; u++)
                    {
                        uvs[u + v * resX] = new Vector2((float) u / (resX - 1), (float) v / (resY - 1));
                    }
                }
                mesh.uv = uvs;
            }

            mesh.RecalculateBounds();
            return mesh;
        }

    }
}
