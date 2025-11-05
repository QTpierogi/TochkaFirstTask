using System;
using System.Collections.Generic;
using System.Linq;

class Run2
{
    private static Dictionary<char, List<char>> graph;
    private static HashSet<char> secureRooms;
    private static HashSet<char> regularRooms;
    private static HashSet<(char, char)> lockedCorridors;

    static List<string> Solve(List<(string, string)> edges)
    {
        List<string> result = new List<string>();
        lockedCorridors = new HashSet<(char, char)>();

        char currentPosition = 'a';
        int maxTurns = 1000;

        for (int turn = 0; turn < maxTurns; turn++)
        {
            var lockableCorridors = FindLockableCorridors();
            if (lockableCorridors.Count == 0)
                return result;

            var (target, path) = FindVirusTargetAndPath(currentPosition);
            if (target == '[')
                return result;

            var bestCorridor = FindBestCorridorToLock(lockableCorridors, path, currentPosition);
            if (bestCorridor == ('\0', '\0'))
                bestCorridor = lockableCorridors[0];

            lockedCorridors.Add(bestCorridor);
            result.Add(bestCorridor.Item1 + "-" + bestCorridor.Item2);

            currentPosition = MoveVirus(currentPosition);
        }
        return result;
    }

    static void AddEdge(char u, char v)
    {
        if (!graph.ContainsKey(u))
            graph[u] = new List<char>();
        graph[u].Add(v);
        if (char.IsUpper(u))
            secureRooms.Add(u);
        else regularRooms.Add(u);
    }

    static void Main()
    {
        regularRooms = new HashSet<char>();
        secureRooms = new HashSet<char>();
        graph = new Dictionary<char, List<char>>();
        var edges = new List<(string, string)>();
        string line;
        while (true)
        {
            line = Console.ReadLine();
            if (string.IsNullOrEmpty(line)) break;
            line = line.Trim();
            var parts = line.Split('-');
            if (parts.Length == 2)
            {
                edges.Add((parts[0], parts[1]));
                char node1 = parts[0][0];
                char node2 = parts[1][0];

                AddEdge(node1, node2);
                AddEdge(node2, node1);
            }
        }
        var result = Solve(edges);
        foreach (var edge in result)
        {
            Console.WriteLine(edge);
        }
    }

    private static List<(char, char)> FindLockableCorridors()
    {
        var lockable = new List<(char, char)>();
        foreach (char room in secureRooms)
        {
            foreach (char neighbor in graph[room])
            {
                if (regularRooms.Contains(neighbor) && !lockedCorridors.Contains((room, neighbor)) &&
                    !lockedCorridors.Contains((neighbor, room)))
                    lockable.Add((room, neighbor));
            }
        }
        return lockable.OrderBy(c => c.Item1).ThenBy(c => c.Item2).ToList();
    }

    private static (char target, List<char> path) FindVirusTargetAndPath(char currentPosition)
    {
        char bestTarget = '[';
        List<char> bestPath = null;
        int minDistance = 100;
        List<List<char>> paths = new List<List<char>>();

        foreach (char target in secureRooms)
        {
            var path = FindShortestPath(currentPosition, target);
            if (path != null)
            {
                int distance = path.Count - 1;
                if (distance < minDistance ||
                    (distance == minDistance && target < bestTarget))
                {
                    minDistance = distance;
                    bestTarget = target;
                    paths.Add(path);
                }
            }
        }
        if (paths.Count > 0)
        {
            var orderedPaths = paths.Where(p => p.Count() - 1 == minDistance && p.Last() == bestTarget).Order();
            bestPath = orderedPaths.First();
        }
        return (bestTarget, bestPath);
    }

    private static List<char> FindShortestPath(char start, char end)
    {
        var queue = new Queue<char>();
        var visited = new HashSet<char>();
        var parent = new Dictionary<char, char>();

        queue.Enqueue(start);
        visited.Add(start);
        parent[start] = '\0';

        while (queue.Count > 0)
        {
            char current = queue.Dequeue();

            if (current == end)
            {
                var path = new List<char>();
                char node = end;
                while (node != '\0')
                {
                    path.Add(node);
                    node = parent[node];
                }
                path.Reverse();
                return path;
            }

            var neighbors = new List<char>(graph[current]);
            neighbors.Sort();

            foreach (char neighbor in neighbors)
            {
                if (visited.Contains(neighbor) ||
                    lockedCorridors.Contains((current, neighbor)) ||
                    lockedCorridors.Contains((neighbor, current)))
                    continue;

                visited.Add(neighbor);
                parent[neighbor] = current;
                queue.Enqueue(neighbor);
            }
        }
        return null;
    }


    private static (char, char) FindBestCorridorToLock(List<(char, char)> lockableCorridors, List<char> virusPath, char currentPosition)
    {
        if (virusPath == null) return lockableCorridors[0];
        for (int i = 0; i < virusPath.Count - 1; i++)
        {
            char from = virusPath[i];
            char to = virusPath[i + 1];

            if (regularRooms.Contains(from) && secureRooms.Contains(to))
            {
                var corridor = (to, from);
                if (lockableCorridors.Contains(corridor))
                    return corridor;
            }
        }
        char target = virusPath[virusPath.Count - 1];
        foreach (var corridor in lockableCorridors)
        {
            if (corridor.Item1 == target)
                return corridor;
        }
        return lockableCorridors.Order().First();
    }

    private static char MoveVirus(char currentPosition)
    {
        var (target, path) = FindVirusTargetAndPath(currentPosition);

        if (path == null || path.Count < 2)
            return currentPosition;

        return path[1];
    }
}