using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace AAEmu.Game.Core.Managers.World;

public static class Terrain
{
    [Pure]
    public static TriangleMesh CreateMesh(float[,] heights, float scaleX, float scaleZ)
    {
        var heightsLength0 = heights.GetLength(0);
        var heightsLength1 = heights.GetLength(1);

        var triangleList = new List<JTriangle>();

        for (var index = 0; index < (heightsLength0 - 1) * (heightsLength1 - 1); index++)
        {
            var quadIndexX = index % (heightsLength0 - 1);
            var quadIndexZ = index / (heightsLength0 - 1);

            triangleList.Add(new JTriangle(
                new JVector((0 + quadIndexX + 0) * scaleX, heights[0 + quadIndexX + 0, 0 + quadIndexZ + 0],
                    (0 + quadIndexZ + 0) * scaleZ),
                new JVector((0 + quadIndexX + 1) * scaleX, heights[0 + quadIndexX + 1, 0 + quadIndexZ + 0],
                    (0 + quadIndexZ + 0) * scaleZ),
                new JVector((0 + quadIndexX + 0) * scaleX, heights[0 + quadIndexX + 0, 0 + quadIndexZ + 1],
                    (0 + quadIndexZ + 1) * scaleZ)
            ));

            triangleList.Add(new JTriangle(
                new JVector((0 + quadIndexX + 1) * scaleX, heights[0 + quadIndexX + 1, 0 + quadIndexZ + 0],
                    (0 + quadIndexZ + 0) * scaleZ),
                new JVector((0 + quadIndexX + 1) * scaleX, heights[0 + quadIndexX + 1, 0 + quadIndexZ + 1],
                    (0 + quadIndexZ + 1) * scaleZ),
                new JVector((0 + quadIndexX + 0) * scaleX, heights[0 + quadIndexX + 0, 0 + quadIndexZ + 1],
                    (0 + quadIndexZ + 1) * scaleZ)
            ));
        }

        return new TriangleMesh(triangleList);
    }

    [Pure]
    public static IEnumerable<RigidBodyShape> CreateShapes(TriangleMesh triangleMesh)
    {
        for (var i = 0; i < triangleMesh.Indices.Length; i++)
        {
            yield return new FatTriangleShape(triangleMesh, i);
        }
    }

    /// <summary>
    /// Adds terrain to the world based on a heightmap, returning the <see cref="RigidBody"/> that was added.
    /// </summary>
    /// <param name="world">The world to add the terrain to.</param>
    /// <param name="heights">The heightmap data of the terrain, which is the heights of the terrain surface.</param>
    /// <param name="scaleX">The x-scale factor. (The x-space between neighbour heights).</param>
    /// <param name="scaleZ">The y-scale factor. (The y-space between neighbour heights).</param>
    /// <returns>The <see cref="RigidBody"/> that represents the terrain.</returns>
    public static RigidBody AddTerrain(this Jitter2.World world, float[,] heights, float scaleX, float scaleZ)
    {
        var terrain = world.CreateRigidBody();
        terrain.AddShape(CreateShapes(CreateMesh(heights, scaleX, scaleZ)), false);
        terrain.Position = JVector.Zero;
        terrain.IsStatic = true;

        return terrain;
    }
}
