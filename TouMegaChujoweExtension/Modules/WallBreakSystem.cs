using System.Collections;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Reactor.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Modules;

/// <summary>
/// A hole the Wrecker can open: it starts at <see cref="Origin"/> on the wall surface, runs along
/// <see cref="Direction"/> and comes out on walkable floor <see cref="Depth"/> units later.
/// </summary>
public readonly record struct WallBreach(Vector2 Origin, Vector2 Direction, float Depth);

/// <summary>
/// Finds walls the Wrecker may break and cuts holes through them.
/// A wall only counts when the floor behind it is floor players can already walk on, so the outer hull of a
/// map (with space, sky or nothing behind it) and the sealed gaps inside thick walls never open. Walkable
/// floor is a flood fill over a grid, started from the vents, ladders, spawn points and the Wrecker, because the
/// room areas of the maps spill over their walls and the space between rooms is enclosed just like a room is.
/// </summary>
public static class WallBreakSystem
{
    // World units. A player's body is a circle of about 0.156 (radius 0.223 at scale 0.7).
    private const float CellSize = 0.3f;
    private const float CellRadius = 0.17f;
    private const float Reach = 1f;
    private const int RayCount = 32;
    private const float HoleHalfWidth = 0.5f;
    private const float NearMargin = 0.4f;
    private const float FarMargin = 0.2f;
    private const float ExitDepth = 0.3f;

    // Breaking through must save at least this much walking; otherwise it is a pillar or a rock, not a wall.
    private const float MinDetour = 3f;
    private const int CellsPerFrame = 2500;

    // In front of the map art (depth 2-8) and behind the players (about 0).
    private const float RubbleZ = 0.5f;

    private static readonly Il2CppReferenceArray<Collider2D> OverlapBuffer = new(32);
    private static readonly Dictionary<int, bool> DoorCache = new();

    private static FloorGrid? _grid;
    private static ShipStatus? _building;
    private static int _blockMask = -1;
    private static int _shipLayer = -1;
    private static int _shadowLayer = -1;
    private static int _playersLayer = -1;
    private static Sprite? _rubbleSprite;

    private sealed class FloorGrid
    {
        public ShipStatus Ship = null!;
        public Vector2 Origin;
        public int Nx;
        public int Ny;
        public bool[] Free = null!;
        public bool[] Reached = null!;
        public int[] VisitStamp = null!;
        public int Stamp;

        public bool InRange(int i, int j) => i >= 0 && j >= 0 && i < Nx && j < Ny;

        public int Index(int i, int j) => j * Nx + i;

        public Vector2 Point(int i, int j) => new(Origin.x + i * CellSize, Origin.y + j * CellSize);

        public (int I, int J) Cell(Vector2 p) =>
            (Mathf.RoundToInt((p.x - Origin.x) / CellSize), Mathf.RoundToInt((p.y - Origin.y) / CellSize));
    }

    /// <summary>
    /// Starts mapping the walkable floor of the current map, a few thousand cells a frame, if that is not
    /// done or under way already.
    /// </summary>
    public static void Prepare()
    {
        var ship = ShipStatus.Instance;
        if (ship == null || (_grid != null && _grid.Ship == ship) || (_building != null && _building == ship))
        {
            return;
        }

        _building = ship;
        Coroutines.Start(CoBuildGrid(ship));
    }

    public static bool TryFindBreach(PlayerControl player, float maxThickness, out WallBreach breach)
    {
        breach = default;
        var grid = _grid;
        if (grid == null || grid.Ship != ShipStatus.Instance)
        {
            Prepare();
            return false;
        }

        EnsureLayers();
        var position = player.GetTruePosition();

        // Wherever the Wrecker stands is floor, even somewhere the first flood did not get to (a ladder top).
        if (IsStandingNormally(player) && !IsNearReached(grid, position))
        {
            SeedAt(grid, position);
        }

        // In a corner the closest wall may be the hull; the next one along may still lead somewhere.
        foreach (var (hitPoint, normal) in FindNearbyWalls(position))
        {
            if (TryBreachAt(grid, position, hitPoint, -normal.normalized, maxThickness, out breach))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryBreachAt(FloorGrid grid, Vector2 position, Vector2 hitPoint, Vector2 direction,
        float maxThickness, out WallBreach breach)
    {
        breach = default;
        var floorAt = -1f;
        for (var s = 0.15f; s <= maxThickness + ExitDepth; s += 0.1f)
        {
            var probe = hitPoint + direction * s;
            if (IsWalkable(grid, probe) && IsWalkable(grid, probe + direction * ExitDepth))
            {
                floorAt = s;
                break;
            }
        }

        if (floorAt < 0f)
        {
            return false;
        }

        var depth = floorAt + ExitDepth;
        var exit = hitPoint + direction * depth;
        if (CanWalkWithin(grid, position, exit, Vector2.Distance(position, exit) + MinDetour))
        {
            return false;
        }

        if (CorridorHasFixedObstacle(hitPoint, direction, depth))
        {
            return false;
        }

        breach = new WallBreach(hitPoint, direction, depth);
        return true;
    }

    /// <summary>
    /// Cuts the hole on this client. Every client runs it with the same numbers from the RPC.
    /// </summary>
    public static void ApplyBreach(Vector2 origin, Vector2 direction, float depth)
    {
        var ship = ShipStatus.Instance;
        if (ship == null || direction.sqrMagnitude < 0.25f)
        {
            return;
        }

        EnsureLayers();
        var corridor = new Corridor(origin, direction.normalized, -NearMargin, Mathf.Clamp(depth, 0.1f, 8f) + FarMargin,
            HoleHalfWidth);
        var mask = (1 << _shipLayer) | (1 << _shadowLayer);
        var found = Physics2D.OverlapBoxAll(corridor.Center, corridor.Size, corridor.AngleDegrees, mask);

        var shipSides = new SideCrossings();
        var shadowSides = new SideCrossings();
        var seen = new HashSet<int>();
        foreach (var collider in found)
        {
            if (collider == null || !seen.Add(collider.GetInstanceID()) || !IsCuttable(collider))
            {
                continue;
            }

            var loops = GetWorldLoops(collider);
            if (loops.Count == 0)
            {
                continue;
            }

            var sides = collider.gameObject.layer == _shipLayer ? shipSides : shadowSides;
            foreach (var (points, closed) in loops)
            {
                corridor.AddSideCrossings(points, closed, sides);
            }

            CutCollider(collider, loops, corridor);
        }

        // The cut opens the sealed space inside a thick wall onto the hole; close the hole's sides off again.
        AddSideWalls(ship, corridor, shipSides, _shipLayer, "WreckerBreachWalls");
        AddSideWalls(ship, corridor, shadowSides, _shadowLayer, "WreckerBreachShadows");
        SpawnRubble(ship, origin, direction.normalized, depth);
    }

    private static IEnumerator CoBuildGrid(ShipStatus ship)
    {
        EnsureLayers();
        DoorCache.Clear();

        var bounds = GetShipBounds(ship);
        var grid = new FloorGrid
        {
            Ship = ship,
            Origin = new Vector2(bounds.xMin, bounds.yMin),
            Nx = Mathf.Max(1, Mathf.CeilToInt(bounds.width / CellSize) + 1),
            Ny = Mathf.Max(1, Mathf.CeilToInt(bounds.height / CellSize) + 1),
        };
        var count = grid.Nx * grid.Ny;
        grid.Free = new bool[count];
        grid.Reached = new bool[count];
        grid.VisitStamp = new int[count];

        var done = 0;
        for (var j = 0; j < grid.Ny; j++)
        {
            for (var i = 0; i < grid.Nx; i++)
            {
                grid.Free[grid.Index(i, j)] = IsClear(grid.Point(i, j), CellRadius);
            }

            done += grid.Nx;
            if (done < CellsPerFrame)
            {
                continue;
            }

            done = 0;
            yield return null;
            if (ship == null || ShipStatus.Instance != ship)
            {
                _building = null;
                yield break;
            }
        }

        foreach (var seed in GetSeeds(ship))
        {
            SeedAt(grid, seed);
        }

        _grid = grid;
        _building = null;
    }

    private static IEnumerable<Vector2> GetSeeds(ShipStatus ship)
    {
        var seeds = new List<Vector2>();
        if (ship.AllVents != null)
        {
            foreach (var vent in ship.AllVents)
            {
                if (vent != null)
                {
                    seeds.Add(vent.transform.position);
                }
            }
        }

        if (ship.Ladders != null)
        {
            foreach (var ladder in ship.Ladders)
            {
                if (ladder != null)
                {
                    seeds.Add(ladder.transform.position);
                }
            }
        }

        // Unused spawn centres are left at zero, which can be outside the map.
        foreach (var spawn in new[] { ship.InitialSpawnCenter, ship.MeetingSpawnCenter, ship.MeetingSpawnCenter2 })
        {
            if (spawn.sqrMagnitude > 0.01f)
            {
                seeds.Add(spawn);
            }
        }

        // Players are left out: during the intro (Airship's spawn choice) they can be parked off the map.
        // The Wrecker's own spot is added once it is standing somewhere (TryFindBreach).
        return seeds;
    }

    private static bool IsStandingNormally(PlayerControl? player)
    {
        // A player phasing, in a vent or on a ladder may be inside a wall; their spot proves nothing.
        return player != null && player.Data != null && !player.Data.IsDead && !player.inVent &&
               !player.walkingToVent && !player.onLadder && !player.inMovingPlat &&
               player.gameObject.layer == _playersLayer && player.Collider != null && player.Collider.enabled;
    }

    private static void SeedAt(FloorGrid grid, Vector2 point)
    {
        var start = FindSeedCell(grid, point);
        if (start < 0 || grid.Reached[start])
        {
            return;
        }

        var queue = new Queue<int>();
        grid.Reached[start] = true;
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var index = queue.Dequeue();
            var i = index % grid.Nx;
            var j = index / grid.Nx;
            TryReach(grid, queue, i + 1, j);
            TryReach(grid, queue, i - 1, j);
            TryReach(grid, queue, i, j + 1);
            TryReach(grid, queue, i, j - 1);
        }
    }

    private static void TryReach(FloorGrid grid, Queue<int> queue, int i, int j)
    {
        if (!grid.InRange(i, j))
        {
            return;
        }

        var index = grid.Index(i, j);
        if (!grid.Free[index] || grid.Reached[index])
        {
            return;
        }

        grid.Reached[index] = true;
        queue.Enqueue(index);
    }

    /// <summary>
    /// The free cell closest to a seed point that the point can see; a seed beside a wall may not land on
    /// the far side of it.
    /// </summary>
    private static int FindSeedCell(FloorGrid grid, Vector2 point)
    {
        var (ci, cj) = grid.Cell(point);
        const int radius = 4;
        var candidates = new List<(int Distance, int I, int J)>();
        for (var j = cj - radius; j <= cj + radius; j++)
        {
            for (var i = ci - radius; i <= ci + radius; i++)
            {
                if (grid.InRange(i, j) && grid.Free[grid.Index(i, j)])
                {
                    candidates.Add(((i - ci) * (i - ci) + (j - cj) * (j - cj), i, j));
                }
            }
        }

        foreach (var (_, i, j) in candidates.OrderBy(c => c.Distance))
        {
            if (IsLineClear(point, grid.Point(i, j)))
            {
                return grid.Index(i, j);
            }
        }

        return -1;
    }

    private static bool IsNearReached(FloorGrid grid, Vector2 point)
    {
        var (ci, cj) = grid.Cell(point);
        for (var j = cj - 1; j <= cj + 1; j++)
        {
            for (var i = ci - 1; i <= ci + 1; i++)
            {
                if (IsOpen(grid, i, j))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsWalkable(FloorGrid grid, Vector2 point)
    {
        var fi = Mathf.FloorToInt((point.x - grid.Origin.x) / CellSize);
        var fj = Mathf.FloorToInt((point.y - grid.Origin.y) / CellSize);
        var reached = false;
        for (var dj = 0; dj <= 1 && !reached; dj++)
        {
            for (var di = 0; di <= 1; di++)
            {
                if (grid.InRange(fi + di, fj + dj) && grid.Reached[grid.Index(fi + di, fj + dj)])
                {
                    reached = true;
                    break;
                }
            }
        }

        return reached && IsClear(point, CellRadius);
    }

    /// <summary>
    /// Whether a player could walk from <paramref name="from"/> to <paramref name="to"/> in at most
    /// <paramref name="limit"/> units without the hole.
    /// </summary>
    private static bool CanWalkWithin(FloorGrid grid, Vector2 from, Vector2 to, float limit)
    {
        var start = NearestReached(grid, from);
        var goal = NearestReached(grid, to);
        if (start < 0 || goal < 0)
        {
            return false;
        }

        var maxSteps = Mathf.CeilToInt(limit / CellSize);
        grid.Stamp++;
        var queue = new Queue<(int Index, int Steps)>();
        grid.VisitStamp[start] = grid.Stamp;
        queue.Enqueue((start, 0));
        while (queue.Count > 0)
        {
            var (index, steps) = queue.Dequeue();
            if (index == goal)
            {
                return true;
            }

            if (steps >= maxSteps)
            {
                continue;
            }

            var i = index % grid.Nx;
            var j = index / grid.Nx;
            for (var dj = -1; dj <= 1; dj++)
            {
                for (var di = -1; di <= 1; di++)
                {
                    if ((di == 0 && dj == 0) || !IsOpen(grid, i + di, j + dj))
                    {
                        continue;
                    }

                    // A diagonal step may not squeeze past a corner.
                    if (di != 0 && dj != 0 && (!IsOpen(grid, i + di, j) || !IsOpen(grid, i, j + dj)))
                    {
                        continue;
                    }

                    var next = grid.Index(i + di, j + dj);
                    if (grid.VisitStamp[next] == grid.Stamp)
                    {
                        continue;
                    }

                    grid.VisitStamp[next] = grid.Stamp;
                    queue.Enqueue((next, steps + 1));
                }
            }
        }

        return false;
    }

    private static bool IsOpen(FloorGrid grid, int i, int j) => grid.InRange(i, j) && grid.Reached[grid.Index(i, j)];

    private static int NearestReached(FloorGrid grid, Vector2 point)
    {
        var (ci, cj) = grid.Cell(point);
        var best = -1;
        var bestDistance = int.MaxValue;
        for (var j = cj - 2; j <= cj + 2; j++)
        {
            for (var i = ci - 2; i <= ci + 2; i++)
            {
                if (!IsOpen(grid, i, j))
                {
                    continue;
                }

                var distance = (i - ci) * (i - ci) + (j - cj) * (j - cj);
                if (distance < bestDistance)
                {
                    best = grid.Index(i, j);
                    bestDistance = distance;
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Wall surfaces within reach, closest first, one per facing (rays that hit the same wall head-on merge).
    /// </summary>
    private static List<(Vector2 Point, Vector2 Normal)> FindNearbyWalls(Vector2 position)
    {
        var found = new List<(float Distance, Vector2 Point, Vector2 Normal)>();
        for (var k = 0; k < RayCount; k++)
        {
            var angle = k * Mathf.PI * 2f / RayCount;
            var ray = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var hits = Physics2D.RaycastAll(position, ray, Reach, _blockMask);

            // Only the first solid thing along the ray counts, and it has to be a wall.
            Collider2D? firstCollider = null;
            var firstDistance = float.MaxValue;
            var firstPoint = Vector2.zero;
            var firstNormal = Vector2.zero;
            foreach (var hit in hits)
            {
                var collider = hit.collider;
                if (collider == null || collider.isTrigger || hit.distance <= 0.001f || IsDoor(collider) ||
                    hit.distance >= firstDistance)
                {
                    continue;
                }

                firstCollider = collider;
                firstDistance = hit.distance;
                firstPoint = hit.point;
                firstNormal = hit.normal;
            }

            if (firstCollider == null || firstCollider.gameObject.layer != _shipLayer ||
                !IsCuttable(firstCollider) || firstNormal.sqrMagnitude < 0.25f)
            {
                continue;
            }

            found.Add((firstDistance, firstPoint, firstNormal.normalized));
        }

        var walls = new List<(Vector2, Vector2)>();
        foreach (var (_, point, normal) in found.OrderBy(h => h.Distance))
        {
            if (walls.Count >= 4)
            {
                break;
            }

            if (walls.Any(w => Vector2.Dot(w.Item2, normal) > 0.95f))
            {
                continue;
            }

            walls.Add((point, normal));
        }

        return walls;
    }

    private static bool CorridorHasFixedObstacle(Vector2 origin, Vector2 direction, float depth)
    {
        var corridor = new Corridor(origin, direction, -0.1f, depth, HoleHalfWidth - 0.1f);
        foreach (var collider in Physics2D.OverlapBoxAll(corridor.Center, corridor.Size, corridor.AngleDegrees, _blockMask))
        {
            if (collider == null || collider.isTrigger || IsDoor(collider))
            {
                continue;
            }

            if (!IsCuttable(collider))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsClear(Vector2 point, float radius)
    {
        var count = Physics2D.OverlapCircleNonAlloc(point, radius, OverlapBuffer, _blockMask);
        for (var k = 0; k < count; k++)
        {
            var collider = OverlapBuffer[k];
            if (collider != null && !collider.isTrigger && !IsDoor(collider))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsLineClear(Vector2 from, Vector2 to)
    {
        if ((to - from).sqrMagnitude < 0.0001f)
        {
            return true;
        }

        foreach (var hit in Physics2D.LinecastAll(from, to, _blockMask))
        {
            var collider = hit.collider;
            if (collider != null && !collider.isTrigger && !IsDoor(collider))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsDoor(Collider2D collider)
    {
        var id = collider.GetInstanceID();
        if (DoorCache.TryGetValue(id, out var isDoor))
        {
            return isDoor;
        }

        // Doors open and close (Airship's electrical doors are static walls that move); they are never cut and
        // never count as walls. Room objects carry door systems too, so only real door parts match.
        isDoor = collider.GetComponentInParent<SomeKindaDoor>() != null ||
                 collider.GetComponentInParent<StaticDoor>() != null ||
                 collider.GetComponentInParent<DoorConsole>() != null ||
                 collider.GetComponentInParent<OpenDoorConsole>() != null;
        DoorCache[id] = isDoor;
        return isDoor;
    }

    private static bool IsCuttable(Collider2D collider)
    {
        if (collider.isTrigger || !collider.enabled || collider.usedByComposite)
        {
            return false;
        }

        var layer = collider.gameObject.layer;
        if (layer != _shipLayer && layer != _shadowLayer)
        {
            return false;
        }

        if (collider.TryCast<EdgeCollider2D>() == null && collider.TryCast<PolygonCollider2D>() == null &&
            collider.TryCast<BoxCollider2D>() == null)
        {
            return false;
        }

        return !IsDoor(collider) && collider.GetComponent<NoShadowBehaviour>() == null;
    }

    private static Rect GetShipBounds(ShipStatus ship)
    {
        var any = false;
        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        foreach (var collider in ship.GetComponentsInChildren<Collider2D>())
        {
            if (collider == null || collider.isTrigger || collider.gameObject.layer != _shipLayer)
            {
                continue;
            }

            var b = collider.bounds;
            min = Vector2.Min(min, b.min);
            max = Vector2.Max(max, b.max);
            any = true;
        }

        if (!any)
        {
            return new Rect(-40f, -40f, 80f, 80f);
        }

        return Rect.MinMaxRect(min.x - 1.5f, min.y - 1.5f, max.x + 1.5f, max.y + 1.5f);
    }

    private static void EnsureLayers()
    {
        if (_blockMask != -1)
        {
            return;
        }

        _playersLayer = LayerMask.NameToLayer("Players");
        _shipLayer = LayerMask.NameToLayer("Ship");
        _shadowLayer = LayerMask.NameToLayer("Shadow");

        // Everything a player's body bumps into.
        var mask = 0;
        try
        {
            mask = Physics2D.GetLayerCollisionMask(_playersLayer);
        }
        catch
        {
            // Older physics settings API; fall back to the ship's usual blocking layers.
        }

        if (mask == 0)
        {
            foreach (var name in new[] { "Default", "Ship", "Objects", "ShortObjects", "IlluminatedBlocking" })
            {
                var layer = LayerMask.NameToLayer(name);
                if (layer >= 0)
                {
                    mask |= 1 << layer;
                }
            }
        }

        mask &= ~(1 << _playersLayer);
        var ghost = LayerMask.NameToLayer("Ghost");
        if (ghost >= 0)
        {
            mask &= ~(1 << ghost);
        }

        _blockMask = mask;
    }

    // Cutting -----------------------------------------------------------------------------------------------

    private static List<(List<Vector2> Points, bool Closed)> GetWorldLoops(Collider2D collider)
    {
        var loops = new List<(List<Vector2>, bool)>();
        var transform = collider.transform;
        var offset = collider.offset;

        var edge = collider.TryCast<EdgeCollider2D>();
        if (edge != null)
        {
            var points = edge.points;
            var world = new List<Vector2>(points.Length);
            for (var k = 0; k < points.Length; k++)
            {
                world.Add(transform.TransformPoint(points[k] + offset));
            }

            if (world.Count >= 2)
            {
                loops.Add((world, false));
            }

            return loops;
        }

        var polygon = collider.TryCast<PolygonCollider2D>();
        if (polygon != null)
        {
            for (var p = 0; p < polygon.pathCount; p++)
            {
                var path = polygon.GetPath(p);
                var world = new List<Vector2>(path.Length);
                for (var k = 0; k < path.Length; k++)
                {
                    world.Add(transform.TransformPoint(path[k] + offset));
                }

                if (world.Count >= 3)
                {
                    loops.Add((world, true));
                }
            }

            return loops;
        }

        var box = collider.TryCast<BoxCollider2D>();
        if (box != null)
        {
            var half = box.size / 2f;
            var corners = new List<Vector2>
            {
                transform.TransformPoint(offset + new Vector2(-half.x, -half.y)),
                transform.TransformPoint(offset + new Vector2(half.x, -half.y)),
                transform.TransformPoint(offset + new Vector2(half.x, half.y)),
                transform.TransformPoint(offset + new Vector2(-half.x, half.y)),
            };
            loops.Add((corners, true));
        }

        return loops;
    }

    private static void CutCollider(Collider2D collider, List<(List<Vector2> Points, bool Closed)> loops, Corridor corridor)
    {
        var pieces = new List<List<Vector2>>();
        var changed = false;
        foreach (var (points, closed) in loops)
        {
            changed |= closed
                ? corridor.ClipClosed(points, pieces)
                : corridor.ClipOpen(points, pieces);
        }

        if (!changed)
        {
            return;
        }

        var edge = collider.TryCast<EdgeCollider2D>();
        var radius = edge != null ? edge.edgeRadius : collider.TryCast<BoxCollider2D>()?.edgeRadius ?? 0f;
        var transform = collider.transform;
        var offset = collider.offset;
        var material = collider.sharedMaterial;

        // An edge keeps its first piece; a polygon or box becomes edges, since a solid shape cannot have a hole.
        var first = 0;
        if (edge != null)
        {
            if (pieces.Count == 0)
            {
                edge.enabled = false;
                return;
            }

            edge.useAdjacentStartPoint = false;
            edge.useAdjacentEndPoint = false;
            edge.points = ToLocal(pieces[0], transform, offset);
            first = 1;
        }
        else
        {
            collider.enabled = false;
        }

        for (var k = first; k < pieces.Count; k++)
        {
            var extra = collider.gameObject.AddComponent<EdgeCollider2D>();
            extra.offset = offset;
            extra.edgeRadius = radius;
            extra.sharedMaterial = material;
            extra.points = ToLocal(pieces[k], transform, offset);
        }
    }

    private static Vector2[] ToLocal(List<Vector2> world, Transform transform, Vector2 offset)
    {
        var local = new Vector2[world.Count];
        for (var k = 0; k < world.Count; k++)
        {
            local[k] = (Vector2)transform.InverseTransformPoint(world[k]) - offset;
        }

        return local;
    }

    private static void AddSideWalls(ShipStatus ship, Corridor corridor, SideCrossings sides, int layer, string name)
    {
        var walls = new List<(Vector2 From, Vector2 To)>();
        AddSideWall(corridor, sides.Left, corridor.HalfWidth, walls);
        AddSideWall(corridor, sides.Right, -corridor.HalfWidth, walls);
        if (walls.Count == 0)
        {
            return;
        }

        var holder = new GameObject(name)
        {
            layer = layer,
        };
        holder.transform.SetParent(ship.transform, true);
        foreach (var (from, to) in walls)
        {
            var edge = holder.AddComponent<EdgeCollider2D>();
            edge.points = new Vector2[]
            {
                holder.transform.InverseTransformPoint(from),
                holder.transform.InverseTransformPoint(to),
            };
        }
    }

    private static void AddSideWall(Corridor corridor, List<float> crossings, float side, List<(Vector2, Vector2)> walls)
    {
        // A single crossing is a thin wall: nothing to seal. Two or more span the wall's thickness.
        if (crossings.Count < 2)
        {
            return;
        }

        var from = crossings.Min();
        var to = crossings.Max();
        if (to - from > 0.05f)
        {
            walls.Add((corridor.ToWorld(from, side), corridor.ToWorld(to, side)));
        }
    }

    private static void SpawnRubble(ShipStatus ship, Vector2 origin, Vector2 direction, float depth)
    {
        var length = depth + 0.1f;
        var center = origin + direction * (depth / 2f);
        var rubble = new GameObject("WreckerRubble");
        rubble.transform.SetParent(ship.transform, true);
        rubble.transform.position = new Vector3(center.x, center.y, RubbleZ);
        rubble.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        rubble.layer = ship.gameObject.layer;

        var parentScale = ship.transform.lossyScale;
        rubble.transform.localScale = new Vector3(
            length / SafeScale(parentScale.x),
            HoleHalfWidth * 2f / 0.5f / SafeScale(parentScale.y),
            1f);

        var renderer = rubble.AddComponent<SpriteRenderer>();
        renderer.sprite = GetRubbleSprite();
    }

    private static float SafeScale(float value) => Mathf.Abs(value) < 0.0001f ? 1f : value;

    /// <summary>
    /// Placeholder broken-wall look: dark rubble with ragged long edges, 1 x 0.5 units before scaling.
    /// </summary>
    private static Sprite GetRubbleSprite()
    {
        if (_rubbleSprite != null)
        {
            return _rubbleSprite;
        }

        const int width = 64;
        const int height = 32;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        var random = new System.Random(5150);
        var edge = new float[width];
        for (var x = 0; x < width; x++)
        {
            edge[x] = 2f + (float)random.NextDouble() * 4f;
        }

        var pixels = new Color[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var fromEdge = Mathf.Min(y, height - 1 - y);
                var alpha = Mathf.Clamp01((fromEdge - edge[x] + 2f) / 2f) * 0.92f;
                var shade = 0.16f + (float)random.NextDouble() * 0.1f;
                if (random.NextDouble() < 0.08)
                {
                    shade += 0.12f;
                }

                pixels[y * width + x] = new Color(shade * 1.05f, shade, shade * 0.92f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        _rubbleSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), width);
        _rubbleSprite.hideFlags = HideFlags.HideAndDontSave;
        return _rubbleSprite;
    }

    private sealed class SideCrossings
    {
        public readonly List<float> Left = [];
        public readonly List<float> Right = [];
    }

    /// <summary>
    /// The hole: a rectangle along <see cref="Axis"/> from <see cref="Start"/> to <see cref="End"/>, spanning
    /// <see cref="HalfWidth"/> to each side.
    /// </summary>
    private readonly struct Corridor
    {
        private const float Epsilon = 1e-5f;

        public readonly Vector2 Origin;
        public readonly Vector2 Axis;
        public readonly Vector2 Side;
        public readonly float Start;
        public readonly float End;
        public readonly float HalfWidth;

        public Corridor(Vector2 origin, Vector2 axis, float start, float end, float halfWidth)
        {
            Origin = origin;
            Axis = axis;
            Side = new Vector2(-axis.y, axis.x);
            Start = start;
            End = end;
            HalfWidth = halfWidth;
        }

        public Vector2 Center => ToWorld((Start + End) / 2f, 0f);

        public Vector2 Size => new(End - Start, HalfWidth * 2f);

        public float AngleDegrees => Mathf.Atan2(Axis.y, Axis.x) * Mathf.Rad2Deg;

        public Vector2 ToWorld(float along, float across) => Origin + Axis * along + Side * across;

        private Vector2 ToLocal(Vector2 world)
        {
            var d = world - Origin;
            return new Vector2(Vector2.Dot(d, Axis), Vector2.Dot(d, Side));
        }

        private bool Inside(Vector2 world)
        {
            var l = ToLocal(world);
            return l.x > Start && l.x < End && l.y > -HalfWidth && l.y < HalfWidth;
        }

        /// <summary>
        /// The part of segment a-b inside the rectangle, as segment parameters (Liang-Barsky).
        /// </summary>
        private bool ClipSegment(Vector2 a, Vector2 b, out float enter, out float exit)
        {
            enter = 0f;
            exit = 1f;
            var la = ToLocal(a);
            var lb = ToLocal(b);
            var ds = lb.x - la.x;
            var dt = lb.y - la.y;
            var p = new[] { -ds, ds, -dt, dt };
            var q = new[] { la.x - Start, End - la.x, la.y + HalfWidth, HalfWidth - la.y };
            for (var k = 0; k < 4; k++)
            {
                if (Mathf.Abs(p[k]) < Epsilon)
                {
                    if (q[k] < 0f)
                    {
                        return false;
                    }

                    continue;
                }

                var r = q[k] / p[k];
                if (p[k] < 0f)
                {
                    if (r > exit)
                    {
                        return false;
                    }

                    enter = Mathf.Max(enter, r);
                }
                else
                {
                    if (r < enter)
                    {
                        return false;
                    }

                    exit = Mathf.Min(exit, r);
                }
            }

            return exit - enter > Epsilon;
        }

        /// <summary>
        /// Splits an open polyline into the pieces outside the rectangle. Returns whether anything was cut.
        /// </summary>
        public bool ClipOpen(List<Vector2> points, List<List<Vector2>> pieces)
        {
            var changed = false;
            List<Vector2>? current = null;
            for (var k = 0; k + 1 < points.Count; k++)
            {
                var a = points[k];
                var b = points[k + 1];
                if (!ClipSegment(a, b, out var enter, out var exit))
                {
                    current ??= [a];
                    current.Add(b);
                    continue;
                }

                changed = true;
                if (enter > Epsilon)
                {
                    current ??= [a];
                    current.Add(Vector2.Lerp(a, b, enter));
                }

                AddPiece(current, pieces);
                current = null;
                if (exit < 1f - Epsilon)
                {
                    current = [Vector2.Lerp(a, b, exit), b];
                }
            }

            if (!changed)
            {
                // Untouched: keep it as one piece so a polygon's other paths survive.
                pieces.Add([.. points]);
                return false;
            }

            AddPiece(current, pieces);
            return true;
        }

        /// <summary>
        /// The same for a closed loop: it is opened at a corner outside the rectangle, so the cut leaves one
        /// piece per gap instead of a seam at the loop's start.
        /// </summary>
        public bool ClipClosed(List<Vector2> points, List<List<Vector2>> pieces)
        {
            var startAt = -1;
            for (var k = 0; k < points.Count; k++)
            {
                if (!Inside(points[k]))
                {
                    startAt = k;
                    break;
                }
            }

            if (startAt < 0)
            {
                // Every corner is inside the (convex) rectangle, so the whole loop goes.
                return true;
            }

            var loop = new List<Vector2>(points.Count + 1);
            for (var k = 0; k < points.Count; k++)
            {
                loop.Add(points[(startAt + k) % points.Count]);
            }

            loop.Add(points[startAt]);
            return ClipOpen(loop, pieces);
        }

        private static void AddPiece(List<Vector2>? piece, List<List<Vector2>> pieces)
        {
            if (piece == null || piece.Count < 2)
            {
                return;
            }

            var length = 0f;
            for (var k = 0; k + 1 < piece.Count; k++)
            {
                length += Vector2.Distance(piece[k], piece[k + 1]);
            }

            if (length > 0.01f)
            {
                pieces.Add(piece);
            }
        }

        public void AddSideCrossings(List<Vector2> points, bool closed, SideCrossings sides)
        {
            var count = closed ? points.Count : points.Count - 1;
            for (var k = 0; k < count; k++)
            {
                var a = ToLocal(points[k]);
                var b = ToLocal(points[(k + 1) % points.Count]);
                AddCrossing(a, b, HalfWidth, sides.Left);
                AddCrossing(a, b, -HalfWidth, sides.Right);
            }
        }

        private void AddCrossing(Vector2 a, Vector2 b, float side, List<float> crossings)
        {
            if ((a.y - side) * (b.y - side) > 0f || Mathf.Abs(b.y - a.y) < Epsilon)
            {
                return;
            }

            var f = (side - a.y) / (b.y - a.y);
            var along = a.x + f * (b.x - a.x);
            if (along >= Start && along <= End)
            {
                crossings.Add(along);
            }
        }
    }
}
